"""HU-23 · Modelo externo con respaldo local (docs/CONTEXTO_IA.md, «Modelo de IA (Sprint 3)»).

- Principal: modelo externo, si está configurado con variables de entorno. Límite de 20 s por llamada.
- Respaldo: Ollama (qwen2.5:7b). Si el externo falla, la misma pregunta va al local.
- 3 fallos del externo en 5 minutos: 5 minutos directo al modelo local.

La clave del proveedor externo es un secreto: solo se lee de MODELO_EXTERNO_API_KEY y nunca se registra.
"""

import os
import threading
import time
from collections import deque

MODELO_LOCAL = os.getenv("OLLAMA_MODEL", "qwen2.5:7b")
OLLAMA_BASE_URL = os.getenv("OLLAMA_BASE_URL", "http://localhost:11434")

PROVEEDOR_EXTERNO = os.getenv("MODELO_EXTERNO_PROVEEDOR", "").strip().lower()
MODELO_EXTERNO = os.getenv("MODELO_EXTERNO", "").strip()
LIMITE_EXTERNO_SEGUNDOS = float(os.getenv("MODELO_EXTERNO_TIEMPO_SEGUNDOS", "20"))

FALLOS_PARA_PAUSA = 3
VENTANA_FALLOS_SEGUNDOS = 300
PAUSA_SEGUNDOS = 300

PROVEEDORES = {"anthropic"}


def externo_configurado() -> bool:
    """Hay modelo externo si el proveedor es conocido y hay modelo y clave. La clave no se devuelve."""
    return PROVEEDOR_EXTERNO in PROVEEDORES and bool(MODELO_EXTERNO) and bool(os.getenv("MODELO_EXTERNO_API_KEY", "").strip())


class Interruptor:
    """Cuenta los fallos del modelo externo. Con 3 fallos en 5 minutos pausa el externo 5 minutos."""

    def __init__(self, reloj=time.monotonic):
        self._reloj = reloj
        self._fallos: deque[float] = deque()
        self._pausa_hasta = 0.0
        self._candado = threading.Lock()

    def disponible(self) -> bool:
        with self._candado:
            return self._reloj() >= self._pausa_hasta

    def segundos_de_pausa(self) -> int:
        with self._candado:
            return max(0, int(self._pausa_hasta - self._reloj()))

    def registrar_fallo(self) -> None:
        with self._candado:
            ahora = self._reloj()
            self._fallos.append(ahora)
            while self._fallos and ahora - self._fallos[0] > VENTANA_FALLOS_SEGUNDOS:
                self._fallos.popleft()
            if len(self._fallos) >= FALLOS_PARA_PAUSA:
                self._pausa_hasta = ahora + PAUSA_SEGUNDOS
                self._fallos.clear()

    def registrar_exito(self) -> None:
        with self._candado:
            self._fallos.clear()


interruptor = Interruptor()


def crear_local(herramientas: list[dict]):
    from langchain_ollama import ChatOllama

    return ChatOllama(model=MODELO_LOCAL, base_url=OLLAMA_BASE_URL, temperature=0).bind_tools(herramientas)


def crear_externo(herramientas: list[dict]):
    """Modelo externo con llamadas a herramientas. Sin reintentos propios: un fallo pasa al respaldo local."""
    if not externo_configurado():
        return None
    if PROVEEDOR_EXTERNO == "anthropic":
        from langchain_anthropic import ChatAnthropic

        modelo = ChatAnthropic(
            model=MODELO_EXTERNO,
            api_key=os.environ["MODELO_EXTERNO_API_KEY"],
            temperature=0,
            max_tokens=2048,
            timeout=LIMITE_EXTERNO_SEGUNDOS,
            max_retries=0,
        )
        return modelo.bind_tools(herramientas)
    return None


def estado_modelos(solo_local: bool = False) -> dict:
    """Lo que ve «Estado del sistema»: los dos modelos y cuál responde ahora."""
    configurado = externo_configurado()
    pausa = interruptor.segundos_de_pausa()
    if not configurado:
        estado_externo = "NO_CONFIGURADO"
    elif pausa > 0:
        estado_externo = "EN_PAUSA"
    else:
        estado_externo = "OK"
    activo = "EXTERNO" if configurado and pausa == 0 and not solo_local else "LOCAL"
    return {
        "externo": {
            "proveedor": PROVEEDOR_EXTERNO or None,
            "modelo": MODELO_EXTERNO or None,
            "estado": estado_externo,
            "pausaSegundos": pausa,
        },
        "activo": activo,
    }
