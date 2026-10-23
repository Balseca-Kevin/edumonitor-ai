from typing import TypedDict, Annotated, Sequence
import operator
from langchain_community.chat_models import ChatOllama
from langgraph.graph import StateGraph, END

class AgentState(TypedDict):
    messages: Annotated[Sequence[dict], operator.add]
    iterations: int

llm = ChatOllama(model="qwen2.5:7b", temperature=0)

def model_node(state: AgentState):
    iterations = state.get("iterations", 0) + 1
    # Límite estricto de máximo 4 iteraciones
    if iterations > 4:
        return {
            "messages": [{"role": "assistant", "content": "Se ha alcanzado el límite máximo de iteraciones."}],
            "iterations": iterations
        }
    
    response = llm.invoke(state["messages"])
    return {"messages": [{"role": "assistant", "content": response.content}], "iterations": iterations}

workflow = StateGraph(AgentState)
workflow.add_node("agent", model_node)
workflow.set_entry_point("agent")
workflow.add_edge("agent", END)

app_graph = workflow.compile()