import asyncio
import logging
import os
import time

import httpx

from app.schemas.tools import CursosAutorizadosRespuesta, TrazaHerramienta

logger = logging.getLogger("agent")
BACKEND_URL = os.getenv("BACKEND_URL", "http://localhost:5000")
LIMITE_SEGUNDOS = 5


async def consultar_backend(
    nombre: str, ruta: str, parametros: dict, token: str
) -> tuple[dict, TrazaHerramienta]:
    inicio = time.perf_counter()
    estado = "ERROR"
    try:
        async with asyncio.timeout(LIMITE_SEGUNDOS):
            async with httpx.AsyncClient(timeout=LIMITE_SEGUNDOS) as client:
                respuesta = await client.get(
                    f"{BACKEND_URL}/api/tools/{ruta}",
                    params=parametros,
                    headers={"Authorization": f"Bearer {token}"},
                )
                respuesta.raise_for_status()
                datos = respuesta.json()
                if not isinstance(datos, dict):
                    raise ValueError("Se esperaba un objeto JSON")
                if ruta == "cursos-autorizados":
                    datos = CursosAutorizadosRespuesta.model_validate(datos).model_dump(mode="json")
                estado = "OK"
    except httpx.HTTPStatusError as exc:
        logger.warning("Falló %s con HTTP %s", nombre, exc.response.status_code)
        datos = {"error": "sin_acceso" if exc.response.status_code == 403 else "consulta_fallida"}
    except (TimeoutError, httpx.TimeoutException):
        datos = {"error": "tiempo_agotado"}
    except (httpx.RequestError, ValueError):
        logger.warning("No se pudo obtener una respuesta válida para %s", nombre)
        datos = {"error": "consulta_fallida"}

    return datos, TrazaHerramienta(
        nombre=nombre,
        parametros=parametros,
        estado=estado,
        duracionMs=int((time.perf_counter() - inicio) * 1000),
    )
