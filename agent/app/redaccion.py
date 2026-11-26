"""C7 · Ajustes de redacción del texto final, después de verificar las cifras. Código determinista, sin modelo.

- Decimales con coma, como se escriben en Ecuador (6.1 → 6,1). Las cifras verificadas se cambian igual para que la pantalla las
  siga encontrando en el texto.
- Sin identificadores internos (cursoId, estudianteId…) ni nombres de herramientas: el docente no los usa.
- Sin la pregunta de cierre genérica («¿Hay algo más en lo que pueda ayudarte?») ni la frase cortada que la introduce.
- «Requiere atención» en lugar de «en riesgo».
"""

import re

# Un número con punto decimal que no es parte de una palabra, una versión (4.5.15) ni una fecha.
_DECIMAL_CON_PUNTO = re.compile(r"(?<![\w.,])(\d+)\.(\d+)(?![\w]|[.,]\d)")

_IDS = r"(?:cursoId|estudianteId|actividadId|refuerzoId|conversacionId|hiloId)"
_ID_ENTRE_PARENTESIS = re.compile(r"\s*\((?:[^()]*?\b" + _IDS + r"\b[^()]*)\)")
_ID_SUELTO = re.compile(r",?\s*\b(?:con\s+)?(?:el\s+)?" + _IDS + r"\s*[:=]?\s*\d+\b")

HERRAMIENTAS_EN_PALABRAS = {
    "listar_cursos_autorizados": "tus cursos asignados",
    "obtener_calificaciones": "las calificaciones del curso",
    "identificar_alertas": "las alertas del curso",
    "consultar_estado_periodo": "el estado del período",
    "calcular_nota_necesaria": "la nota necesaria",
    "obtener_desempeno_tematico": "el desempeño por tema",
    "obtener_detalle_actividad": "el panel de la actividad",
    "consultar_refuerzos_aplicados": "los refuerzos aplicados",
    "obtener_plan_refuerzo": "el plan de refuerzo",
}
_HERRAMIENTA = re.compile(r"`?\b(" + "|".join(HERRAMIENTAS_EN_PALABRAS) + r")\b`?")

# La pregunta de cierre y, si la hay, la frase que la introduce en la misma línea («Para ayudarte mejor, ¿podrías…?»).
_CIERRE_GENERICO = re.compile(
    r"\n*[ \t]*(?:\b(?:para|si)\b[^\n.!?¿]*,[ \t]*)?¿[^?\n]*(algo m[aá]s|ayudarte|ayudar en|te puedo ayudar|necesitas|quieres que|te gustar[ií]a|deseas|otra (pregunta|consulta))[^?\n]*\?\s*$",
    re.IGNORECASE,
)

# La aplicación dice «Requiere atención», nunca «en riesgo».
_ESTA_EN_RIESGO = re.compile(r"\best[aá](n?)\s+(?:en|con)\s+riesgo(?:\s+acad[eé]mico)?\b", re.IGNORECASE)
_EN_RIESGO = re.compile(r"\b(\w+)\s+(?:en|con)\s+riesgo(?:\s+acad[eé]mico)?\b", re.IGNORECASE)


def _requiere_atencion(m: re.Match) -> str:
    palabra = m.group(1)
    verbo = "requieren" if palabra.lower().endswith("s") else "requiere"
    return f"{palabra} que {verbo} atención"


def con_coma(texto: str) -> str:
    return _DECIMAL_CON_PUNTO.sub(r"\1,\2", texto or "")


def pulir(texto: str) -> str:
    """Texto final para el docente."""
    salida = _ID_ENTRE_PARENTESIS.sub("", texto or "")
    salida = _ID_SUELTO.sub("", salida)
    salida = _HERRAMIENTA.sub(lambda m: HERRAMIENTAS_EN_PALABRAS[m.group(1)], salida)
    salida = _ESTA_EN_RIESGO.sub(lambda m: "requieren atención" if m.group(1) else "requiere atención", salida)
    salida = _EN_RIESGO.sub(_requiere_atencion, salida)
    sin_cierre = _CIERRE_GENERICO.sub("", salida).rstrip().rstrip(",;:").rstrip()
    if sin_cierre:
        salida = sin_cierre
    return con_coma(salida).strip()


def pulir_cifras(cifras: list[dict]) -> list[dict]:
    """Las cifras con el mismo formato del texto pulido."""
    return [{**c, "valor": con_coma(c["valor"])} if isinstance(c.get("valor"), str) else c for c in cifras]
