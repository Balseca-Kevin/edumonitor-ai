# Sprint 2 · plan de trabajo

Re-planificación: domingo 8 nov 2026, después de la Review del Sprint 1. Review del Sprint 2: jueves 12 nov 2026. Corte de código: miércoles 11 nov, 23:00.

## Sprint Goal

El docente abre EduMonitor, ve el panel de indicadores de su curso y las alertas que requieren atención con su evidencia, y pregunta al asistente desde la misma aplicación; EduMonitor se levanta con un solo comando y el aula virtual corre aparte.

## Compromiso

6 HU · 34 tareas · 39 SP · 68 h.

| HU | Historia | SP | Responsable | Rama |
|---|---|---|---|---|
| HU-20 | Entorno único de ejecución con Docker Compose | 5 | Kevin | `feature/HU-20-docker-unico` |
| HU-19 | Identidad visual y pantallas de acceso | 5 | Santiago | `feature/HU-19-identidad-visual` |
| HU-09 | Panel de indicadores del curso | 8 | Kevin | `feature/HU-09-panel-indicadores` |
| HU-12 | Alertas académicas con reglas configurables | 5 | Sebastián | `feature/HU-12-alertas` |
| HU-13 | Herramientas del agente con trazas | 8 | Jeremy (TA-13.1 y TA-13.2: Kevin) | `feature/HU-13-herramientas-agente` |
| HU-14 | Consulta al asistente desde la aplicación | 8 | Kevin | `feature/HU-14-asistente-app` |

## Definition of Done del Sprint 2

Una HU está terminada cuando:

- Funciona dentro de la aplicación levantada con `docker compose up -d` en la raíz, con el aula virtual de `moodle-lab/` encendida aparte y sus datos de laboratorio.
- Cada criterio de aceptación se puede mostrar en pantalla, sin terminal, Postman, Swagger ni archivos.
- Tiene pruebas automatizadas que pasan en la integración continua.
- Respeta `docs/contratos-api.md` y, si toca la interfaz, `docs/sprint-2/diseno.md`.
- No expone secretos ni datos identificables.
- Está fusionada en `develop` por Kevin y su tarjeta está en «Finalizada» en Jira.
- Su responsable puede explicar qué pide la historia, qué tablas lee, qué endpoint llama la pantalla y qué prueba la cubre.

Cambio frente al Sprint 1: la revisión por pares se reemplaza por la aprobación de Kevin. Decisión del equipo para el Sprint 2. Todo Pull Request hacia `develop` o `main` necesita la aprobación de Kevin (CODEOWNERS). Kevin es el único que fusiona; sus propios Pull Request los fusiona con el bypass del ruleset, porque GitHub no deja aprobar el Pull Request propio.

## Dependencias que no se pueden mover

| Hora límite | Qué queda listo | Quién lo necesita |
|---|---|---|
| Lun 9, 12:00 | `docker-compose.yml` de EduMonitor en la raíz y `moodle-lab/` separado (TA-20.1 a 20.3) | Todo el equipo |
| Lun 9, 12:00 | Tokens y componentes base (TA-19.1) | Kevin (panel y asistente), Sebastián (pantalla de alertas) |
| Lun 9, 18:00 | Servicio de indicadores y endpoint (TA-09.1 a 09.3) | Sebastián (TA-12.1) |
| Lun 9, 18:00 | `GET /api/tools/calificaciones` (TA-13.1) | Jeremy (TA-13.3) |
| Mar 10, 18:00 | Alertas y su endpoint (TA-12.3) | Kevin (TA-13.2), panel (tarjeta «Requiere atención») |
| Mié 11, 09:00 | `GET /api/tools/alertas` (TA-13.2) | Jeremy (TA-13.4) |

Mientras una dependencia no esté, se trabaja contra el contrato con datos simulados.

## Tareas

### HU-20 · Entorno único de ejecución con Docker Compose (Kevin)

| TA | Tarea | h | Límite | Archivos |
|---|---|---|---|---|
| TA-20.1 | Dockerfile multi-etapa de la API y del frontend (build de Vite + nginx con proxy `/api` → `api:5000`). `frontend/src/services/api.js` usa `VITE_API_URL ?? ''` para que en Docker la app llame a `/api` en la misma dirección | 2 | Lun 9, 12:00 | `backend/Dockerfile`, `frontend/Dockerfile`, `frontend/nginx.conf`, `.dockerignore`, `frontend/src/services/api.js` |
| TA-20.2 | Dockerfile del agente y servicio `ollama` con GPU y descarga de `qwen2.5:7b` | 2 | Lun 9, 12:00 | `agent/Dockerfile`, `docker-compose.yml` |
| TA-20.3 | `docker-compose.yml` de EduMonitor en la raíz (`name: edumonitor`: web, api, sqlserver, agent, ollama) y `moodle-lab/docker-compose.yml` solo con moodle y moodle-db (`name: aula-virtual`); healthchecks, `depends_on` con `service_healthy`, mismos volúmenes | 2 | Lun 9, 12:00 | `docker-compose.yml`, `moodle-lab/docker-compose.yml`, `.env.example` |
| TA-20.4 | Configuración de Moodle en la tabla `ConfiguracionMoodle`, token protegido con Data Protection y llaves en el volumen `api-keys` | 2 | Mar 10 | `backend/EduMonitor.Domain`, `backend/EduMonitor.Infrastructure/Moodle`, migración |
| TA-20.5 | Pantalla «Estado del sistema» (`/admin/estado`) y `GET /api/admin/estado` | 2 | Mié 11 | `backend/EduMonitor.Api/Controllers`, `frontend/src/pages/admin/EstadoSistema.jsx` |
| TA-20.6 | README de arranque con un comando y prueba de reinicio | 1 | Mié 11 | `README.md` |

Notas de TA-20.3:
- Dos entornos, como pide la especificación: EduMonitor es independiente de Moodle y el Moodle de laboratorio ocupa el lugar del institucional. Ningún servicio de la raíz tiene `depends_on` hacia `moodle-lab/`.
- EduMonitor y su agente se usan en una sola dirección, `http://localhost:5173`. `agent` y `ollama` no publican puertos; solo la API llama al agente por la red interna.
- Hoy los dos entornos comparten el proyecto `edumonitor` por `COMPOSE_PROJECT_NAME` en `.env`. Se quita esa variable de `.env.example` (Kevin la quita a mano de su `.env`) y cada archivo declara su `name:`. Cada volumen se declara con su nombre completo (`name: edumonitor_sqlserver-data`, `name: edumonitor_moodledata`, `name: edumonitor_moodle-db-data`) para reutilizar los datos actuales. Comprobar con `docker volume ls` antes del primer `up`.
- Antes del primer `up` nuevo, Kevin apaga el entorno viejo con `docker compose -f moodle-lab/docker-compose.yml --env-file .env down`, sin `-v`.
- Dentro de Docker, las variables de EduMonitor cambian de `localhost` a nombres de servicio: `ConnectionStrings__EduMonitorDB` con `Server=sqlserver,1433`, `BACKEND_URL=http://api:5000`, `OLLAMA_BASE_URL=http://ollama:11434`, `Agente__Url=http://agent:8000`. Moodle se llama desde fuera de su entorno: `Moodle__Url=http://host.docker.internal:8080`.
- Si Moodle rechaza las llamadas de la API porque su `SITE_URL` es `http://localhost:8080`, detente y explícale el ajuste a Kevin antes de aplicarlo.
- GPU: `deploy.resources.reservations.devices` con `driver: nvidia`. Si Docker Desktop no ve la GPU, el servicio `ollama` va en un perfil `gpu` y el agente usa `OLLAMA_BASE_URL=http://host.docker.internal:11434` con Ollama de Windows.
- `scripts/iniciar-sistema.bat` se reduce a levantar los dos entornos, en orden, y abrir `http://localhost:5173`.

### HU-19 · Identidad visual y pantallas de acceso (Santiago)

| TA | Tarea | h | Límite | Archivos |
|---|---|---|---|---|
| TA-19.1 | Tokens y componentes base: botón, campo, tarjeta, tabla, insignia de estado, aviso | 3 | Lun 9, 12:00 | `frontend/src/styles/tokens.css`, `frontend/src/components/ui/` |
| TA-19.2 | Estructura: barra lateral por curso (Panel, Alertas, Asistente) y cabecera con la tira del período | 2 | Lun 9, 18:00 | `frontend/src/components/Layout.jsx`, `frontend/src/components/TiraPeriodo.jsx` |
| TA-19.3 | Rediseño de inicio de sesión | 2 | Mar 10 | `frontend/src/pages/Login.jsx` |
| TA-19.4 | Rediseño de Mis cursos y configuración de Moodle | 2 | Mar 10 | `frontend/src/pages/Cursos.jsx`, `frontend/src/pages/ConfiguracionMoodle.jsx` |
| TA-19.5 | Pantallas «Acceso no permitido» (403) y «Tu sesión venció» (401), conectadas al interceptor de Axios | 1 | Mié 11 | `frontend/src/pages/AccesoNoPermitido.jsx`, `frontend/src/pages/SesionVencida.jsx`, `frontend/src/services/api.js` |
| TA-19.6 | Pruebas de interfaz actualizadas | 1 | Mié 11 | `frontend/src/**/*.test.jsx` |

### HU-09 · Panel de indicadores del curso (Kevin)

| TA | Tarea | h | Límite | Archivos |
|---|---|---|---|---|
| TA-09.1 | `IndicadorService`: promedio ponderado 40/30/30 con normalización, media, mediana, desviación muestral, grupos, z, finalización, tendencia (N = 3), distribución y resumen por unidad | 3 | Lun 9, 18:00 | `backend/EduMonitor.Application/Indicadores/` |
| TA-09.2 | Pruebas unitarias con casos conocidos (9, 8, 6, 3, 3 → 5,80 y 2,77; categoría faltante; cero frente a sin entrega) | 2 | Lun 9, 18:00 | `backend/EduMonitor.Tests/Indicadores/` |
| TA-09.3 | `GET /api/cursos/{id}/indicadores` con autorización por curso y `noDisponibles` | 2 | Lun 9, 18:00 | `backend/EduMonitor.Api/Controllers/IndicadoresController.cs` |
| TA-09.4 | Pantalla «Panel del curso» en bento grid (ver `diseno.md`) con Recharts | 4 | Mar 10 | `frontend/src/pages/curso/Panel.jsx`, `frontend/src/services/indicadores.js` |
| TA-09.5 | Prueba de interfaz del panel, incluido «no disponible» | 1 | Mié 11 | `frontend/src/pages/curso/Panel.test.jsx` |

### HU-12 · Alertas académicas con reglas configurables (Sebastián)

| TA | Tarea | h | Límite | Archivos |
|---|---|---|---|---|
| TA-12.1 | Reglas R1, R2 y R4 como clases deterministas sobre `IndicadorService` | 3 | Mar 10, 12:00 | `backend/EduMonitor.Application/Alertas/` |
| TA-12.2 | `GET/PUT /api/cursos/{id}/reglas` con umbrales por defecto | 2 | Mar 10 | `backend/EduMonitor.Api/Controllers/AlertasController.cs` |
| TA-12.3 | `GET /api/cursos/{id}/alertas`, `PATCH .../alertas/{alertaId}`, recálculo tras sincronizar y tras cambiar reglas, migración de `Alerta` | 2 | Mar 10, 18:00 | mismo controlador, `SincronizacionService`, migración |
| TA-12.4 | Pantalla «Alertas»: lista con evidencia por regla, marcar revisada y panel de umbrales | 3 | Mié 11 | `frontend/src/pages/curso/Alertas.jsx` |
| TA-12.5 | Pruebas de cada regla: un caso que la activa y uno que no | 2 | Mié 11 | `backend/EduMonitor.Tests/Alertas/` |

### HU-13 · Herramientas del agente con trazas (Jeremy; TA-13.1 y 13.2 de Kevin)

| TA | Tarea | h | Límite | Archivos |
|---|---|---|---|---|
| TA-13.1 | `GET /api/tools/calificaciones` (Kevin) | 1 | Lun 9, 18:00 | `ToolsController.cs` |
| TA-13.2 | `GET /api/tools/alertas` (Kevin) | 1 | Mié 11, 09:00 | `ToolsController.cs` |
| TA-13.3 | Herramienta `obtener_calificaciones` con parámetros validados por Pydantic | 2 | Mar 10 | `agent/app/tools/`, `agent/app/schemas/` |
| TA-13.4 | Herramienta `identificar_alertas` | 1 | Mié 11, 12:00 | `agent/app/tools/` |
| TA-13.5 | Mensaje de sistema: usar herramientas, no inventar, separar hechos, inferencias y recomendaciones, decir cuando no hay acceso. Parte del mensaje de sistema de 07da04c (rama `fix/HU-07-prompt-sistema`), que Kevin fusiona en `develop` antes del lunes 9 | 2 | Mar 10 | `agent/app/graph.py` |
| TA-13.6 | Traza de cada llamada (herramienta, parámetros, estado, duración) devuelta en la respuesta | 2 | Mar 10 | `agent/app/graph.py`, `agent/app/schemas/tools.py` |
| TA-13.7 | Evaluación con 10 preguntas y tabla de resultados | 2 | Mié 11 | `docs/evidencias/sprint-2/HU-13/evaluacion.md` |
| TA-13.8 | Pruebas de las herramientas con backend simulado (respx) | 1 | Mié 11 | `agent/tests/` |

### HU-14 · Consulta al asistente desde la aplicación (Kevin)

| TA | Tarea | h | Límite | Archivos |
|---|---|---|---|---|
| TA-14.1 | `POST /api/agente/chat`: valida curso, llama al agente, guarda `Conversacion` y trazas, migración | 3 | Mar 10 | `backend/EduMonitor.Api/Controllers/AgenteController.cs`, `backend/EduMonitor.Application/Agente/` |
| TA-14.2 | Token de servicio rol `AGENTE` con claims `docente` y `curso`, 2 min; `/api/tools/*` lo aceptan | 2 | Mar 10 | `JwtService`, política `CursoAutorizado` |
| TA-14.3 | Panel «Asistente»: pregunta, respuesta y lista de herramientas usadas con estado y duración | 3 | Mié 11 | `frontend/src/pages/curso/Asistente.jsx` |
| TA-14.4 | Pruebas del proxy: 401, 403 sin llamar al agente, 503 con agente caído | 2 | Mié 11 | `backend/EduMonitor.Tests/Api/AgenteApiTests.cs` |

## Orden de trabajo de Kevin

1. TA-20.1, TA-20.2, TA-20.3 (domingo 8 en la noche y lunes 9 en la mañana).
2. TA-09.1, TA-09.2, TA-09.3, TA-13.1 (lunes 9).
3. TA-14.2, TA-14.1, TA-20.4, TA-09.4 (martes 10).
4. TA-13.2 apenas Sebastián fusione TA-12.3 (martes 10, 18:00); límite miércoles 11, 09:00.
5. TA-14.3, TA-14.4, TA-09.5, TA-20.5, TA-20.6 (miércoles 11).
6. Jueves 12: `release/sprint-2`, fusión a `main`, etiqueta `v0.2.0`, ensayo del guion.

Si una tarea de Kevin se atrasa, la primera que sale del sprint es TA-20.5.

## Guion de la Review (todo en el navegador)

1. Kevin: Sprint Goal y cambios que salieron de la Review 1.
2. Kevin, HU-20: «Estado del sistema» en verde; la clave del aula virtual sigue guardada tras reiniciar.
3. Santiago, HU-19: inicio de sesión, estructura, doc01 abre la ruta de BDD-4B y ve «Acceso no permitido».
4. Kevin, HU-09: panel de AGIL-4B; nota cero, sin entrega y pendiente diferenciados.
5. Sebastián, HU-12: alertas con evidencia; cambio del umbral de R4; marcar revisada.
6. Jeremy y Kevin, HU-13 y HU-14: «¿Qué estudiantes requieren atención en AGIL-4B?» en el panel «Asistente» con sus herramientas; pregunta sobre BDD-4B y respuesta sin acceso.
7. Kevin: qué vuelve al backlog y qué entra al Sprint 3.

## Evidencia para el informe

Cada responsable guarda en `docs/evidencias/sprint-2/HU-xx/` las capturas de la aplicación que muestran cada criterio de aceptación, con fecha visible. Kevin guarda la captura diaria del tablero de Jira y el burndown del miércoles 11 a las 23:00. El registro del daily va en `docs/sprint-2/daily.md`.
