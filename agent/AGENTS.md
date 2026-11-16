# Agente EduMonitor · reglas del servicio de IA

Python 3.11, FastAPI, LangGraph, Pydantic, pytest. Modelo externo con Ollama como respaldo desde el Sprint 3. Corre en el contenedor `agent` y solo lo llama la API por la red interna (`http://agent:8000`).

## Qué puede y qué no

- Solo llama a `/api/tools/*` de la API (`BACKEND_URL`), al modelo externo y a Ollama (`OLLAMA_BASE_URL`, modelo `OLLAMA_MODEL`).
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

## Sprint 3

### Modelo externo con respaldo local

El Product Owner autorizó un modelo externo con cautela, tolerancia a fallos y plan de respaldo. Reglas completas en «Modelo de IA (Sprint 3)» de `docs/CONTEXTO_IA.md`.

- El modelo principal es el externo; `qwen2.5:7b` en Ollama es el respaldo. El proveedor se elige por variables de entorno; sus nombres se agregan a `.env.example` en la tarea de HU-23 que lo implemente. La clave del proveedor nunca va en código, pruebas, logs ni trazas.
- El agente recibe del backend datos ya seudonimizados (E-001, E-002…). No pide ni reconstruye nombres.
- Pasa al modelo local ante error, tiempo agotado (20 s), 429 o respuesta sin el formato esperado; con 3 fallos en 5 minutos va directo al local durante 5 minutos. La respuesta de `/chat` dice qué modelo respondió.
- Con 4096 tokens de contexto en el modelo local, las herramientas devuelven datos resumidos. `num_ctx` sube a 8192 solo después de medir memoria y tiempo en la RTX 4050.

### Herramientas nuevas

Rutas y JSON en el bloque «Sprint 3» de `docs/contratos-api.md`. Ninguna se programa antes de que su contrato esté detallado.

| Herramienta | HU | Qué devuelve |
|---|---|---|
| `obtener_calificaciones` con `promedioMenorA` y orden por `sinEntrega` | HU-23 | Filtro y orden hechos en el backend (corrigen las preguntas 5, 6 y 9 de TA-13.7) |
| `obtener_detalle_actividad` | HU-22 | Enunciado, entregas, distribución y dominio por criterio de una actividad |
| `obtener_desempeno_tematico` | HU-08, HU-15 | Promedio por tema de un estudiante o del curso |
| `obtener_retroalimentacion` | HU-15 | Criterios de la rúbrica con el nivel obtenido y el comentario del docente |
| `guardar_tarea_refuerzo` | HU-15 | Borrador de refuerzo en estado BORRADOR (lo aprueba el docente) |
| `consultar_refuerzos_aplicados` | HU-15 (TA-15.A2) | Refuerzos aprobados con el promedio del tema antes y después |
| `calcular_nota_necesaria` | HU-10 (TA-10.A1) | Nota mínima para llegar a 7,0 con las notas actuales |

### Grafo

- El foco de la pantalla (curso, actividad, estudiante) y los últimos 4 turnos llegan en `POST /chat`; el modelo no busca ids en listas.
- Nodo de verificación de cifras después de redactar: cada número de la respuesta tiene que estar en lo que devolvieron las herramientas de ese turno; devuelve la herramienta y la fila de cada cifra (TA-23.A1).
- Preguntas clínicas o psicológicas: el asistente dice que no las puede evaluar y ofrece entregas, temas fallados y evolución (TA-23.A3).

### Evaluación

18 preguntas: las 10 de TA-13.7, 5 de seguimiento y 3 fuera de alcance. Se corren con los dos modelos desde la pantalla «Calidad del asistente» (TA-18.A1 y TA-18.A2), no desde la terminal.

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

`python -m pytest` desde `agent/`. Las herramientas se prueban con `respx` simulando la API: respuesta correcta, 403, 500 y tiempo agotado. La evaluación de 10 preguntas (TA-13.7) está en la sección «TA-13.7 · Evaluación en el sistema» de `docs/evidencias/sprint-2/HU-13/README.md`.
