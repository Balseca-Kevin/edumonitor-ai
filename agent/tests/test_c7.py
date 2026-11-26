"""C7 · Tramo de la pantalla en las herramientas, enrutador de preguntas, guía de la aplicación y redacción del texto final."""

import pytest
from fastapi.testclient import TestClient
from langchain_core.messages import AIMessage
from pydantic import ValidationError

import app.graph as graph
import app.main as main
from app.enrutador import (
    APLICACION, DATOS, GENERAL, MIXTO, clasificar, contexto_minimo, parece_dato_del_curso, pregunta_del_docente, ruta_de,
)
from app.guia_app import GUIA_APP
from app.redaccion import con_coma, pulir, pulir_cifras
from app.schemas.tools import ChatPeticion

client = TestClient(main.app)


class Modelo:
    """Modelo falso con herramientas enlazadas: .bound es el modelo sin herramientas, como en bind_tools."""

    def __init__(self, *respuestas, sin_herramientas=None):
        self.respuestas = list(respuestas)
        self.recibidos = []
        if sin_herramientas is not None:
            self.bound = sin_herramientas

    async def ainvoke(self, mensajes):
        self.recibidos.append(mensajes)
        return self.respuestas.pop(0)


def con_contexto(pregunta: str) -> str:
    return f"Curso: AGIL-4B (cursoId 1)\nPeríodo: semana 12 de 15.\nTramo de la pantalla: todo el ciclo. Las cifras son de todo el ciclo.\n{pregunta}"


# --- Tramo ---

def test_con_tramo_solo_en_calificaciones_y_alertas():
    assert graph.con_tramo("obtener_calificaciones", {"cursoId": 1}, "u2") == {"cursoId": 1, "tramo": "u2"}
    assert graph.con_tramo("identificar_alertas", {"cursoId": 1}, "p1") == {"cursoId": 1, "tramo": "p1"}
    assert graph.con_tramo("consultar_estado_periodo", {"cursoId": 1}, "u2") == {"cursoId": 1}
    assert graph.con_tramo("obtener_calificaciones", {"cursoId": 1}, "todo") == {"cursoId": 1}


@pytest.mark.parametrize("tramo", ["x1", "u0", "semana", "p3"])
def test_tramo_invalido_se_rechaza(tramo):
    with pytest.raises(ValidationError):
        ChatPeticion(mensaje="hola", token="t", tramo=tramo)


# --- Enrutador ---

@pytest.mark.parametrize("pregunta,ruta", [
    ("¿Qué cursos tengo asignados?", DATOS),
    ("¿Quiénes tienen promedio bajo 7?", DATOS),
    ("¿Cómo va E-005?", DATOS),
    ("¿En qué temas falla el curso en la Tarea 3?", DATOS),
    ("Resume el estado del curso: promedio, quiénes requieren atención, temas donde falla y qué viene.", DATOS),
    ("¿Ya puedes generar tareas de refuerzo?", APLICACION),
    ("¿Qué puedes hacer?", APLICACION),
    ("¿Quién eres?", APLICACION),
    ("¿Cómo descargo el plan en PDF?", APLICACION),
    ("¿Qué es Scrum?", GENERAL),
    ("Explícame la diferencia entre Scrum y Kanban", GENERAL),
    ("Dame ideas de actividades para enseñar historias de usuario", GENERAL),
    ("¿Cuál es la capital de Francia?", GENERAL),
    ("Hola", GENERAL),
    ("Gracias", GENERAL),
    ("Explícame qué es Scrum y dime quién tiene más atrasos", MIXTO),
])
def test_clasificar(pregunta, ruta):
    assert clasificar(pregunta) == ruta
    assert ruta_de(con_contexto(pregunta)) == ruta


def test_la_pregunta_se_separa_del_contexto():
    assert pregunta_del_docente(con_contexto("¿Qué es Scrum?")) == "¿Qué es Scrum?"
    # «Curso:» de la línea de contexto no hace que la pregunta parezca de datos.
    assert ruta_de("Curso: AGIL-4B (cursoId 1)\nFoco: estudiante E-005 (estudianteId 8).\nHola") == GENERAL


def test_plan_de_refuerzo_siempre_va_por_datos():
    mensaje = "Curso: AGIL-4B (cursoId 1)\nFoco: plan de refuerzo (refuerzoId 7) de Tarea 3, temas Historias de usuario.\nRedacta el borrador del plan de refuerzo."
    assert ruta_de(mensaje) == DATOS


def test_parece_dato_del_curso():
    assert parece_dato_del_curso("E-005 tiene 6,42.")
    assert parece_dato_del_curso("El promedio es 5.8")
    assert not parece_dato_del_curso("Se aprueba con 7,0 y el grupo alto empieza en 8,5.")
    assert not parece_dato_del_curso("Scrum tiene 3 roles y 5 eventos.")


# --- Redacción ---

def test_decimales_con_coma_sin_tocar_versiones_ni_fechas():
    assert con_coma("Promedio 6.42 y Moodle 4.5.15 el 2026-11-28") == "Promedio 6,42 y Moodle 4.5.15 el 2026-11-28"


def test_pulir_quita_ids_herramientas_y_cierre():
    texto = (
        "Tienes 1 curso asignado: AGIL-4B (cursoId 1), con promedio 7.42 según obtener_calificaciones.\n\n"
        "¿Hay algo más en lo que pueda ayudarte?"
    )
    assert pulir(texto) == "Tienes 1 curso asignado: AGIL-4B, con promedio 7,42 según las calificaciones del curso."


def test_pulir_quita_la_frase_cortada_antes_del_cierre():
    texto = "Para esa pregunta no necesito datos del curso.\n\nPara poder ayudarte mejor con el curso AGIL-4B, ¿necesitas algo más?"
    assert pulir(texto) == "Para esa pregunta no necesito datos del curso."
    # Una cifra con coma antes del cierre no se pierde.
    assert pulir("E-005 tiene 4,25, ¿necesitas algo más?") == "E-005 tiene 4,25"


@pytest.mark.parametrize("texto,esperado", [
    ("Hay 3 estudiantes en riesgo académico.", "Hay 3 estudiantes que requieren atención."),
    ("E-005 está en riesgo.", "E-005 requiere atención."),
    ("Ellos están en riesgo académico.", "Ellos requieren atención."),
    ("Los alumnos con riesgo son E-001 y E-002.", "Los alumnos que requieren atención son E-001 y E-002."),
])
def test_pulir_cambia_en_riesgo_por_requiere_atencion(texto, esperado):
    assert pulir(texto) == esperado
    assert "riesgo" not in pulir(texto)


def test_pulir_cifras_usa_la_misma_coma_del_texto():
    assert pulir_cifras([{"valor": "6.42", "verificada": True}]) == [{"valor": "6,42", "verificada": True}]


# --- Grafo y /chat ---

def test_prompt_por_ruta():
    assert graph.prompt_de(None) == graph.PROMPT_SISTEMA
    assert graph.prompt_de("datos") == graph.PROMPT_SISTEMA
    assert "Plan de refuerzo" in graph.prompt_de("aplicacion")
    assert "listar_cursos_autorizados" not in graph.prompt_de("aplicacion")
    assert "listar_cursos_autorizados" not in graph.prompt_de("general")
    assert "parte general" in graph.prompt_de("mixto")
    assert "de forma breve" not in graph.PROMPT_SISTEMA


def test_pregunta_de_la_aplicacion_va_sin_herramientas(monkeypatch):
    desnudo = Modelo(AIMessage(content="Sí. En «Plan de refuerzo» eliges la actividad, los temas y los estudiantes. ¿Necesitas algo más?"))
    con_herr = Modelo(sin_herramientas=desnudo)
    monkeypatch.setattr(graph, "llm", con_herr)
    monkeypatch.setattr(graph, "llm_externo", None)

    r = client.post("/chat", json={"mensaje": con_contexto("¿Ya puedes generar tareas de refuerzo?"), "token": "t"})

    assert r.status_code == 200
    cuerpo = r.json()
    assert cuerpo["herramientas"] == []
    assert cuerpo["proveedor"] == "LOCAL"
    assert "Plan de refuerzo" in cuerpo["respuesta"]
    assert "¿Necesitas algo más?" not in cuerpo["respuesta"]
    assert con_herr.recibidos == []  # se usó el modelo sin herramientas
    assert "Guía de EduMonitor" in desnudo.recibidos[0][0].content
    # Sin herramientas, el modelo no recibe el tramo, el período ni el id del curso: solo el curso y la pregunta.
    pregunta = desnudo.recibidos[0][-1].content
    assert pregunta == "Curso: AGIL-4B\n¿Ya puedes generar tareas de refuerzo?"


def test_contexto_minimo():
    assert contexto_minimo(con_contexto("¿Qué es Scrum?")) == "Curso: AGIL-4B\n¿Qué es Scrum?"
    assert contexto_minimo("Hola") == "Hola"


def test_concepto_de_la_materia_va_sin_herramientas():
    assert ruta_de(con_contexto("¿Qué diferencia hay entre gestión de configuración y control de versiones?")) == GENERAL
    assert clasificar("¿Cuáles son las diferencias entre Scrum y XP?") == GENERAL


def test_guia_pide_si_o_no_y_no_inventa_tarjetas():
    assert "«Sí» o «No»" in GUIA_APP
    assert "No hay tarjetas en la barra lateral" in GUIA_APP


def test_pregunta_general_que_inventa_notas_se_repite_por_datos(monkeypatch):
    desnudo = Modelo(AIMessage(content="E-005 tiene 4,25."))
    con_herr = Modelo(AIMessage(content="No hay datos que consultar para esa pregunta."), sin_herramientas=desnudo)
    monkeypatch.setattr(graph, "llm", con_herr)
    monkeypatch.setattr(graph, "llm_externo", None)

    r = client.post("/chat", json={"mensaje": con_contexto("Hola"), "token": "t"})

    assert r.status_code == 200
    assert len(desnudo.recibidos) == 1 and len(con_herr.recibidos) == 1
    assert con_herr.recibidos[0][0].content == graph.PROMPT_SISTEMA


def test_fuera_de_alcance_sigue_antes_del_enrutador():
    r = client.post("/chat", json={"mensaje": con_contexto("¿Tiene ansiedad Ana Acosta?"), "token": "t"})
    assert r.status_code == 200
    assert r.json()["proveedor"] == "NINGUNO"


@pytest.mark.parametrize("pregunta", [
    "¿Qué cursos tengo asignados?", "¿Qué notas tiene E-005?", "¿Cómo le va a E-011?", "¿Quiénes tienen promedio bajo 6?",
    "¿Qué estudiantes de BDD-4B tienen promedio bajo 7?", "¿Qué estudiantes requieren atención?",
    "¿Quién tiene más actividades sin entregar?", "¿Qué actividades le faltan entregar a E-007?",
    "¿En cuál de esas actividades le fue peor?", "¿Qué nota necesita para llegar a 7,0?",
    "De esos estudiantes, ¿en qué temas falla el curso?", "¿Cuántos de ellos tienen actividades sin entregar?",
    "¿Y cuándo vence lo que le falta?",
])
def test_preguntas_de_la_evaluacion_van_por_datos(pregunta):
    """Las 18 preguntas de «Calidad del asistente» no cambian: las que esperan herramientas siguen yendo por datos."""
    assert ruta_de(con_contexto(pregunta)) == DATOS
