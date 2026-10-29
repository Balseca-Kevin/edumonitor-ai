import httpx
import os
import time
import logging
from app.schemas.tools import CursosAutorizadosRespuesta, TrazaHerramienta

logger = logging.getLogger("agent")
BACKEND_URL = os.getenv("BACKEND_URL", "http://localhost:5000")
HERRAMIENTA = "listar_cursos_autorizados"


def _traza(estado: str, inicio: float) -> TrazaHerramienta:
    """Crea la traza de la llamada y la deja en el log. Nunca incluye el token."""
    traza = TrazaHerramienta(nombre=HERRAMIENTA, parametros={}, estado=estado, duracionMs=int((time.time() - inicio) * 1000))
    logger.info("Herramienta %s parametros %s estado %s duracion %d ms", traza.nombre, traza.parametros, traza.estado, traza.duracionMs)
    return traza


async def consultar_cursos_backend(token: str) -> tuple[dict, TrazaHerramienta]:
    """Llama al endpoint GET /api/tools/cursos-autorizados del backend con el JWT."""
    headers = {"Authorization": f"Bearer {token}"}
    url = f"{BACKEND_URL}/api/tools/cursos-autorizados"
    inicio = time.time()

    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            response = await client.get(url, headers=headers)

        if response.status_code == 200:
            # Una respuesta fuera del contrato lanza ValidationError y termina como ERROR: no llega al modelo.
            data = CursosAutorizadosRespuesta.model_validate(response.json())
            return data.model_dump(mode="json"), _traza("OK", inicio)

        logger.warning("Backend devolvió estado %s", response.status_code)
        return {"error": f"Estado HTTP {response.status_code}"}, _traza("ERROR", inicio)

    except Exception as e:
        logger.error("Falla al consultar el backend: %s", type(e).__name__)
        return {"error": "Conexión fallida"}, _traza("ERROR", inicio)
