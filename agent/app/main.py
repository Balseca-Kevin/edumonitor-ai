import time
import logging
from fastapi import FastAPI, HTTPException
from langchain_core.messages import HumanMessage
from app.schemas.tools import ChatPeticion, ChatRespuesta
from app.graph import app_graph, MODEL_NAME, OLLAMA_BASE_URL
import httpx

logger = logging.getLogger("agent")
app = FastAPI(title="EduMonitor AI - Servicio de Agente", version="0.1.0")

@app.post("/chat", response_model=ChatRespuesta)
async def chat_endpoint(request: ChatPeticion):
    inicio = time.time()
    try:
        inputs = {
            "messages": [HumanMessage(content=request.mensaje)],
            "token": request.token,
            "iterations": 0,
            "trazas": [],
            "error": None
        }
        
        result = await app_graph.ainvoke(inputs)
        duracion_total = int((time.time() - inicio) * 1000)
        
        last_message = result["messages"][-1]
        contenido = last_message.content
        trazas = result.get("trazas", [])
        
        return ChatRespuesta(
            respuesta=contenido,
            herramientas=trazas,
            modelo=MODEL_NAME,
            duracionMs=duracion_total
        )
    except Exception as e:
        logger.exception("Error procesando solicitud en /chat")
        raise HTTPException(status_code=500, detail="Error interno del servicio de agente.")


@app.get("/salud")
async def salud():
    """HU-20 · Estado del agente y de Ollama para la pantalla «Estado del sistema» (docs/contratos-api.md).
    Solo lo llama la API por la red interna. ollama es OK si responde /api/tags en 2 s; si no, NO_DISPONIBLE."""
    ollama = "NO_DISPONIBLE"
    try:
        async with httpx.AsyncClient(timeout=2) as cliente:
            respuesta = await cliente.get(f"{OLLAMA_BASE_URL}/api/tags")
            if respuesta.status_code == 200:
                ollama = "OK"
    except httpx.HTTPError:
        logger.warning("Ollama no respondió en /api/tags")
    return {"estado": "OK", "modelo": MODEL_NAME, "ollama": ollama}
