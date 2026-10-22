# Contexto del proyecto EduMonitor AI (para asistentes de IA)

Pega este archivo al inicio de cualquier conversación con una IA que no lea el repositorio por su cuenta (Gemini web, ChatGPT, Copilot Chat sin el repo abierto). Claude Code y Copilot en VS Code lo cargan solos a través de `CLAUDE.md` y `AGENTS.md`.

## Qué es

EduMonitor AI es una aplicación web separada de Moodle. Lee cursos, matriculados, actividades, calificaciones y estados de entrega de un Moodle de laboratorio por servicios web REST oficiales, los guarda en SQL Server, calcula indicadores en código determinista en el backend y ofrece al docente un agente de IA que consulta esos datos llamando a herramientas del backend.

Proyecto final de Metodologías Ágiles (UTA, FISEI, carrera de Software, 4to «B», julio–diciembre 2026). Docente y Product Owner: Ing. Hernán Fabricio Naranjo Ávalos. Scrum con 3 sprints de 2 semanas (19 oct – 27 nov 2026). Los documentos fuente están en `docs/planificacion/`.

## Equipo y áreas

| Integrante | Rol Scrum | Área | Carpeta principal |
|---|---|---|---|
| Kevin Balseca | Scrum Master / Developer | Backend y Moodle: API, autenticación, autorización por curso, sincronización, auditoría | `backend/` (Api, Application), `moodle-lab/` |
| Sebastián Freire | Developer | Datos y analítica: Moodle de laboratorio, modelo SQL Server, cliente Moodle, indicadores, alertas | `backend/` (Domain, Infrastructure), `moodle-lab/` |
| Santiago Escobar | Developer | Interfaz: pantallas del docente y del administrador | `frontend/` |
| Jeremy Guamán | Developer | Agente de IA: servicio, herramientas, prompts, trazas | `agent/` |

## Stack fijo

No se cambia sin acuerdo del equipo.

- Moodle 4.x en Docker, datos ficticios. Lectura solo por servicios web REST con el servicio externo `edumonitor_ws` y un token de solo lectura.
- Backend: ASP.NET Core Web API sobre .NET 8 (`net8.0`), controladores, Entity Framework Core 8 con SQL Server, JWT, Swagger, xUnit. Paquetes `Microsoft.*` siempre en versión 8.0.x.
- Base de datos: SQL Server 2022 en Docker, base `EduMonitorDB`, migraciones de EF Core versionadas.
- Frontend: React 18 + Vite, React Router, Axios, Recharts, Vitest + Testing Library.
- Agente: Python 3.11, FastAPI, LangGraph, Pydantic, Ollama local (`qwen2.5:7b`, alternativa `llama3.2:3b`), pytest.
- Gestión: Git + GitHub con Git Flow, GitHub Actions, Jira (tablero Kanban: Por hacer, En curso, En revisión, Finalizada).

## Arquitectura

Monolito modular con un servicio de agente aparte. No hay microservicios.

```
React (SPA) ──HTTPS + JWT──► ASP.NET Core API ──► SQL Server (EduMonitorDB)
                                   │
                                   ├──REST token solo lectura──► Moodle de laboratorio
                                   ▲
Agente FastAPI + LangGraph ──/api/tools/*──┘      Agente ──► Ollama (modelo local)
```

- El frontend solo habla con el backend.
- El agente solo habla con el backend (`/api/tools/*`) y con Ollama. Nunca lee la base ni conoce el token de Moodle.
- Solo el backend habla con Moodle y con SQL Server.
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
- `CALIFICACION.estado` distingue `SIN_ENTREGA`, `PENDIENTE` y `CALIFICADO`. Una nota 0 calificada es distinta de sin entrega.
- Sincronización manual, idempotente (sincronizar dos veces no duplica). Cada ejecución queda registrada con estado `COMPLETADA`, `PARCIAL` o `FALLIDA`, fecha, origen, cantidad de registros y errores.
- Si una función de Moodle falla, la sincronización queda `PARCIAL` y el dato afectado se muestra como no disponible, nunca como 0.
- Roles del sistema: `DOCENTE` y `ADMIN`. No existe rol estudiante; los estudiantes son datos importados.
- Cada docente ve solo sus cursos. Acceso a un curso ajeno: 403. Sin token: 401.
- Se guardan datos mínimos de estudiantes: id de Moodle, nombre y apellido. Sin correos ni datos de contacto.
- Ponderación UTA: exámenes 40 %, tareas 30 %, APE 30 %. Aprobación con 7,0.
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

## Alcance del Sprint 1 (19 – 30 oct, se presenta el 22 oct)

| HU | Historia | Responsable |
|---|---|---|
| HU-01 | Moodle de laboratorio con la estructura UTA | Sebastián |
| HU-02 | Servicios web de Moodle y conexión segura | Kevin |
| HU-03 | Modelo de datos con migraciones y semillas | Sebastián |
| HU-04 | Autenticación JWT y autorización por rol y por curso | Kevin |
| HU-05 | Sincronización manual de un curso | Kevin |
| HU-06 | Pantallas de inicio de sesión y Mis cursos | Santiago |
| HU-07 | Spike del agente con FastAPI, LangGraph y Ollama | Jeremy |

Fuera del Sprint 1: dashboard, indicadores, mapeo temático, alertas, recomendaciones, informes, auditoría completa. No construir nada de eso todavía.

## Convenciones

- Ramas Git Flow: `main`, `develop`, `feature/HU-xx-descripcion`, `release/sprint-n`, `hotfix/descripcion`. Nada se sube directo a `main` ni a `develop`.
- Commits con Conventional Commits en español e imperativo: `tipo(ámbito): descripción`. Tipos: feat, fix, test, docs, refactor, chore, ci, build. Ámbitos: moodle, db, backend, auth, sync, frontend, agent, docs, ci. Pie: `Refs: SCRUM-n`.
- Nombres del dominio en español sin tildes (`Curso`, `Calificacion`, `Sincronizacion`). Lo propio del framework se deja como es (`Program.cs`, `DbContext`, `useEffect`).
- Errores HTTP del backend con `ProblemDetails` (RFC 7807).
- Secretos solo en variables de entorno (`.env`, nunca en el repositorio). En .NET se leen con el patrón Options; los nombres usan doble guion bajo (`Moodle__Token`).
- Cada cambio trae pruebas: xUnit en backend, Vitest en frontend, pytest en agente.

## Contratos

Las rutas, los JSON y los códigos de respuesta están en `docs/contratos-api.md`. Ese archivo manda sobre cualquier suposición. Si una tarea necesita algo que no está ahí, se propone el cambio y se avisa al equipo antes de implementarlo.
