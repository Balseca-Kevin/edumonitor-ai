import pytest
import respx
import httpx
from unittest.mock import AsyncMock, patch
from fastapi.testclient import TestClient
from langchain_core.messages import AIMessage, HumanMessage

from app.main import app
from app.schemas.tools import ChatPeticion, TrazaHerramienta

client = TestClient(app)

def test_chat_contrato_y_cursos_ok():
    backend_url = "http://localhost:5000/api/tools/cursos-autorizados"
    mock_backend = {
        "herramienta": "listar_cursos_autorizados",
        "generado": "2026-10-22T10:00:00Z",
        "cursos": [{"id": 1, "nombreCorto": "AGIL-4B", "nombre": "Metodologías Ágiles", "ultimaSincronizacion": None}]
    }
    
    mock_graph_response = {
        "messages": [
            HumanMessage(content="¿Qué cursos tengo?"),
            AIMessage(content="Tienes asignado el curso Metodologías Ágiles (AGIL-4B).")
        ],
        "trazas": [
            TrazaHerramienta(
                nombre="listar_cursos_autorizados",
                parametros={},
                estado="OK",
                duracionMs=45
            )
        ]
    }

    with respx.mock:
        respx.get(backend_url).respond(status_code=200, json=mock_backend)
        
        with patch("app.main.app_graph.ainvoke", new_callable=AsyncMock) as mock_invoke:
            mock_invoke.return_value = mock_graph_response
            
            response = client.post("/chat", json={"mensaje": "¿Qué cursos tengo?", "token": "jwt-fake"})
            assert response.status_code == 200
            data = response.json()
            
            assert "respuesta" in data
            assert "Metodologías Ágiles" in data["respuesta"]
            assert len(data["herramientas"]) == 1
            assert data["herramientas"][0]["estado"] == "OK"
            assert data["herramientas"][0]["nombre"] == "listar_cursos_autorizados"

def test_chat_backend_caido_no_inventa():
    backend_url = "http://localhost:5000/api/tools/cursos-autorizados"
    
    mock_graph_response = {
        "messages": [
            HumanMessage(content="¿Qué cursos tengo?"),
            AIMessage(content="No pude consultar tus cursos en este momento.")
        ],
        "trazas": [
            TrazaHerramienta(
                nombre="listar_cursos_autorizados",
                parametros={},
                estado="ERROR",
                duracionMs=12
            )
        ]
    }

    with respx.mock:
        respx.get(backend_url).side_effect = httpx.ConnectError("Conexión rehusada")
        
        with patch("app.main.app_graph.ainvoke", new_callable=AsyncMock) as mock_invoke:
            mock_invoke.return_value = mock_graph_response
            
            response = client.post("/chat", json={"mensaje": "¿Qué cursos tengo?", "token": "jwt-fake"})
            assert response.status_code == 200
            data = response.json()
            
            assert data["respuesta"] == "No pude consultar tus cursos en este momento."
            assert len(data["herramientas"]) == 1
            assert data["herramientas"][0]["estado"] == "ERROR"

def test_chat_backend_401():
    backend_url = "http://localhost:5000/api/tools/cursos-autorizados"
    
    mock_graph_response = {
        "messages": [
            HumanMessage(content="¿Qué cursos tengo?"),
            AIMessage(content="No pude consultar tus cursos en este momento.")
        ],
        "trazas": [
            TrazaHerramienta(
                nombre="listar_cursos_autorizados",
                parametros={},
                estado="ERROR",
                duracionMs=15
            )
        ]
    }

    with respx.mock:
        respx.get(backend_url).respond(status_code=401)
        
        with patch("app.main.app_graph.ainvoke", new_callable=AsyncMock) as mock_invoke:
            mock_invoke.return_value = mock_graph_response
            
            response = client.post("/chat", json={"mensaje": "¿Qué cursos tengo?", "token": "jwt-invalido"})
            assert response.status_code == 200
            data = response.json()
            
            assert data["respuesta"] == "No pude consultar tus cursos en este momento."
            assert data["herramientas"][0]["estado"] == "ERROR"

def test_token_no_aparece_en_logs():
    token_secreto = "token-super-secreto-12345"
    peticion = ChatPeticion(mensaje="Hola", token=token_secreto)
    assert token_secreto not in repr(peticion)