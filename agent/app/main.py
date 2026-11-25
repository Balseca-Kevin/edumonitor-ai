import logging
import time

import httpx
from fastapi import FastAPI, HTTPException
from langchain_core.messages import AIMessage, HumanMessage

from app import graph, modelos
from app.alcance import fuera_de_alcance
from app.graph import FalloExterno, MODEL_NAME, OLLAMA_BASE_URL, app_graph, texto_de
from app.schemas.tools import ChatPeticion, ChatRespuesta
from app.verificacion import numeros_permitidos

logger = logging.getLogger("agent")
app = FastAPI(title="EduMonitor AI - Servicio de Agente", version="0.3.0")


async def _correr(request: ChatPeticion, modo: str) -> dict:
    historial = []
    for turno in request.historial:
        historial += [HumanMessage(content=turno.pregunta), AIMessage(content=turno.respuesta)]
    permitidas = numeros_permitidos(request.mensaje, *[f"{t.pregunta} {t.respuesta}" for t in request.historial])
    return await app_graph.ainvoke({
        "messages": [*historial, HumanMessage(content=request.mensaje)],
        "token": request.token,
        "cursoId": request.cursoId,
        "iterations": 0,
        "trazas": [],
        "error": None,
        "modo": modo,
        "resultados": [],
        "permitidas": permitidas,
        "reintento": False,
        "cifras": [],
    })


@app.post("/chat", response_model=ChatRespuesta)
async def chat_endpoint(request: ChatPeticion):
    """HU-23 · Externo primero y, si falla, la misma pregunta al modelo local. Las preguntas fuera de alcance se responden con
    reglas, sin modelo (TA-23.A3)."""
    inicio = time.time()

    fija = fuera_de_alcance(request.mensaje)
    if fija is not None:
        return ChatRespuesta(respuesta=fija, herramientas=[], modelo="ninguno", proveedor="NINGUNO",
                             duracionMs=int((time.time() - inicio) * 1000))

    hay_externo = graph.llm_externo is not None
    if request.modo == "externo" and not hay_externo:
        raise HTTPException(status_code=503, detail="El modelo externo no está configurado.")

    usar_externo = hay_externo and (request.modo == "externo" or (request.modo == "auto" and modelos.interruptor.disponible()))
    respaldo = hay_externo and request.modo == "auto" and not usar_externo
    try:
        resultado = None
        if usar_externo:
            try:
                resultado = await _correr(request, "externo")
                modelos.interruptor.registrar_exito()
            except FalloExterno as exc:
                modelos.interruptor.registrar_fallo()
                logger.warning("El modelo externo falló (%s); responde el modelo local", exc)
                if request.modo == "externo":
                    raise HTTPException(status_code=503, detail="El modelo externo no respondió.") from exc
                respaldo = True
        if resultado is None:
            resultado = await _correr(request, "local")
    except HTTPException:
        raise
    except Exception:
        logger.exception("Error procesando solicitud en /chat")
        raise HTTPException(status_code=500, detail="Error interno del servicio de agente.")

    externo = resultado is not None and usar_externo and not respaldo
    return ChatRespuesta(
        respuesta=texto_de(resultado["messages"][-1]),
        herramientas=resultado.get("trazas", []),
        modelo=modelos.MODELO_EXTERNO if externo else MODEL_NAME,
        duracionMs=int((time.time() - inicio) * 1000),
        proveedor="EXTERNO" if externo else "LOCAL",
        respaldo=respaldo,
        cifras=resultado.get("cifras", []),
    )


@app.get("/salud")
async def salud():
    """HU-20 y HU-23 · Estado del agente, de Ollama y del modelo externo para «Estado del sistema». Solo lo llama la API.
    ollama es OK si responde /api/tags en 2 s. El modelo externo no se llama: se informa si está configurado y si está en pausa."""
    ollama = "NO_DISPONIBLE"
    try:
        async with httpx.AsyncClient(timeout=2) as cliente:
            respuesta = await cliente.get(f"{OLLAMA_BASE_URL}/api/tags")
            if respuesta.status_code == 200:
                ollama = "OK"
    except httpx.HTTPError:
        logger.warning("Ollama no respondió en /api/tags")
    return {"estado": "OK", "modelo": MODEL_NAME, "ollama": ollama, **modelos.estado_modelos()}
