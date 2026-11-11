# Contexto del proyecto EduMonitor AI (para asistentes de IA)

Pega este archivo al inicio de cualquier conversación con una IA que no lea el repositorio por su cuenta (Gemini web, ChatGPT, Copilot Chat sin el repo abierto). Claude Code y Copilot en VS Code lo cargan solos a través de `CLAUDE.md` y `AGENTS.md`.

## Qué es

EduMonitor AI es una aplicación web separada de Moodle. Lee cursos, matriculados, actividades, calificaciones y estados de entrega de un Moodle de laboratorio por servicios web REST oficiales, los guarda en SQL Server, calcula indicadores en código determinista en el backend y ofrece al docente un agente de IA que consulta esos datos llamando a herramientas del backend.

Proyecto final de Metodologías Ágiles (UTA, FISEI, carrera de Software, 4to «B», julio–diciembre 2026). Docente y Product Owner: Ing. Hernán Fabricio Naranjo Ávalos. Scrum con 3 sprints (19 oct – 27 nov 2026). Los documentos fuente están en `docs/planificacion/` y el plan del sprint activo en `docs/sprint-2/PLAN.md`.

## Fechas

| Sprint | Período | Review |
|---|---|---|
| 1 | 19 oct – 30 oct 2026 | 29 oct 2026 |
| 2 | 2 nov – 12 nov 2026 (re-planificación: domingo 8 nov 2026) | jueves 12 nov 2026 |
| 3 | 16 nov – 27 nov 2026 | por confirmar |

## Lo que pidió el Product Owner en la Review del Sprint 1

- «TODO debe estar en el Software, nada por consola o archivos». Toda HU se demuestra en una pantalla de la aplicación.
- El inicio de sesión se veía genérico. La interfaz se rehace con el sistema de diseño de `docs/sprint-2/diseno.md`.
- Las historias de usuario no van en primera persona y deben coincidir entre planificación, Jira e informe.

## Equipo y responsabilidades del Sprint 2

| Integrante | Rol Scrum | Área | HU del Sprint 2 |
|---|---|---|---|
| Kevin Balseca | Scrum Master / Developer | Backend, Moodle, integración, Docker | HU-20 Entorno Docker, HU-09 Panel de indicadores, HU-14 Asistente en la app; herramientas del backend de HU-13 |
| Sebastián Freire | Developer | Datos y analítica | HU-12 Alertas con reglas configurables |
| Santiago Escobar | Developer | Interfaz | HU-19 Identidad visual y pantallas de acceso |
| Jeremy Guamán | Developer | Agente de IA | HU-13 Herramientas del agente con trazas (TA-13.1 y TA-13.2 las hace Kevin) |

Cada responsable construye su HU de punta a punta y la presenta en la Review.

## Stack fijo

No se cambia sin acuerdo del equipo.

- Moodle 4.5 en Docker (`erseco/alpine-moodle:v4.5.15`), datos ficticios. Lectura solo por servicios web REST con el servicio externo `edumonitor_ws` y un token de solo lectura.
- Backend: ASP.NET Core Web API sobre .NET 8 (`net8.0`), controladores, Entity Framework Core 8 con SQL Server, JWT, Swagger, xUnit. Paquetes `Microsoft.*` siempre en versión 8.0.x.
- Base de datos: SQL Server 2022 en Docker, base `EduMonitorDB`, migraciones de EF Core versionadas.
- Frontend: React 18 + Vite, React Router, Axios, Recharts, Vitest + Testing Library. Tipografía Archivo.
- Agente: Python 3.11, FastAPI, LangGraph, Pydantic, Ollama (`qwen2.5:7b`, alternativa `llama3.2:3b`), pytest.
- Entorno: dos entornos Docker separados. EduMonitor (web, api, sqlserver, agent, ollama y moodle-puente) en `docker-compose.yml` de la raíz, proyecto `edumonitor`. `moodle-puente` comparte la red de la API y lleva su `localhost:8080` al Moodle publicado en el host, porque Moodle solo acepta su `SITE_URL`. Aula virtual (moodle, moodle-db) en `moodle-lab/docker-compose.yml`, proyecto `aula-virtual`. La especificación pide una aplicación independiente de Moodle; el Moodle de laboratorio ocupa el lugar del institucional.
- Gestión: Git + GitHub con Git Flow, GitHub Actions, Jira (tablero: Por hacer, En curso, En revisión, Finalizada).

## Arquitectura

Monolito modular con un servicio de agente aparte. No hay microservicios.

```
 EduMonitor · docker compose up -d en la raíz · http://localhost:5173
 ┌────────────────────────────────────────────────────────────────────────────────────────────┐
 │ Navegador ──► web (nginx + React) ──/api──► api (ASP.NET Core) ──► sqlserver (EduMonitorDB) │
 │                                               │   ▲                                         │
 │                                  POST /chat   │   │  /api/tools/* (token de servicio)        │
 │                                               ▼   │                                         │
 │                                     agent (FastAPI + LangGraph) ──► ollama                   │
 └───────────────────────────────────────────────┬────────────────────────────────────────────┘
                                                 │ REST solo lectura con token
                                                 ▼  http://host.docker.internal:8080
 Aula virtual de laboratorio · docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d
 ┌──────────────────────────────┐
 │ moodle (4.5) ──► moodle-db    │
 └──────────────────────────────┘
```

| Entorno | Servicio | Puerto publicado | Volumen |
|---|---|---|---|
| EduMonitor (raíz) | web | 5173 | ninguno |
| EduMonitor (raíz) | api | 5000 | api-keys |
| EduMonitor (raíz) | sqlserver | 1433 | edumonitor_sqlserver-data |
| EduMonitor (raíz) | agent | ninguno, solo red interna | ninguno |
| EduMonitor (raíz) | ollama | ninguno, solo red interna | ollama-models |
| EduMonitor (raíz) | moodle-puente | ninguno, comparte la red de api | ninguno |
| Aula virtual (`moodle-lab/`) | moodle | 8080 | edumonitor_moodledata |
| Aula virtual (`moodle-lab/`) | moodle-db | ninguno | edumonitor_moodle-db-data |

- El docente usa una sola dirección, `http://localhost:5173`. El asistente es una pantalla de la app (`/cursos/:id/asistente`); el navegador nunca habla con el agente.
- El navegador solo habla con `web`; `web` pasa `/api` a `api`.
- El agente solo habla con `api` (`/api/tools/*`) y con `ollama`. Nunca lee la base ni conoce el token de Moodle.
- Solo `api` habla con Moodle y con SQL Server. Moodle está en otro entorno: la API lo llama por `Moodle__Url` (`http://localhost:8080`, que dentro de Docker lleva `moodle-puente` a `host.docker.internal:8080`), como llamaría al Moodle institucional.
- Desde el Sprint 2 el docente pregunta en `POST /api/agente/chat`. La API valida el curso, emite un token de servicio corto para el agente y llama a `agent`. El agente ya no recibe el JWT del docente.
- Promedios, desviaciones y alertas se calculan en el backend con código probado. El modelo de lenguaje solo explica resultados y recomienda.

### Capas del backend

```
backend/
  EduMonitor.sln
  EduMonitor.Api/             Controladores, Program.cs, políticas de autorización, Swagger, middleware
  EduMonitor.Application/     Casos de uso, servicios, DTOs, interfaces (IMoodleClient, repositorios, IJwtService)
  EduMonitor.Domain/          Entidades y enums del dominio. Sin dependencias.
  EduMonitor.Infrastructure/  DbContext, migraciones, semillas, cliente Moodle (HttpClient), implementaciones
  EduMonitor.Tests/           xUnit
```

Dependencias permitidas: Api → Application, Infrastructure · Infrastructure → Application, Domain · Application → Domain · Domain → nada.

## Reglas del dominio

- Moodle es solo lectura. El sistema nunca escribe en Moodle ni modifica calificaciones.
- `CALIFICACION.estado` distingue `SIN_ENTREGA`, `PENDIENTE` y `CALIFICADO`. Una nota 0 calificada es distinta de sin entrega. Solo las notas `CALIFICADO` entran a los promedios.
- Sincronización manual, idempotente. Cada ejecución queda registrada con estado `COMPLETADA`, `PARCIAL` o `FALLIDA`, fecha, origen, cantidad de registros y errores.
- Si una función de Moodle falla, la sincronización queda `PARCIAL` y el indicador afectado se muestra como «no disponible», nunca como 0.
- Roles del sistema: `DOCENTE` y `ADMIN`. El agente usa un token de servicio con rol `AGENTE` atado al docente que preguntó. No existe rol estudiante.
- Cada docente ve solo sus cursos. Acceso a un curso ajeno: 403. Sin token: 401. En la interfaz, 403 lleva a «Acceso no permitido» y 401 a «Tu sesión venció».
- Datos mínimos de estudiantes: id de Moodle, nombre y apellido.
- Ponderación UTA: exámenes 40 %, tareas 30 %, APE 30 %. Aprobación con 7,0. Si falta una categoría, los pesos se normalizan y el valor se marca como parcial.
- Indicadores (planificación, tabla 5): promedio ponderado, media, mediana, desviación estándar muestral, grupos (alto ≥ 8,5; medio 6,0 a 8,49; bajo < 6,0), puntuación z, tasa de finalización, tendencia con N = 3. Caso de prueba fijo: 9, 8, 6, 3, 3 → promedio 5,80, desviación 2,77.
- Reglas de alerta del Sprint 2: R1 descenso reciente (tendencia ≤ −1,5 con N = 3), R2 atrasos (2 o más actividades vencidas sin entrega), R4 promedio bajo (promedio ponderado < 7,0 con al menos 4 notas). Umbrales configurables por curso.
- Lenguaje de alertas: «Requiere atención». Nunca «reprobará» ni «en riesgo».

## Funciones de Moodle permitidas

Solo estas. Ninguna otra.

| Función | Uso |
|---|---|
| `core_webservice_get_site_info` | Probar conexión, versión y funciones habilitadas |
| `core_enrol_get_users_courses` | Cursos de un usuario |
| `core_course_get_contents` | Secciones (unidades) y módulos (actividades) |
| `core_enrol_get_enrolled_users` | Matriculados con su rol |
| `gradereport_user_get_grade_items` | Ítems, categorías, pesos y notas |
| `mod_assign_get_assignments` | Tareas y fechas límite |
| `mod_assign_get_submissions` | Estado de entrega por estudiante |
| `core_completion_get_activities_completion_status` | Estado de finalización |

## Alcance por sprint

| HU | Historia | Sprint | Responsable |
|---|---|---|---|
| HU-01 a HU-07 | Moodle de laboratorio, conexión, modelo de datos, autenticación, sincronización, pantallas iniciales, spike del agente | 1 | varios |
| HU-20 | Entorno único de ejecución con Docker Compose | 2 | Kevin |
| HU-19 | Identidad visual y pantallas de acceso | 2 | Santiago |
| HU-09 | Panel de indicadores del curso | 2 | Kevin |
| HU-12 | Alertas académicas con reglas configurables | 2 | Sebastián |
| HU-13 | Herramientas del agente con trazas | 2 | Jeremy |
| HU-14 | Consulta al asistente desde la aplicación | 2 | Kevin |
| HU-08, HU-10, HU-11, HU-15, HU-16, HU-17, HU-18 | Mapeo temático, ficha del estudiante, desempeño por tema, refuerzo, informe, auditoría, cierre | 3 | varios |

No construir nada del Sprint 3 mientras el sprint activo sea el 2.

## Convenciones

- Ramas Git Flow: `main`, `develop`, `feature/HU-xx-descripcion`, `release/sprint-n`, `hotfix/descripcion`. Nada se sube directo a `main` ni a `develop`.
- Todo Pull Request hacia `develop` o `main` necesita la aprobación de Kevin (CODEOWNERS). Kevin es el único que fusiona, con la integración continua en verde; sus propios Pull Request los fusiona con el bypass del ruleset, porque GitHub no deja aprobar el Pull Request propio.
- Commits con Conventional Commits en español e imperativo: `tipo(ámbito): descripción`. Tipos: feat, fix, test, docs, refactor, style, chore, ci, build. Ámbitos: moodle, db, backend, auth, sync, indicadores, alertas, frontend, agent, docker, docs, ci. Pie: `Refs: SCRUM-n`.
- Nombres del dominio en español sin tildes (`Curso`, `Calificacion`, `Sincronizacion`). Lo propio del framework se deja como es (`Program.cs`, `DbContext`, `useEffect`).
- Errores HTTP del backend con `ProblemDetails` (RFC 7807).
- Secretos solo en variables de entorno (`.env`, nunca en el repositorio). En .NET se leen con el patrón Options; los nombres usan doble guion bajo (`Moodle__Token`).
- Cada cambio trae pruebas: xUnit en backend, Vitest en frontend, pytest en agente.

## Contratos

Las rutas, los JSON y los códigos de respuesta están en `docs/contratos-api.md`. Ese archivo manda sobre cualquier suposición. Si una tarea necesita algo que no está ahí, se propone el cambio y se avisa al equipo antes de implementarlo.
