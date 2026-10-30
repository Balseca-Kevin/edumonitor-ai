import asyncio

from langchain_core.messages import AIMessage, HumanMessage, SystemMessage

import app.graph as graph


class LlmQueRegistra:
    """Sustituye al modelo y guarda los mensajes que recibe."""

    def __init__(self):
        self.recibidos = None

    async def ainvoke(self, mensajes):
        self.recibidos = mensajes
        return AIMessage(content="ok")


def test_el_modelo_recibe_primero_el_prompt_de_sistema(monkeypatch):
    llm = LlmQueRegistra()
    monkeypatch.setattr(graph, "llm", llm)

    asyncio.run(graph.model_node({"messages": [HumanMessage(content="¿Qué cursos tengo?")], "iterations": 0}))

    assert isinstance(llm.recibidos[0], SystemMessage)
    assert llm.recibidos[0].content == graph.PROMPT_SISTEMA
    assert llm.recibidos[1].content == "¿Qué cursos tengo?"


def test_el_prompt_prohibe_pedir_credenciales_y_exige_la_herramienta():
    assert "nunca le pidas" in graph.PROMPT_SISTEMA
    assert "listar_cursos_autorizados" in graph.PROMPT_SISTEMA
