# Contexto del proyecto EduMonitor AI (para asistentes de IA)

Pega este archivo al inicio de cualquier conversación con una IA que no lea el repositorio por su cuenta (Gemini web, ChatGPT, Copilot Chat sin el repo abierto). Claude Code y Copilot en VS Code lo cargan solos a través de `CLAUDE.md` y `AGENTS.md`.

## Qué es

EduMonitor AI es una aplicación web separada de Moodle. Lee cursos, matriculados, actividades, calificaciones y estados de entrega de un Moodle de laboratorio por servicios web REST oficiales, los guarda en SQL Server, calcula indicadores en código determinista en el backend y ofrece al docente un agente de IA que consulta esos datos llamando a herramientas del backend.

Proyecto final de Metodologías Ágiles (UTA, FISEI, carrera de Software, 4to «B», julio–diciembre 2026). Docente y Product Owner: Ing. Hernán Fabricio Naranjo Ávalos. Scrum con 3 sprints. Los documentos fuente están en `docs/planificacion/` y el plan del sprint activo en `docs/sprint-3/PLAN.md`.

## Fechas

Las fechas, horas y tiempos del proyecto y del informe los define Kevin. Ningún asistente las escribe, las calcula ni las propone. Sprint activo: 3, el último.

## Entrega

El Product Owner revisa el proyecto como carpeta (OneDrive o copia local), no en GitHub: no mira commits, PR ni ramas. El README, las evidencias y la demostración tienen que funcionar desde esa carpeta.

## Lo que pidió el Product Owner en la Review del Sprint 1

- «TODO debe estar en el Software, nada por consola o archivos». Toda HU se demuestra en una pantalla de la aplicación.
- El inicio de sesión se veía genérico. La interfaz se rehizo con el diseño D (`docs/sprint-2/diseno.md`); en el Sprint 3 la guía es `docs/sprint-3/diseno.md`.
- Las historias de usuario no van en primera persona y deben coincidir entre planificación, Jira e informe.

## Lo que pidió el Product Owner en la Review del Sprint 2

- Señaló un solo error: la aplicación no muestra las notas completas de los estudiantes, solo cálculos (promedios, conteos, alertas). Sin las notas el docente no puede comprobar ningún cálculo. Respuesta: HU-21, libro de calificaciones, como primera pantalla del curso, y toda cifra se puede abrir para ver de qué notas sale.
- Autorizó usar un modelo de IA externo, con cautela, tolerancia a fallos y un plan de respaldo. Reglas en «Modelo de IA» más abajo y en `agent/AGENTS.md`.

## Equipo y responsabilidades del Sprint 3

| Integrante | Rol Scrum | Área | HU del Sprint 3 |
|---|---|---|---|
| Kevin Balseca | Scrum Master / Developer | Backend, Moodle, integración, Docker | HU-22 Mapa del curso y panel de actividad (pantallas), HU-23 Asistente con contexto, memoria y respaldo, HU-17 Trazas y auditoría; endpoint de HU-21 y generador de documentos de HU-15 |
| Sebastián Freire | Developer | Datos y analítica | HU-08 Mapeo temático; cálculos por actividad, criterio y tema de HU-22 |
| Santiago Escobar | Developer | Interfaz | HU-21 Libro de calificaciones, HU-10 Ficha del estudiante, HU-16 Informe del curso |
| Jeremy Guamán | Developer | Agente de IA | HU-15 Retroalimentación y refuerzo; nodos del grafo de HU-23; evaluación del agente (HU-18) |

HU-18 (pruebas, documentación y demostración) es de todo el equipo. Tareas y horas en `docs/sprint-3/PLAN.md`.

## Equipo y responsabilidades del Sprint 2 (cerrado)

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
- Frontend: React 18 + Vite, React Router, Axios, Recharts, Vitest + Testing Library. Diseño del Sprint 3 (`docs/sprint-3/diseno.md`, prototipo en `docs/sprint-3/prototipo/`), que sigue al diseño D: Archivo y JetBrains Mono para códigos de curso.
- Agente: Python 3.11, FastAPI, LangGraph, Pydantic, pytest. Modelo: proveedor externo autorizado por el Product Owner en la Review del Sprint 2 (por confirmar con la prueba de las 10 preguntas), con Ollama (`qwen2.5:7b`, alternativa `llama3.2:3b`) como respaldo local.
- Entorno: dos entornos Docker separados. EduMonitor (web, api, sqlserver, agent, ollama y moodle-puente) en `docker-compose.yml` de la raíz, proyecto `edumonitor`. La API usa la red de `moodle-puente`, que lleva su `localhost:8080` al Moodle publicado en el host, porque Moodle solo acepta su `SITE_URL`; el puente es el dueño de esa red para que un reinicio de la API no lo rompa. Aula virtual (moodle, moodle-db) en `moodle-lab/docker-compose.yml`, proyecto `aula-virtual`. La especificación pide una aplicación independiente de Moodle; el Moodle de laboratorio ocupa el lugar del institucional.
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
| EduMonitor (raíz) | moodle-puente | 5000 de la API (la API usa su red) | ninguno |
| Aula virtual (`moodle-lab/`) | moodle | 8080 | edumonitor_moodledata |
| Aula virtual (`moodle-lab/`) | moodle-db | ninguno | edumonitor_moodle-db-data |

- El docente usa una sola dirección, `http://localhost:5173`. El asistente es una pantalla de la app (`/cursos/:id/asistente`); el navegador nunca habla con el agente.
- El navegador solo habla con `web`; `web` pasa `/api` a `api`.
- El agente solo habla con `api` (`/api/tools/*`) y con `ollama`. Nunca lee la base ni conoce el token de Moodle.
- Solo `api` habla con Moodle y con SQL Server. Moodle está en otro entorno: la API lo llama por `Moodle__Url` (`http://localhost:8080`, que dentro de Docker lleva `moodle-puente` a `host.docker.internal:8080`), como llamaría al Moodle institucional.
- Desde el Sprint 2 el docente pregunta en `POST /api/agente/chat`. La API valida el curso, emite un token de servicio corto para el agente y llama a `agent`. El agente ya no recibe el JWT del docente.
- Promedios, desviaciones y alertas se calculan en el backend con código probado. El modelo de lenguaje solo explica resultados y recomienda.
- Desde el Sprint 3 el agente llama primero al modelo externo y, si falla, a `ollama`. El proveedor se elige con variables de entorno, que se agregan a `.env.example` en la tarea de HU-23 que lo implemente.

## Modelo de IA (Sprint 3)

- Principal: modelo externo con llamadas a herramientas compatibles con LangGraph. Respaldo: `qwen2.5:7b` en `ollama`.
- Seudonimización: antes de llamar al modelo externo, el backend cambia cada nombre de estudiante por un código (E-001, E-002…). La tabla código-nombre queda en el backend y la pantalla muestra nombres. No salen nombres, correos ni ids de Moodle.
- Tolerancia a fallos:
  1. Límite de 20 s para el modelo externo; la consulta completa mantiene los 60 s.
  2. Error, tiempo agotado, 429 o respuesta sin el formato esperado: la misma pregunta va al modelo local.
  3. 3 fallos del externo en 5 minutos: 5 minutos directo al modelo local.
  4. «Estado del sistema» muestra los dos modelos, cuál está activo y el interruptor «Usar solo el modelo local» (solo ADMIN).
  5. Cada respuesta dice «Respondió: modelo externo» o «Respondió: modelo local (respaldo)», y la traza lo guarda.
  6. Si fallan los dos: «El asistente no está disponible» (503, como desde HU-14).
- Contexto y memoria: el backend envía al agente el foco de la pantalla (curso, actividad, estudiante, por id) y los últimos 4 turnos de la conversación guardada.
- Verificación de cifras: un nodo de código revisa que cada número de la respuesta esté en lo que devolvieron las herramientas y devuelve de dónde sale cada uno, para enlazarlo en pantalla. Si falta uno, el modelo redacta otra vez; si vuelve a faltar, la cifra se marca como no verificada.
- Fuera de alcance: el asistente no evalúa aspectos clínicos ni psicológicos; dice que no puede y ofrece lo que muestran los datos.
- Evaluación: 18 preguntas (10 del Sprint 2, 5 de seguimiento, 3 fuera de alcance), corridas con los dos modelos desde la pantalla «Calidad del asistente».

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

- Datos reales: todo lo que se ve en la aplicación sale de Moodle o de lo que el docente registra en EduMonitor. Nada de ejemplo ni de adorno, salvo los puntos y las preguntas decorativas del inicio de sesión. Si falta un dato en el aula virtual, se agrega en Moodle con `moodle-lab/scripts/`. Origen de cada dato: `docs/sprint-3/diseno.md`.
- Las unidades y sus temas siguen los sílabos de la carrera (`docs/sprint-3/silabos.md`).

- Moodle es solo lectura. El sistema nunca escribe en Moodle ni modifica calificaciones.
- `CALIFICACION.estado` distingue `SIN_ENTREGA`, `PENDIENTE` y `CALIFICADO`. Una nota 0 calificada es distinta de sin entrega. Solo las notas `CALIFICADO` entran a los promedios.
- Sincronización manual, idempotente. Cada ejecución queda registrada con estado `COMPLETADA`, `PARCIAL` o `FALLIDA`, fecha, origen, cantidad de registros y errores.
- Si una función de Moodle falla, la sincronización queda `PARCIAL` y el indicador afectado se muestra como «no disponible», nunca como 0.
- Roles del sistema: `DOCENTE` y `ADMIN`. El agente usa un token de servicio con rol `AGENTE` atado al docente que preguntó. No existe rol estudiante.
- Cada docente ve solo sus cursos. Acceso a un curso ajeno: 403. Sin token: 401. En la interfaz, 403 lleva a «Acceso no permitido» y 401 a «Tu sesión venció».
- Cursos del laboratorio:
  - doc01 tiene AGIL-4B (Moodle 2) y MCS-4B «Manejo y Configuración del Software (lab)» (Moodle 4, desde el 12 nov 2026, con est01 a est12).
  - doc02 tiene BDD-4B (Moodle 3).
  - La API agrega MCS-4B al arrancar si no existe (`SembradorDatos.AsegurarCursoMcsAsync`).
- Datos mínimos de estudiantes: id de Moodle, nombre y apellido.
- Ponderación UTA: exámenes 40 %, tareas 30 %, APE 30 %. Aprobación con 7,0. Si falta una categoría, los pesos se normalizan y el valor se marca como parcial.
- Indicadores (planificación, tabla 5): promedio ponderado, media, mediana, desviación estándar muestral, grupos (alto ≥ 8,5; medio 6,0 a 8,49; bajo < 6,0), puntuación z, tasa de finalización, tendencia con N = 3. Caso de prueba fijo: 9, 8, 6, 3, 3 → promedio 5,80, desviación 2,77.
- Reglas de alerta del Sprint 2: R1 descenso reciente (tendencia ≤ −1,5 con N = 3), R2 atrasos (2 o más actividades vencidas sin entrega), R4 promedio bajo (promedio ponderado < 7,0 con al menos 4 notas). Umbrales configurables por curso.
- Lenguaje de alertas: «Requiere atención». Nunca «reprobará» ni «en riesgo».
- Cálculos del Sprint 3, deterministas en Application y con pruebas:
  - Desempeño por tema y por criterio de rúbrica, con el mapeo de HU-08.
  - Por criterio: porcentaje de dominio del curso y correlación entre la nota del criterio y la nota total. En pantalla: «Casi todo el curso falló este criterio: revisar cómo se enseñó» o «Este criterio no separa a quienes van bien del resto: revisar el enunciado». Con 32 estudiantes es orientativo: se dice «revisar», no un veredicto, y no aparece la palabra correlación.
  - Nota mínima para llegar a 7,0 con 40/30/30 y las actividades pendientes, o aviso de que ya no alcanza. En pantalla: «cálculo con las notas actuales», nunca «nota final».
  - Refuerzo aplicado: al aprobar un refuerzo se guardan estudiante, tema y fecha; después de la siguiente sincronización se compara el promedio del tema antes y después. Estados: subió, igual, bajó, sin notas nuevas. En pantalla se dice el cambio («subió de 5,2 a 7,1 en el tema después del refuerzo»), no que el refuerzo lo causó.
- Los borradores de retroalimentación y refuerzo los aprueba el docente y se descargan en PDF o DOCX. Nada se envía al estudiante.

## Funciones de Moodle permitidas

Solo estas. Ninguna otra. Las tres últimas se agregan a `edumonitor_ws` en el Sprint 3 para rúbricas y comentarios del docente.

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
| `core_grading_get_definitions` | Rúbricas y sus criterios (Sprint 3, HU-08) |
| `core_grading_get_gradingform_instances` | Cómo se calificó cada criterio de la rúbrica (Sprint 3, HU-08 y HU-22) |
| `mod_assign_get_submission_status` | Estado y comentario del docente por entrega (Sprint 3, HU-10 y HU-15) |

## Alcance por sprint

Estado del Sprint 3. Detalle del Sprint 2 por tarea en `docs/sprint-2/PLAN.md`; del Sprint 3 en `docs/sprint-3/PLAN.md`.

| HU | Historia | Sprint | Responsable | Estado |
|---|---|---|---|---|
| HU-01 a HU-07 | Moodle de laboratorio, conexión, modelo de datos, autenticación, sincronización, pantallas iniciales, spike del agente | 1 | varios | Hechas en el Sprint 1 (#1 a #18) |
| HU-20 | Entorno único de ejecución con Docker Compose | 2 | Kevin | Hecha (#27, #29) |
| HU-19 | Identidad visual y pantallas de acceso | 2 | Santiago | Hecha con el diseño D (#35) |
| HU-09 | Panel de indicadores del curso | 2 | Kevin | Hecha (#23, #26) |
| HU-12 | Alertas académicas con reglas configurables | 2 | Sebastián | Hecha (#38) |
| HU-13 | Herramientas del agente con trazas | 2 | Jeremy | Hecha (#21, #24, #28, #37, #39, #40) |
| HU-14 | Consulta al asistente desde la aplicación | 2 | Kevin | Hecha (#25, #36, #35) |
| HU-21 | Libro de calificaciones (nueva, por la Review del Sprint 2) | 3 | Santiago; endpoint de Kevin | Hecha (pantalla adelantada por Kevin) |
| HU-10 | Ficha del estudiante | 3 | Santiago | Hecha (adelantada por Kevin) |
| HU-08 | Mapeo de actividades y criterios de rúbrica a temas | 3 | Sebastián | Hecha (adelantada por Kevin) |
| HU-22 | Mapa del curso y panel de actividad (nueva, absorbe HU-11) | 3 | Kevin; cálculos de Sebastián | Hecha |
| HU-11 | Desempeño por tema y temas críticos | 3 | — | Fusionada en HU-22 |
| HU-15 | Retroalimentación y refuerzo descargables | 3 | Jeremy; generador de documentos de Kevin | Hecha (adelantada por Kevin) |
| HU-23 | Asistente con contexto, memoria, verificación de cifras y respaldo local (nueva) | 3 | Kevin; nodos del grafo de Jeremy | Hecha |
| HU-16 | Informe del curso | 3 | Santiago | Hecha (adelantada por Kevin) |
| HU-17 | Trazas visibles y auditoría | 3 | Kevin | Hecha |
| HU-18 | Pruebas, documentación y demostración final | 3 | Todo el equipo | Evaluación, «Calidad del asistente», matriz y guía hechas; faltan capturas, informe, video y ensayo |

Los agregados del Sprint 3 (refuerzo antes y después, diagnóstico por criterio, nota necesaria, cifras enlazadas, «Calidad del asistente», preguntas fuera de alcance) son tareas TA-xx.A dentro de esas HU. Se activó la línea de corte: salen TA-22.A1 y TA-22.A2 y no entra ningún agregado TA-xx.B. El rediseño suma TA-08.A1, TA-21.A1, TA-21.A2, TA-22.A3, TA-22.A4, TA-22.A5, TA-15.A3 y TA-15.A4. Detalle en `docs/sprint-3/PLAN.md`.

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
