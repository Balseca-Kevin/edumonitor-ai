import os
import time
from typing import TypedDict, Annotated, Sequence, List
import operator

from langchain_core.messages import BaseMessage, HumanMessage, AIMessage, ToolMessage, SystemMessage
from langchain_core.tools import tool
from langchain_ollama import ChatOllama
from langgraph.graph import StateGraph, END

from app.schemas.tools import TrazaHerramienta
from app.tools.backend_tools import consultar_cursos_backend

MAX_ITERACIONES = 3
MODEL_NAME = os.getenv("OLLAMA_MODEL", "qwen2.5:7b")
OLLAMA_BASE_URL = os.getenv("OLLAMA_BASE_URL", "http://localhost:11434")

# Sin este mensaje el modelo no sabe que el docente ya inició sesión y pide credenciales
# en lugar de llamar a la herramienta.
PROMPT_SISTEMA = (
    "Eres el asistente de EduMonitor AI para docentes de la UTA. "
    "El docente ya inició sesión y el sistema envía su autorización automáticamente: "
    "nunca le pidas usuario, contraseña ni credenciales. "
    "Para cualquier pregunta sobre sus cursos, llama a la herramienta listar_cursos_autorizados "
    "antes de responder. "
    "Responde en español, de forma breve, usando solo los datos que devuelve la herramienta; "
    "no inventes cursos ni fechas. Si la herramienta no devuelve cursos, dilo así."
)

class AgentState(TypedDict):
    messages: Annotated[Sequence[BaseMessage], operator.add]
    token: str
    iterations: int
    trazas: List[TrazaHerramienta]
    error: bool

@tool
def listar_cursos_autorizados() -> str:
    """Obtiene la lista de cursos a los que el docente tiene acceso autorizado."""
    return "Llamada a listar_cursos_autorizados"

llm = ChatOllama(
    model=MODEL_NAME,
    base_url=OLLAMA_BASE_URL,
    temperature=0
).bind_tools([listar_cursos_autorizados])

async def model_node(state: AgentState):
    iterations = state.get("iterations", 0) + 1
    if iterations > MAX_ITERACIONES:
        return {
            "messages": [AIMessage(content="Se ha alcanzado el límite máximo de iteraciones.")],
            "iterations": iterations
        }
    
    response = await llm.ainvoke([SystemMessage(content=PROMPT_SISTEMA), *state["messages"]])
    return {"messages": [response], "iterations": iterations}

async def tool_node(state: AgentState):
    token = state["token"]
    resultado, traza = await consultar_cursos_backend(token)
    
    trazas = list(state.get("trazas", []))
    trazas.append(traza)
    
    if traza.estado == "ERROR":
        return {"error": True, "trazas": trazas}
        
    last_message = state["messages"][-1]
    tool_call_id = last_message.tool_calls[0]["id"] if hasattr(last_message, "tool_calls") and last_message.tool_calls else "tool_1"
    
    tool_message = ToolMessage(content=str(resultado), tool_call_id=tool_call_id)
    return {"messages": [tool_message], "trazas": trazas, "error": False}

def error_node(state: AgentState):
    """Responde con el texto fijo obligatorio si la herramienta falla, sin invocar al modelo."""
    return {"messages": [AIMessage(content="No pude consultar tus cursos en este momento.")], "error": True}

def should_continue(state: AgentState):
    if state.get("error"):
        return "error"
    
    last_message = state["messages"][-1]
    if hasattr(last_message, "tool_calls") and last_message.tool_calls:
        return "tools"
    return END

workflow = StateGraph(AgentState)
workflow.add_node("agent", model_node)
workflow.add_node("tools", tool_node)
workflow.add_node("error", error_node)

workflow.set_entry_point("agent")
workflow.add_conditional_edges("agent", should_continue, {"tools": "tools", "error": "error", END: END})
workflow.add_conditional_edges("tools", lambda s: "error" if s.get("error") else "agent", {"error": "error", "agent": "agent"})
workflow.add_edge("error", END)

app_graph = workflow.compile()