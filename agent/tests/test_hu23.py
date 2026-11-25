"""HU-23 · Modelo externo con respaldo local, memoria de 4 turnos, verificación de cifras (TA-23.A1) y fuera de alcance (TA-23.A3)."""

import asyncio
from decimal import Decimal

from fastapi.testclient import TestClient
from langchain_core.messages import AIMessage, HumanMessage

import app.graph as graph
import app.main as main
from app import modelos
from app.alcance import fuera_de_alcance
from app.modelos import Interruptor
from app.verificacion import cifras_de_texto, verificar

client = TestClient(main.app)


class Modelo:
    """Modelo falso: devuelve las respuestas en orden y guarda los mensajes recibidos."""

    def __init__(self, *respuestas):
        self.respuestas = list(respuestas)
        self.recibidos = []

    async def ainvoke(self, mensajes):
        self.recibidos.append(mensajes)
        respuesta = self.respuestas.pop(0)
        if isinstance(respuesta, Exception):
            raise respuesta
        return respuesta


def pedir(**extra):
    return client.post("/chat", json={"mensaje": "Curso: AGIL-4B (cursoId 1)\n¿Cómo va el curso?", "token": "t", **extra})


# --- Verificación de cifras ---

DATOS = [{
    "herramienta": "obtener_calificaciones",
    "datos": {"estudiantes": [
        {"id": 8, "codigo": "E-005", "promedioPonderado": 6.42, "calificaciones": [{"actividadId": 103, "actividad": "Tarea 3", "nota": 4.0}]},
    ]},
}]


def test_cifras_de_texto_ignora_codigos_siglas_y_numeracion():
    cifras = cifras_de_texto("1. E-005 de AGIL-4B tiene 6,4 y domina 55 %.")
    assert [c["texto"] for c in cifras] == ["6,4", "55%"]


def test_cifra_redondeada_queda_verificada_con_su_fila():
    cifras = verificar("E-005 tiene promedio 6,4 y sacó 4 en la Tarea 3.", DATOS)
    promedio = next(c for c in cifras if c["valor"] == "6,4")
    assert promedio["verificada"] and promedio["herramienta"] == "obtener_calificaciones"
    assert promedio["estudianteId"] == 8
    assert promedio["campo"] == "estudiantes[0].promedioPonderado"
    nota = next(c for c in cifras if c["valor"] == "4")
    assert nota["verificada"] and nota["actividadId"] == 103


def test_entero_no_coincide_con_un_decimal_y_queda_no_verificada():
    cifras = verificar("E-005 tiene promedio 5.", DATOS)
    assert cifras == [{"valor": "5", "herramienta": None, "campo": None, "estudianteId": None, "actividadId": None, "verificada": False}]


def test_constantes_y_cifras_de_la_pregunta_no_se_marcan():
    assert verificar("Bajo 7,0 con 40 % de exámenes y 12 estudiantes.", DATOS, {Decimal(12)}) == []


def test_porcentaje_sale_de_una_proporcion():
    datos = [{"herramienta": "obtener_desempeno_tematico", "datos": {"temas": [{"domina": 0.553}]}}]
    assert verificar("El curso domina el 55 % del tema.", datos)[0]["verificada"]


def test_verificar_pide_redactar_otra_vez_una_sola_vez():
    estado = {"messages": [AIMessage(content="Promedio 9,9.")], "resultados": DATOS, "iterations": 2, "reintento": False}
    primero = graph.verificar_node(estado)
    assert isinstance(primero["messages"][0], HumanMessage) and "9,9" in primero["messages"][0].content
    segundo = graph.verificar_node({**estado, "reintento": True})
    assert segundo["cifras"][0]["verificada"] is False


# --- Fuera de alcance ---

def test_preguntas_fuera_de_alcance_responden_sin_modelo():
    assert "clínicos" in fuera_de_alcance("¿E-005 tiene depresión o ansiedad?")
    assert "modificar notas" in fuera_de_alcance("Súbele la nota de la Tarea 3 a Ana")
    assert "No envío mensajes" in fuera_de_alcance("Envíale un correo al estudiante")
    assert "predicciones" in fuera_de_alcance("¿Reprobará Diego la materia?")


def test_preguntas_de_datos_no_son_fuera_de_alcance():
    for pregunta in ["¿Quiénes tienen promedio ponderado bajo 7?", "¿Qué notas cambiaron en la última sincronización?",
                     "¿Quiénes enviaron la Tarea 3?", "¿Quiénes aprobaron el examen?", "Dame un diagnóstico del curso"]:
        assert fuera_de_alcance(pregunta) is None, pregunta


def test_chat_fuera_de_alcance_no_llama_al_modelo(monkeypatch):
    local = Modelo()
    monkeypatch.setattr(graph, "llm", local)
    datos = client.post("/chat", json={"mensaje": "¿Diego tiene algún trastorno psicológico?", "token": "t"}).json()
    assert datos["proveedor"] == "NINGUNO" and datos["modelo"] == "ninguno"
    assert local.recibidos == []


# --- Interruptor de 3 fallos en 5 minutos ---

def test_interruptor_pausa_cinco_minutos_con_tres_fallos_en_cinco_minutos():
    ahora = [0.0]
    interruptor = Interruptor(reloj=lambda: ahora[0])
    interruptor.registrar_fallo()
    ahora[0] = 400  # el primer fallo ya salió de la ventana
    interruptor.registrar_fallo()
    interruptor.registrar_fallo()
    assert interruptor.disponible()
    interruptor.registrar_fallo()
    assert not interruptor.disponible()
    ahora[0] = 400 + 299
    assert not interruptor.disponible()
    ahora[0] = 400 + 300
    assert interruptor.disponible()


# --- Respaldo local ---

def test_responde_el_externo_cuando_funciona(monkeypatch):
    monkeypatch.setattr(modelos, "interruptor", Interruptor())
    monkeypatch.setattr(modelos, "MODELO_EXTERNO", "modelo-externo")
    monkeypatch.setattr(graph, "llm_externo", Modelo(AIMessage(content=[{"type": "text", "text": "Todo en orden."}])))
    monkeypatch.setattr(graph, "llm", Modelo())
    datos = pedir().json()
    assert datos["proveedor"] == "EXTERNO" and datos["respaldo"] is False
    assert datos["modelo"] == "modelo-externo"
    assert datos["respuesta"] == "Todo en orden."


def test_si_el_externo_falla_la_misma_pregunta_va_al_local(monkeypatch):
    monkeypatch.setattr(modelos, "interruptor", Interruptor())
    externo = Modelo(RuntimeError("429 Too Many Requests"))
    local = Modelo(AIMessage(content="Respuesta local."))
    monkeypatch.setattr(graph, "llm_externo", externo)
    monkeypatch.setattr(graph, "llm", local)
    datos = pedir().json()
    assert datos["proveedor"] == "LOCAL" and datos["respaldo"] is True
    assert datos["respuesta"] == "Respuesta local."
    assert local.recibidos[0][-1].content == externo.recibidos[0][-1].content


def test_respuesta_sin_formato_esperado_tambien_va_al_local(monkeypatch):
    monkeypatch.setattr(modelos, "interruptor", Interruptor())
    externo = Modelo(AIMessage(content="", tool_calls=[{"name": "herramienta_inventada", "args": {}, "id": "x"}]))
    monkeypatch.setattr(graph, "llm_externo", externo)
    monkeypatch.setattr(graph, "llm", Modelo(AIMessage(content="ok")))
    assert pedir().json()["respaldo"] is True


def test_tiempo_agotado_del_externo_va_al_local(monkeypatch):
    class Lento:
        async def ainvoke(self, _mensajes):
            await asyncio.sleep(1)

    monkeypatch.setattr(modelos, "interruptor", Interruptor())
    monkeypatch.setattr(graph, "LIMITE_EXTERNO_SEGUNDOS", 0.01)
    monkeypatch.setattr(graph, "llm_externo", Lento())
    monkeypatch.setattr(graph, "llm", Modelo(AIMessage(content="ok")))
    assert pedir().json()["respaldo"] is True


def test_con_el_externo_en_pausa_va_directo_al_local(monkeypatch):
    interruptor = Interruptor()
    for _ in range(3):
        interruptor.registrar_fallo()
    monkeypatch.setattr(modelos, "interruptor", interruptor)
    externo = Modelo()
    monkeypatch.setattr(graph, "llm_externo", externo)
    monkeypatch.setattr(graph, "llm", Modelo(AIMessage(content="ok")))
    datos = pedir().json()
    assert datos["respaldo"] is True and externo.recibidos == []


def test_solo_modelo_local_no_llama_al_externo_y_no_es_respaldo(monkeypatch):
    monkeypatch.setattr(modelos, "interruptor", Interruptor())
    externo = Modelo()
    monkeypatch.setattr(graph, "llm_externo", externo)
    monkeypatch.setattr(graph, "llm", Modelo(AIMessage(content="ok")))
    datos = pedir(modo="local").json()
    assert datos["proveedor"] == "LOCAL" and datos["respaldo"] is False
    assert externo.recibidos == []


def test_modo_externo_sin_configurar_responde_503(monkeypatch):
    monkeypatch.setattr(graph, "llm_externo", None)
    assert pedir(modo="externo").status_code == 503


# --- Memoria ---

def test_los_turnos_anteriores_llegan_al_modelo_antes_de_la_pregunta(monkeypatch):
    monkeypatch.setattr(graph, "llm_externo", None)
    local = Modelo(AIMessage(content="ok"))
    monkeypatch.setattr(graph, "llm", local)
    historial = [{"pregunta": "¿Cómo va E-005?", "respuesta": "E-005 tiene 6,4."}]
    pedir(historial=historial)
    recibidos = local.recibidos[0]
    assert recibidos[1].content == "¿Cómo va E-005?"
    assert recibidos[2].content == "E-005 tiene 6,4."
    assert recibidos[3].content.endswith("¿Cómo va el curso?")


def test_historial_de_mas_de_cuatro_turnos_es_invalido():
    turnos = [{"pregunta": "p", "respuesta": "r"}] * 5
    assert pedir(historial=turnos).status_code == 422


def test_salud_informa_los_dos_modelos(monkeypatch):
    datos = client.get("/salud").json()
    assert datos["modelo"] == graph.MODEL_NAME
    assert datos["externo"]["estado"] in {"NO_CONFIGURADO", "OK", "EN_PAUSA"}
    assert datos["activo"] in {"EXTERNO", "LOCAL"}


# --- Herramientas nuevas (HU-22, TA-15.A2) ---

import pytest
import respx

from app.tools import backend_tools


@pytest.mark.asyncio
@respx.mock
async def test_detalle_de_actividad_y_refuerzos_aplicados_llaman_a_su_ruta(monkeypatch):
    monkeypatch.setattr(backend_tools, "BACKEND_URL", "http://backend.test")
    actividad = respx.get("http://backend.test/api/tools/actividad?cursoId=1&actividadId=103").respond(json={"herramienta": "obtener_detalle_actividad"})
    aplicados = respx.get("http://backend.test/api/tools/refuerzos-aplicados?cursoId=1").respond(json={"herramienta": "consultar_refuerzos_aplicados"})
    estado = {
        "messages": [AIMessage(content="", tool_calls=[
            {"name": "obtener_detalle_actividad", "args": {"cursoId": 1, "actividadId": 103}, "id": "a"},
            {"name": "consultar_refuerzos_aplicados", "args": {"cursoId": 1}, "id": "b"},
        ])],
        "token": "t", "iterations": 1, "trazas": [], "error": None,
    }

    resultado = await graph.tool_node(estado)

    assert actividad.called and aplicados.called
    assert [t.estado for t in resultado["trazas"]] == ["OK", "OK"]
    assert {r["herramienta"] for r in resultado["resultados"]} == {"obtener_detalle_actividad", "consultar_refuerzos_aplicados"}


def test_detalle_de_actividad_exige_la_actividad():
    with pytest.raises(Exception):
        graph.HERRAMIENTAS["obtener_detalle_actividad"][1].model_validate({"cursoId": 1})


# --- HU-18 · Las 3 preguntas fuera de alcance de la evaluación (backend/EduMonitor.Application/Evaluacion/EvaluacionService.cs) ---

@pytest.mark.parametrize("pregunta", [
    "¿Karina Lara tiene depresión o ansiedad?",
    "Súbele la nota de la Tarea 3 a Elena Espinoza",
    "¿Isabel Iza va a reprobar la materia?",
])
def test_preguntas_fuera_de_alcance_de_la_evaluacion(pregunta):
    assert fuera_de_alcance(pregunta) is not None
