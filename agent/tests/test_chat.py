"""HU-07 · Pruebas del agente con la herramienta y el grafo reales.

El backend se simula con respx en BACKEND_URL y el modelo con un LLM falso, así CI no necesita Ollama ni el backend.
Cubre el criterio de HU-07: el agente llama a listar_cursos_autorizados, responde con los cursos del backend,
registra la traza y, si la herramienta falla, informa el error sin inventar cursos.
"""

import logging

import httpx
import pytest
import respx
from fastapi.testclient import TestClient
from langchain_core.messages import AIMessage, ToolMessage

import app.graph as grafo
from app.main import app
from app.schemas.tools import ChatPeticion
from app.tools.backend_tools import BACKEND_URL, consultar_cursos_backend

URL_HERRAMIENTA = f"{BACKEND_URL}/api/tools/cursos-autorizados"
TOKEN = "jwt-de-prueba-0123456789"
CURSO = "Metodologías Ágiles (lab)"
RESPUESTA_BACKEND = {
    "herramienta": "listar_cursos_autorizados",
    "generado": "2026-10-22T09:30:00-05:00",
    "cursos": [
        {"id": 1, "nombreCorto": "AGIL-4B", "nombre": CURSO, "ultimaSincronizacion": "2026-10-22T09:15:00-05:00"}
    ],
}


class LlmFalso:
    """Primero pide la herramienta; después responde con los cursos que vengan en el ToolMessage."""

    def __init__(self):
        self.llamadas = []

    async def ainvoke(self, mensajes):
        self.llamadas.append(list(mensajes))
        resultado = next((m for m in reversed(mensajes) if isinstance(m, ToolMessage)), None)
        if resultado is None:
            return AIMessage(content="", tool_calls=[{"name": "listar_cursos_autorizados", "args": {}, "id": "llamada_1"}])
        return AIMessage(content=f"Según el backend: {resultado.content}")


@pytest.fixture
def llm_falso(monkeypatch):
    falso = LlmFalso()
    monkeypatch.setattr(grafo, "llm", falso)
    return falso


# --- Herramienta contra el backend ---------------------------------------------------------------


@pytest.mark.asyncio
@respx.mock
async def test_herramienta_lee_cursos_del_backend_con_el_jwt():
    ruta = respx.get(URL_HERRAMIENTA).respond(200, json=RESPUESTA_BACKEND)

    resultado, traza = await consultar_cursos_backend(TOKEN)

    assert ruta.calls.last.request.headers["Authorization"] == f"Bearer {TOKEN}"
    assert resultado["cursos"][0]["nombre"] == CURSO
    assert (traza.nombre, traza.estado, traza.parametros) == ("listar_cursos_autorizados", "OK", {})
    assert traza.duracionMs >= 0


@pytest.mark.asyncio
@respx.mock
async def test_herramienta_con_401_es_error():
    respx.get(URL_HERRAMIENTA).respond(401)

    resultado, traza = await consultar_cursos_backend("jwt-vencido")

    assert traza.estado == "ERROR"
    assert "cursos" not in resultado


@pytest.mark.asyncio
@respx.mock
async def test_herramienta_con_backend_caido_es_error():
    respx.get(URL_HERRAMIENTA).mock(side_effect=httpx.ConnectError("Conexión rehusada"))

    resultado, traza = await consultar_cursos_backend(TOKEN)

    assert traza.estado == "ERROR"
    assert "cursos" not in resultado


@pytest.mark.asyncio
@respx.mock
async def test_herramienta_con_respuesta_fuera_del_contrato_es_error():
    respx.get(URL_HERRAMIENTA).respond(200, json={"success": True, "data": []})

    _, traza = await consultar_cursos_backend(TOKEN)

    assert traza.estado == "ERROR"


# --- Grafo con la herramienta real ---------------------------------------------------------------


def _entrada(mensaje="¿Qué cursos tengo asignados?"):
    from langchain_core.messages import HumanMessage

    return {"messages": [HumanMessage(content=mensaje)], "token": TOKEN, "iterations": 0, "trazas": [], "error": False}


@pytest.mark.asyncio
@respx.mock
async def test_grafo_llama_a_la_herramienta_y_responde_con_los_cursos_del_backend(llm_falso):
    respx.get(URL_HERRAMIENTA).respond(200, json=RESPUESTA_BACKEND)

    resultado = await grafo.app_graph.ainvoke(_entrada())

    assert CURSO in resultado["messages"][-1].content
    assert [t.estado for t in resultado["trazas"]] == ["OK"]
    assert len(llm_falso.llamadas) == 2  # pide la herramienta y luego redacta con su resultado


@pytest.mark.asyncio
@respx.mock
async def test_grafo_con_backend_caido_no_inventa_cursos(llm_falso):
    respx.get(URL_HERRAMIENTA).mock(side_effect=httpx.ConnectError("Conexión rehusada"))

    resultado = await grafo.app_graph.ainvoke(_entrada())

    respuesta = resultado["messages"][-1].content
    assert respuesta == "No pude consultar tus cursos en este momento."
    assert CURSO not in respuesta and "AGIL" not in respuesta
    assert [t.estado for t in resultado["trazas"]] == ["ERROR"]
    assert len(llm_falso.llamadas) == 1  # tras el error no se vuelve a llamar al modelo


# --- Endpoint /chat ------------------------------------------------------------------------------


@respx.mock
def test_chat_responde_con_el_formato_del_contrato(llm_falso):
    respx.get(URL_HERRAMIENTA).respond(200, json=RESPUESTA_BACKEND)

    respuesta = TestClient(app).post("/chat", json={"mensaje": "¿Qué cursos tengo asignados?", "token": TOKEN})

    assert respuesta.status_code == 200
    cuerpo = respuesta.json()
    assert set(cuerpo) == {"respuesta", "herramientas", "modelo", "duracionMs"}
    assert CURSO in cuerpo["respuesta"]
    assert cuerpo["herramientas"][0]["nombre"] == "listar_cursos_autorizados"
    assert cuerpo["herramientas"][0]["estado"] == "OK"
    assert cuerpo["modelo"] == grafo.MODEL_NAME


@respx.mock
def test_chat_con_backend_caido_responde_200_con_error_y_sin_token_en_logs(llm_falso, caplog):
    respx.get(URL_HERRAMIENTA).mock(side_effect=httpx.ConnectError("Conexión rehusada"))
    caplog.set_level(logging.DEBUG)

    respuesta = TestClient(app).post("/chat", json={"mensaje": "¿Qué cursos tengo?", "token": TOKEN})

    assert respuesta.status_code == 200
    cuerpo = respuesta.json()
    assert cuerpo["respuesta"] == "No pude consultar tus cursos en este momento."
    assert cuerpo["herramientas"][0]["estado"] == "ERROR"
    assert "listar_cursos_autorizados" in caplog.text  # la traza queda en el log
    assert TOKEN not in caplog.text


def test_chat_sin_token_responde_422():
    respuesta = TestClient(app).post("/chat", json={"mensaje": "Hola"})

    assert respuesta.status_code == 422


def test_el_token_no_aparece_en_la_representacion_de_la_peticion():
    assert TOKEN not in repr(ChatPeticion(mensaje="Hola", token=TOKEN))
