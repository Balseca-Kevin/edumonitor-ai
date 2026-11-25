"""TA-23.A1 · Verificación de cifras: cada número de la respuesta tiene que estar en lo que devolvieron las herramientas del turno.

Código determinista, sin modelo. Para cada cifra devuelve la herramienta y el campo de donde sale y, si los hay, el estudiante
y la actividad de esa fila, para que la pantalla la enlace con el libro (TA-23.A2).
"""

import json
import re
from decimal import Decimal, ROUND_HALF_UP

# Cifras de la respuesta: 6,1 · 6.10 · 32 · 55 %. Los códigos E-001 y las siglas como AGIL-4B no cuentan.
_CIFRA = re.compile(r"(?<![\w.,-])(\d+(?:[.,]\d+)?)(\s*%)?(?![\w])")
_CODIGO = re.compile(r"\bE-\d{3}\b")
_NUMERACION = re.compile(r"^\s*\d+[.)]\s", re.MULTILINE)
_NUMERO_EN_TEXTO = re.compile(r"\d+(?:\.\d+)?")
# Números que son parte de un nombre: «Tarea 3», «unidades 1 a 3», «tareas 7, 8 y 9», «Examen Parcial 2», «U4». No son cifras.
_NOMBRE_NUMERADO = re.compile(
    r"\b(?:tareas?|talleres|taller|apes?|unidad(?:es)?|parcial(?:es)?|examen(?:es)?(?:\s+parcial(?:es)?)?|actividad(?:es)?|"
    r"sprints?|criterios?|preguntas?|u|p)\s*\d+(?:\s*(?:,|y|e|a|al|-|–)\s*\d+(?![.,]\d))*",
    re.IGNORECASE,
)
# Campos que identifican, no miden: ids, códigos y nombres. Un número de la respuesta no se verifica con ellos.
_CAMPOS_NO_CIFRA = {"id", "codigo", "moodleId", "nombreCorto", "nombre", "apellido", "sigla", "herramienta", "generado"}


def _campo_no_cifra(clave: str) -> bool:
    return clave in _CAMPOS_NO_CIFRA or clave.endswith("Id")

# Constantes del dominio que el asistente puede nombrar sin consultarlas: escala 10, aprobación 7,0, grupo bajo 6,0,
# ponderación 40/30/30 y las 3 notas de la tendencia.
CONSTANTES = {Decimal(v) for v in ("10", "7", "6", "8.5", "40", "30", "3", "100")}


def _decimal(texto: str) -> Decimal:
    return Decimal(texto.replace(",", "."))


def cifras_de_texto(texto: str) -> list[dict]:
    """Cifras de la respuesta en orden de aparición, sin repetir, con su número de decimales y si llevan %."""
    limpio = _CODIGO.sub(" ", texto or "")
    limpio = _NUMERACION.sub(" ", limpio)
    limpio = _NOMBRE_NUMERADO.sub(" ", limpio)
    vistas = set()
    cifras = []
    for coincidencia in _CIFRA.finditer(limpio):
        valor = coincidencia.group(1)
        porcentaje = bool(coincidencia.group(2))
        clave = (valor, porcentaje)
        if clave in vistas:
            continue
        vistas.add(clave)
        decimales = len(valor.split(",")[-1]) if "," in valor else (len(valor.split(".")[-1]) if "." in valor else 0)
        cifras.append({"texto": valor + ("%" if porcentaje else ""), "valor": _decimal(valor), "decimales": decimales, "porcentaje": porcentaje})
    return cifras


def _hojas(datos, ruta="", contexto=None):
    """Recorre el JSON y da (ruta, número, contexto) por cada número, también los que van dentro de un texto («Tarea 3»)."""
    contexto = dict(contexto or {})
    if isinstance(datos, dict):
        if "codigo" in datos and isinstance(datos.get("id"), int):
            contexto["estudianteId"] = datos["id"]
        if isinstance(datos.get("estudianteId"), int):
            contexto["estudianteId"] = datos["estudianteId"]
        if isinstance(datos.get("actividadId"), int):
            contexto["actividadId"] = datos["actividadId"]
        for clave, valor in datos.items():
            if _campo_no_cifra(clave):
                continue
            yield from _hojas(valor, f"{ruta}.{clave}" if ruta else clave, contexto)
    elif isinstance(datos, list):
        for i, valor in enumerate(datos):
            yield from _hojas(valor, f"{ruta}[{i}]", contexto)
    elif isinstance(datos, bool) or datos is None:
        return
    elif isinstance(datos, (int, float)):
        yield ruta, Decimal(str(datos)), contexto
    elif isinstance(datos, str):
        for numero in _NUMERO_EN_TEXTO.findall(_CODIGO.sub(" ", datos)):
            yield ruta, Decimal(numero), contexto


def _hojas_de_texto(datos):
    return ((ruta, numero, ctx) for ruta, numero, ctx in _hojas(datos) if isinstance(_valor_en(datos, ruta), str))


def _valor_en(datos, ruta: str):
    for parte in re.findall(r"[^.\[\]]+|\[\d+\]", ruta):
        try:
            datos = datos[int(parte[1:-1])] if parte.startswith("[") else datos[parte]
        except (KeyError, IndexError, TypeError, ValueError):
            return None
    return datos


def _coincide(cifra: dict, numero: Decimal) -> bool:
    decimales = cifra["decimales"]
    candidatos = [numero]
    if cifra["porcentaje"] or numero <= 1:
        candidatos.append(numero * 100)
    for candidato in candidatos:
        if candidato == cifra["valor"]:
            return True
        # Redondeo de un valor con decimales a los decimales que muestra la respuesta (6,42 → 6,4). Un entero solo coincide
        # con el mismo entero, salvo en porcentajes.
        if decimales > 0 or (cifra["porcentaje"] and candidato is not numero):
            paso = Decimal(1).scaleb(-decimales)
            if candidato.quantize(paso, rounding=ROUND_HALF_UP) == cifra["valor"]:
                return True
    return False


def verificar(respuesta: str, resultados: list[dict], permitidas: set[Decimal] | None = None) -> list[dict]:
    """Por cada cifra de la respuesta: valor, herramienta, campo, estudianteId, actividadId y si quedó verificada.

    `resultados` es la lista de {"herramienta": nombre, "datos": json} de las herramientas del turno. `permitidas` son las cifras
    de la pregunta y del historial, que el asistente puede repetir.
    """
    permitidas = (permitidas or set()) | CONSTANTES
    hojas = [(r["herramienta"], ruta, numero, ctx) for r in resultados for ruta, numero, ctx in _hojas(r["datos"])]
    # Primero los campos numéricos (notas, promedios, conteos); los números dentro de un texto quedan como último recurso.
    textos = {(r["herramienta"], ruta) for r in resultados for ruta, _, _ in _hojas_de_texto(r["datos"])}
    hojas.sort(key=lambda h: (h[0], h[1]) in textos)
    salida = []
    for cifra in cifras_de_texto(respuesta):
        fuente = next(((h, ruta, ctx) for h, ruta, numero, ctx in hojas if _coincide(cifra, numero)), None)
        if fuente is not None:
            herramienta, ruta, ctx = fuente
            salida.append({
                "valor": cifra["texto"], "herramienta": herramienta, "campo": ruta,
                "estudianteId": ctx.get("estudianteId"), "actividadId": ctx.get("actividadId"), "verificada": True,
            })
        elif cifra["valor"] in permitidas:
            continue
        else:
            salida.append({"valor": cifra["texto"], "herramienta": None, "campo": None, "estudianteId": None, "actividadId": None, "verificada": False})
    return salida


def numeros_permitidos(*textos: str) -> set[Decimal]:
    return {c["valor"] for texto in textos for c in cifras_de_texto(texto)}


def datos_de_herramienta(contenido: str):
    try:
        return json.loads(contenido)
    except (TypeError, ValueError):
        return {}
