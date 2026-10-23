import httpx
import os

BACKEND_URL = os.getenv("BACKEND_URL", "http://localhost:5000")

async def listar_cursos_autorizados(token: str) -> dict:
    """Llama al backend para obtener los cursos autorizados del usuario."""
    headers = {"Authorization": f"Bearer {token}"}
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            response = await client.get(f"{BACKEND_URL}/api/tools/listar_cursos_autorizados", headers=headers)
            if response.status_code == 200:
                return response.json()
            return {"success": False, "error": f"Error del backend: Estado {response.status_code}"}
    except Exception as e:
        # Si el backend está apagado o falla, NO inventa datos y reporta el error
        return {"success": False, "error": f"No se pudo conectar con el backend: {str(e)}"}