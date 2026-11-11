import asyncio

import httpx
import pytest
import respx
from langchain_core.messages import AIMessage, HumanMessage

import app.graph as graph
from app.tools import backend_tools


def llamada(nombre, parametros, id_llamada="llamada-1"):
    return {"name": nombre, "args": parametros, "id": id_llamada}


def estado(*llamadas):
    return {
        "messages": [AIMessage(content="", tool_calls=list(llamadas))],
        "token": "token-de-servicio-de-prueba",
        "iterations": 1,
        "trazas": [],
        "error": None,
    }


@pytest.mark.asyncio
@respx.mock
async def test_grafo_registra_cada_llamada_y_envia_el_token_fuera_del_modelo(monkeypatch):
    url = "http://backend.test/api/tools"
    monkeypatch.setattr(backend_tools, "BACKEND_URL", "http://backend.test")
    notas = respx.get(f"{url}/calificaciones?cursoId=1&estudianteId=9").respond(
        json={"calificaciones": [{"nota": 0}]}
    )
    alertas = respx.get(f"{url}/alertas?cursoId=1").respond(json={"alertas": []})
    cursos = respx.get(f"{url}/cursos-autorizados").respond(
        json={"generado": "2026-11-10T00:00:00Z", "cursos": []}
    )
    llamadas = [
        llamada("obtener_calificaciones", {"cursoId": 1, "estudianteId": 9}, "notas"),
        llamada("identificar_alertas", {"cursoId": 1}, "alertas"),
        llamada("listar_cursos_autorizados", {}, "cursos"),
    ]

    resultado = await graph.tool_node(estado(*llamadas))

    assert [t.nombre for t in resultado["trazas"]] == [l["name"] for l in llamadas]
    assert [t.parametros for t in resultado["trazas"]] == [l["args"] for l in llamadas]
    assert all(t.estado == "OK" and t.duracionMs >= 0 for t in resultado["trazas"])
    assert [m.tool_call_id for m in resultado["messages"]] == [l["id"] for l in llamadas]
    assert '"nota": 0' in resultado["messages"][0].content
    for ruta in (notas, alertas, cursos):
        assert ruta.calls.last.request.headers["Authorization"] == "Bearer token-de-servicio-de-prueba"
    assert "token-de-servicio-de-prueba" not in repr(resultado)


@pytest.mark.parametrize("args", [
    {}, {"cursoId": 0}, {"cursoId": "1"}, {"cursoId": True},
    {"cursoId": 1, "estudianteId": -1}, {"cursoId": 1, "token": "secreto"},
])
@pytest.mark.asyncio
@respx.mock
async def test_parametros_invalidos_quedan_en_traza_sin_llamar_al_backend(args):
    resultado = await graph.tool_node(estado(llamada("obtener_calificaciones", args)))

    assert len(respx.calls) == 0
    assert resultado["trazas"][0].estado == "ERROR"
    assert resultado["trazas"][0].parametros == {}
    assert "parametros_invalidos" in resultado["messages"][0].content


@pytest.mark.parametrize("fallo,fragmento", [
    (403, "No tienes acceso a ese curso."),
    (503, "No pude consultar los datos académicos"),
    ("timeout", "La consulta tardó más de lo esperado."),
])
@pytest.mark.asyncio
@respx.mock
async def test_grafo_responde_al_fallo_sin_volver_a_invocar_el_modelo(monkeypatch, fallo, fragmento):
    monkeypatch.setattr(backend_tools, "BACKEND_URL", "http://backend.test")
    ruta = respx.get("http://backend.test/api/tools/alertas?cursoId=2")
    if fallo == "timeout":
        async def backend_lento(_request):
            await asyncio.sleep(1)
            return httpx.Response(200, json={"alertas": []})

        monkeypatch.setattr(backend_tools, "LIMITE_SEGUNDOS", 0.02)
        ruta.mock(side_effect=backend_lento)
    else:
        ruta.respond(status_code=fallo)

    class Modelo:
        invocaciones = 0

        async def ainvoke(self, mensajes):
            self.invocaciones += 1
            return AIMessage(content="", tool_calls=[llamada("identificar_alertas", {"cursoId": 2})])

    modelo = Modelo()
    monkeypatch.setattr(graph, "llm", modelo)
    entrada = estado()
    entrada["messages"] = [HumanMessage(content="¿Qué alertas tiene el curso 2?")]
    entrada["iterations"] = 0

    resultado = await graph.app_graph.ainvoke(entrada)

    assert modelo.invocaciones == 1
    assert fragmento in resultado["messages"][-1].content
    assert resultado["trazas"][0].estado == "ERROR"
