import httpx
import respx
from fastapi.testclient import TestClient

from app.graph import MODEL_NAME, OLLAMA_BASE_URL
from app.main import app

client = TestClient(app)


@respx.mock
def test_salud_con_ollama_disponible():
    respx.get(f"{OLLAMA_BASE_URL}/api/tags").mock(return_value=httpx.Response(200, json={"models": []}))

    respuesta = client.get("/salud")

    assert respuesta.status_code == 200
    assert respuesta.json() == {"estado": "OK", "modelo": MODEL_NAME, "ollama": "OK"}


@respx.mock
def test_salud_con_ollama_caido_responde_no_disponible():
    respx.get(f"{OLLAMA_BASE_URL}/api/tags").mock(side_effect=httpx.ConnectError("sin conexión"))

    respuesta = client.get("/salud")

    assert respuesta.status_code == 200
    assert respuesta.json()["estado"] == "OK"
    assert respuesta.json()["ollama"] == "NO_DISPONIBLE"
