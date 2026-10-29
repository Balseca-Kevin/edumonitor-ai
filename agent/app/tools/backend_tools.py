import httpx
import os
import time
import logging
from app.schemas.tools import CursosAutorizadosRespuesta, TrazaHerramienta

logger = logging.getLogger("agent")
BACKEND_URL = os.getenv("BACKEND_URL", "http://localhost:5000")

async def consultar_cursos_backend(token: str) -> tuple[dict, TrazaHerramienta]:
    """Llama al endpoint GET /api/tools/cursos-autorizados del backend con el JWT."""
    headers = {"Authorization": f"Bearer {token}"}
    url = f"{BACKEND_URL}/api/tools/cursos-autorizados"
    inicio = time.time()
    
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            response = await client.get(url, headers=headers)
            duracion = int((time.time() - inicio) * 1000)
            
            if response.status_code == 200:
                data = CursosAutorizadosRespuesta.model_validate(response.json())
                traza = TrazaHerramienta(
                    nombre="listar_cursos_autorizados",
                    parametros={},
                    estado="OK",
                    duracionMs=duracion
                )
                return data.model_dump(mode="json"), traza
            else:
                logger.warning(f"Backend devolvió estado {response.status_code}")
                traza = TrazaHerramienta(
                    nombre="listar_cursos_autorizados",
                    parametros={},
                    estado="ERROR",
                    duracionMs=duracion
                )
                return {"error": f"Estado HTTP {response.status_code}"}, traza
                
    except Exception as e:
        duracion = int((time.time() - inicio) * 1000)
        logger.error(f"Falla al conectar con backend: {str(e)}")
        traza = TrazaHerramienta(
            nombre="listar_cursos_autorizados",
            parametros={},
            estado="ERROR",
            duracionMs=duracion
        )
        return {"error": "Conexión fallida"}, traza