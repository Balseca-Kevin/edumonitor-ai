"""C7 · Enrutador de la pregunta, con reglas y sin modelo.

Corre después de las reglas de fuera de alcance. Decide si la pregunta necesita los datos del curso:

- ``datos``: notas, promedios, alertas, entregas, fechas del período, estudiantes (E-001) o cursos asignados. Va con herramientas.
- ``aplicacion``: qué hace el asistente o cómo se usa EduMonitor. Va sin herramientas, con la guía de la aplicación.
- ``general``: saludos y preguntas generales del docente (conceptos de la materia, ideas, redacción). Va sin herramientas.
- ``mixto``: una parte general y una de datos en la misma pregunta. Va con herramientas y responde las dos partes.

En la duda, ``datos``: con herramientas el modelo todavía puede responder una pregunta general, y sin ellas no puede consultar.
"""

import re
import unicodedata

DATOS, APLICACION, GENERAL, MIXTO = "datos", "aplicacion", "general", "mixto"


def _normalizar(texto: str) -> str:
    sin_tildes = "".join(c for c in unicodedata.normalize("NFD", texto or "") if unicodedata.category(c) != "Mn")
    return " ".join(sin_tildes.lower().split())


# Códigos de estudiante y nombres de actividades del curso: siempre son datos.
_ENTIDAD = re.compile(r"\be-\d{3}\b|\b(tarea|taller|ape|examen parcial|examen)\s*\d+\b")

# Lo que vive en los datos del curso.
_DATOS = re.compile(
    r"\b(notas?|promedios?|calificacion\w*|calificad\w*|alertas?|atras\w*|requieren? atencion|sin entreg\w*|entreg\w*|"
    r"estudiantes?|alumnos?|matriculad\w*|cursos? (tengo|asignad\w*|del docente)|mis cursos|"
    r"vence\w*|vencid\w*|semana del periodo|que semana|que viene|siguiente evaluacion|proxima evaluacion|"
    r"nota necesaria|llegar a 7|refuerzos? aplicad\w*|temas? (donde|en los que|que) (falla|fallan|fallo|fallaron)|"
    r"domina\w*|bajo 6|bajo 7|menos de 7|mas de 8|ceros?|por calificar|rubrica de la|criterios? de la tarea|"
    r"le fue|les fue|como va|como van|quienes? (tiene|tienen|esta|estan|sac\w*|no)|cuant[oa]s (estudiantes|alumnos|tienen|entregaron|faltan))\b"
)

# Preguntas sobre el asistente o la aplicación.
_APLICACION = re.compile(
    r"\b(que puedes hacer|que sabes hacer|en que (me )?puedes ayudar|quien eres|que eres|como te llamas|para que sirves?|"
    r"(ya )?puedes (generar|crear|hacer|redactar|descargar|exportar|armar|preparar)|"
    r"como (se|puedo|hago|veo|descargo|armo|apruebo|sincronizo|uso|funciona|cambio|ajusto|marco|abro)|"
    r"donde (veo|esta|estan|encuentro|se ve|se ven)|para que sirve|que significa \"?requiere atencion|"
    r"edumonitor|esta aplicacion|la aplicacion|pantalla|boton|plan de refuerzo|tareas de refuerzo|"
    r"umbrales?|sincroniz\w*|modelo local|informe del curso|libro de calificaciones|mapa del curso)\b"
)

# Preguntas generales: conceptos, saludos, ideas, redacción.
_GENERAL = re.compile(
    r"^(hola|buen[oa]s?( dias| tardes| noches)?|gracias|muchas gracias|ok|vale|perfecto|genial)\b|"
    r"\b(que es|que son|explica\w*|define|definicion|(que |cual es la |cuales son las )?diferencias? (hay )?entre|en que consiste|ideas? (de|para)|"
    r"ejemplos? de|dame (ideas|ejemplos)|como (enseno|ensenar|explico|explicar)|"
    r"capital de|cual es la (capital|diferencia)|quien (invento|creo|escribio|fue))\b"
)

# Pedidos de redacción. Con datos del curso («resume el estado del curso») son de datos, no mixtos.
_REDACCION = re.compile(r"\b(redacta\w*|resume\w*|resumen|traduce\w*|escribe\w*)\b")

# Líneas de contexto que la API pone antes de la pregunta del docente (AgenteService).
_CONTEXTO = ("Curso:", "Período:", "Periodo:", "Tramo de la pantalla:", "Foco:")


def pregunta_del_docente(mensaje: str) -> str:
    """La pregunta sin las líneas de contexto de la API."""
    lineas = [linea for linea in (mensaje or "").splitlines() if not linea.strip().startswith(_CONTEXTO)]
    return "\n".join(lineas).strip()


def contexto_minimo(mensaje: str) -> str:
    """Para las rutas sin herramientas: el curso sin su id y la pregunta. El tramo, el período y el foco solo sirven a los datos."""
    curso = next((linea.strip() for linea in (mensaje or "").splitlines() if linea.strip().startswith("Curso:")), "")
    curso = re.sub(r"\s*\(cursoId[^)]*\)", "", curso)
    pregunta = pregunta_del_docente(mensaje)
    return f"{curso}\n{pregunta}" if curso else pregunta


def ruta_de(mensaje: str) -> str:
    """Ruta del mensaje que llega a POST /chat. Un plan de refuerzo en el foco siempre va por datos."""
    if "refuerzoId" in (mensaje or ""):
        return DATOS
    return clasificar(pregunta_del_docente(mensaje))


def clasificar(mensaje: str) -> str:
    """Ruta de la pregunta: datos, aplicacion, general o mixto."""
    texto = _normalizar(mensaje)
    if not texto:
        return DATOS
    datos = bool(_ENTIDAD.search(texto) or _DATOS.search(texto))
    aplicacion = bool(_APLICACION.search(texto))
    concepto = bool(_GENERAL.search(texto))
    general = concepto or bool(_REDACCION.search(texto))

    if aplicacion and not _ENTIDAD.search(texto) and not re.search(r"\bquienes?\b|\bcuant[oa]s\b", texto):
        return APLICACION
    if datos and concepto:
        return MIXTO
    if datos:
        return DATOS
    if general:
        return GENERAL
    if aplicacion:
        return APLICACION
    return DATOS


# Una respuesta sin herramientas no puede traer notas ni estudiantes: si los trae, se inventaron y la pregunta va por datos.
_NOTA_INVENTADA = re.compile(r"\bE-\d{3}\b|(?<![\w.,])(?!(?:7|6|8)[.,](?:0|5)\b)\d{1,2}[.,]\d{1,2}(?![\w.,])")


def parece_dato_del_curso(respuesta: str) -> bool:
    """True si una respuesta sin herramientas trae un código de estudiante o una nota con decimales (salvo 7,0, 6,0 y 8,5)."""
    return bool(_NOTA_INVENTADA.search(respuesta or ""))
