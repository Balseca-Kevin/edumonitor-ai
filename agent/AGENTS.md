# Agente EduMonitor · reglas del servicio de IA

Python 3.11, FastAPI, LangGraph, Pydantic, Ollama, pytest. Corre en el contenedor `agent` y solo lo llama la API por la red interna (`http://agent:8000`).

## Qué puede y qué no

- Solo llama a `/api/tools/*` de la API (`BACKEND_URL`) y a Ollama (`OLLAMA_BASE_URL`, modelo `OLLAMA_MODEL`).
- Nunca lee la base, nunca conoce el token de Moodle, nunca calcula promedios ni alertas.
- Usa el `token` que llega en `POST /chat`. Es un token de servicio de la API con rol `AGENTE`, atado a un docente y a un curso.

## Herramientas del Sprint 2

| Herramienta | Endpoint | Parámetros |
|---|---|---|
| `listar_cursos_autorizados` | `GET /api/tools/cursos-autorizados` | ninguno |
| `obtener_calificaciones` | `GET /api/tools/calificaciones` | `cursoId`, `estudianteId` opcional |
| `identificar_alertas` | `GET /api/tools/alertas` | `cursoId` |

Los parámetros van en camelCase, igual que en `docs/contratos-api.md`, para que la traza muestre los mismos nombres que la petición a la API.

Cada herramienta: parámetros validados con Pydantic, tiempo máximo de 5 s, y una traza (`nombre`, `parametros`, `estado` OK o ERROR, `duracionMs`) que se devuelve en la respuesta de `/chat`.

## Comportamiento del modelo

- Responde solo con datos que devolvieron las herramientas.
- Separa hechos, inferencias y recomendaciones.
- Si una herramienta falla, dice qué no pudo consultar y no inventa notas, estudiantes ni cursos.
- Si una herramienta responde 403, dice que no tiene acceso a ese curso.
- Usa «Requiere atención», nunca «reprobará» ni «en riesgo».
- Límite de iteraciones del grafo: 4.

## Endpoints

- `POST /chat` y `GET /salud`, con los JSON de `docs/contratos-api.md`.

## Pruebas

`python -m pytest` desde `agent/`. Las herramientas se prueban con `respx` simulando la API: respuesta correcta, 403, 500 y tiempo agotado. La evaluación de 10 preguntas va en `docs/evidencias/sprint-2/HU-13/evaluacion.md`.
