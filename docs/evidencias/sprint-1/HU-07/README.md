# HU-07 · Spike del agente con FastAPI, LangGraph y Ollama

**Responsable:** Jeremy Guamán · **Estado:** Falta

**Evidencia que pide la planificación (6.3):** modelo local respondiendo con una herramienta conectada.

| Archivo | Quién lo sube | Contenido |
|---|---|---|
| `TA-07.1_modelos_y_tiempos.md` | Jeremy | Los dos modelos probados con Ollama y sus tiempos de respuesta en el equipo del grupo |
| `HU-07_agente_herramienta.png` | Jeremy | `POST /chat` respondiendo «¿Qué cursos tengo asignados?» con la traza de `listar_cursos_autorizados` |
| `HU-07_herramienta_falla.png` | Jeremy | Respuesta del agente con el backend detenido: informa el error sin inventar cursos |

La herramienta del backend ya está en `develop`: `GET /api/tools/cursos-autorizados` (PR #3).
