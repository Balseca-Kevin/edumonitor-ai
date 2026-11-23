"""TA-23.A3 · Preguntas fuera de alcance, resueltas con reglas antes de llamar al modelo.

La especificación deja fuera la evaluación clínica o psicológica, escribir en Moodle o modificar notas, los mensajes automáticos
a estudiantes y la predicción. El asistente dice que no puede y ofrece lo que muestran los datos.
"""

import re
import unicodedata


def _normalizar(texto: str) -> str:
    sin_tildes = "".join(c for c in unicodedata.normalize("NFD", texto or "") if unicodedata.category(c) != "Mn")
    return sin_tildes.lower()


_REGLAS = [
    (
        re.compile(
            r"\b(depresi\w*|ansiedad|ansios\w*|psicolog\w*|psiquiatr\w*|salud mental|trastorno\w*|tdah|autis\w*|"
            r"emocional\w*|estres\w*|suicid\w*|clinic\w*|terapi\w*|problemas? (familiar|personal)\w*)\b"
        ),
        "No puedo evaluar aspectos clínicos, psicológicos ni personales de un estudiante. Puedo mostrarte lo que dicen sus datos: "
        "sus entregas, los temas donde falla y la evolución de sus notas frente a 7,0. Pregúntame, por ejemplo, «¿En qué temas falla?».",
    ),
    (
        re.compile(
            r"\b(cambia(le)?|cambiar(le)?|modifica(le)?|modificar(le)?|sube(le)?|subir(le)?|baja(le)?|bajar(le)?|corrige(le)?|"
            r"corregir(le)?|ponle|ponerle|poner|edita(le)?|editar(le)?)\b"
            r".{0,40}\b(nota|notas|calificacion\w*|moodle)\b"
        ),
        "No puedo modificar notas ni escribir en Moodle: EduMonitor solo lee los datos del aula virtual. Puedo mostrarte la nota "
        "que tiene registrada y de qué actividades sale.",
    ),
    (
        re.compile(r"\b(envia(le|les)?|enviar(le|les)?|manda(le|les)?|mandar(le|les)?|notifica(le|les)?|notificar(le|les)?|escribele|escribirle)\b.{0,40}\b(correo|mensaje|email|estudiante\w*|alumn\w*)\b"),
        "No envío mensajes a los estudiantes. Puedo redactar un borrador de retroalimentación o de refuerzo que tú revisas, "
        "apruebas y descargas en PDF o DOCX.",
    ),
    (
        re.compile(r"\b(reprobara\w*|aprobara\w*|predic\w*|pronostic\w*|va a (reprobar|aprobar|perder|pasar))\b"),
        "No hago predicciones sobre si un estudiante aprobará. Puedo darte, con las notas actuales, la nota que necesita en lo "
        "que le queda para llegar a 7,0, o decirte si con esas notas ya no alcanza.",
    ),
]


def fuera_de_alcance(pregunta: str) -> str | None:
    """Respuesta fija si la pregunta está fuera de alcance; None si el asistente puede responderla con los datos."""
    texto = _normalizar(pregunta)
    for patron, respuesta in _REGLAS:
        if patron.search(texto):
            return respuesta
    return None
