import time
import logging
from fastapi import FastAPI, HTTPException
from langchain_core.messages import HumanMessage
from app.schemas.tools import ChatPeticion, ChatRespuesta
from app.graph import app_graph, MODEL_NAME

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
            "error": False
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