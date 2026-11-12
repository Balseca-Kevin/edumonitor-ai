# EduMonitor AI

[![CI](https://github.com/Balseca-Kevin/edumonitor-ai/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/Balseca-Kevin/edumonitor-ai/actions/workflows/ci.yml)

Aplicación web separada de Moodle que lee cursos, matriculados y calificaciones de un Moodle de laboratorio, calcula indicadores en el backend y ofrece al docente un agente de IA que consulta esos datos. Proyecto final de Metodologías Ágiles · UTA · FISEI · Software 4to «B» · julio–diciembre 2026.

## Equipo

El proyecto se desarrolla con **Scrum**: 3 sprints de 2 semanas, Product Backlog de historias de usuario con Story Points, Sprint Review al cierre de cada sprint y un tablero en Jira (Por hacer, En curso, En revisión, Finalizada).

**Product Owner:** Ing. Hernán Fabricio Naranjo Ávalos (docente).

| Integrante | Usuario de GitHub | Rol Scrum | Área técnica |
|---|---|---|---|
| Kevin David Balseca Tumbaco | [Balseca-Kevin](https://github.com/Balseca-Kevin) | Scrum Master y Developer | Backend y Moodle |
| Sebastián Alexander Freire Ortega | [sebastianftr](https://github.com/sebastianftr) | Developer | Datos y analítica |
| Santiago David Escobar Chico | [kaijin7](https://github.com/kaijin7) | Developer | Interfaz |
| Jeremy Alexander Guamán Tuquerres | [levi06516-ui](https://github.com/levi06516-ui) | Developer | Agente de IA |

## Arquitectura

Monolito modular con un servicio de agente aparte, en dos entornos Docker separados, como pide la especificación: EduMonitor es independiente de Moodle y el Moodle de laboratorio ocupa el lugar del institucional (detalle en [docs/CONTEXTO_IA.md](docs/CONTEXTO_IA.md)).

```
 EduMonitor · docker compose up -d --wait · http://localhost:5173
 ┌──────────────────────────────────────────────────────────────────────────────┐
 │ Navegador ──► web (nginx + React) ──/api──► api (ASP.NET Core) ──► sqlserver │
 │                                               │  ▲                           │
 │                                    POST /chat │  │ /api/tools/*              │
 │                                               ▼  │                           │
 │                                     agent (FastAPI + LangGraph) ──► ollama   │
 └───────────────────────────────────────────────┬──────────────────────────────┘
                                                 │ REST solo lectura con token (moodle-puente)
                                                 ▼
 Aula virtual · docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d --wait
 ┌──────────────────────────────┐
 │ moodle (4.5) ──► moodle-db    │
 └──────────────────────────────┘
```

- El docente usa una sola dirección: <http://localhost:5173>. El navegador solo habla con `web`, que pasa `/api` a la API.
- Solo la API habla con Moodle, con SQL Server y con el agente. El agente no publica puerto y solo llama a `/api/tools/*` y a Ollama.
- Moodle es de solo lectura. Los cálculos se hacen en el backend con código probado; el modelo de lenguaje solo explica y recomienda.

| Entorno | Servicio | Puerto en el equipo | Carpeta |
|---|---|---|---|
| EduMonitor (`edumonitor`) | web: React 18 compilado + nginx | 5173 | `frontend/` |
| EduMonitor | api: ASP.NET Core .NET 8 (Swagger en `/swagger`) | 5000 | `backend/` |
| EduMonitor | sqlserver: SQL Server 2022 (`EduMonitorDB`) | 1433 | `backend/` (migraciones) |
| EduMonitor | agent: FastAPI + LangGraph | sin publicar | `agent/` |
| EduMonitor | ollama: modelo `qwen2.5:7b` con la GPU | sin publicar | — |
| EduMonitor | moodle-puente: lleva el `localhost:8080` de la API al Moodle del equipo | sin publicar | — |
| Aula virtual (`aula-virtual`) | moodle: Moodle 4.5 LTS | 8080 | `moodle-lab/` |
| Aula virtual | moodle-db: PostgreSQL de Moodle | sin publicar | `moodle-lab/` |

## Sprints

Release Plan vigente, re-planificado el 8 nov 2026 ([docs/sprint-2/PLAN.md](docs/sprint-2/PLAN.md)). Duración total: 19 oct – 27 nov 2026. Total: 120 SP.

| Sprint | Período | Objetivo | SP | Estado |
|---|---|---|---|---|
| 1 | 19 oct – 30 oct 2026 | Moodle de laboratorio, conexión por servicios web, modelo de datos, autenticación con autorización por curso, Mis cursos, primera sincronización y spike del agente | 39 | Cerrado · Review el 29 oct 2026 |
| 2 | 2 nov – 12 nov 2026 | Panel de indicadores, alertas con evidencia, asistente en la app, herramientas del agente con trazas, identidad visual y entorno único con Docker | 39 de 39 hechos | Cerrado · Review el jueves 12 nov 2026 · v0.2.1 |
| 3 | 16 nov – 27 nov 2026 | Mapeo temático, ficha del estudiante, desempeño por tema, tareas de refuerzo, informe, auditoría y cierre (HU-08, HU-10, HU-11, HU-15 a HU-18) | 42 | Por iniciar |

Cambio frente a la planificación inicial (110 SP), por la retroalimentación de la Review del Sprint 1: entran las HU nuevas HU-19 y HU-20, HU-14 se adelanta al Sprint 2, y HU-08, HU-10 y HU-11 pasan al Sprint 3.

### Sprint 1

> **Sprint Goal:** Tener el Moodle de laboratorio configurado y conectado por servicios web, el modelo de datos creado, la autenticación con autorización por curso, la pantalla de cursos y la primera sincronización verificable; además, validar el agente local con un spike técnico.

7 HU · 35 TA · 39 SP. Estado final según los Pull Request fusionados en `develop` (revisado el 11 nov 2026): «Hecha» si los PR de la HU están fusionados.

| HU | Historia | Responsable | SP | Estado | PR | Evidencia |
|---|---|---|---|---|---|---|
| HU-01 | Moodle de laboratorio con la estructura UTA | Sebastián Freire | 8 | Hecha | [#1](https://github.com/Balseca-Kevin/edumonitor-ai/pull/1), [#10](https://github.com/Balseca-Kevin/edumonitor-ai/pull/10), [#11](https://github.com/Balseca-Kevin/edumonitor-ai/pull/11) | [HU-01](docs/evidencias/sprint-1/HU-01/) |
| HU-02 | Servicios web de Moodle y conexión segura | Kevin Balseca | 5 | Hecha | [#2](https://github.com/Balseca-Kevin/edumonitor-ai/pull/2), [#12](https://github.com/Balseca-Kevin/edumonitor-ai/pull/12), [#14](https://github.com/Balseca-Kevin/edumonitor-ai/pull/14), [#17](https://github.com/Balseca-Kevin/edumonitor-ai/pull/17) | [HU-02](docs/evidencias/sprint-1/HU-02/) |
| HU-03 | Modelo de datos con migraciones y semillas | Sebastián Freire | 5 | Hecha: TA-03.2 y TA-03.5 en #15; entidades y migraciones base en #3 y #4. Cobertura de TA-03.1, TA-03.3 y TA-03.4: por confirmar | [#15](https://github.com/Balseca-Kevin/edumonitor-ai/pull/15) | [HU-03](docs/evidencias/sprint-1/HU-03/) |
| HU-04 | Autenticación JWT y autorización por rol y por curso | Kevin Balseca | 5 | Hecha | [#3](https://github.com/Balseca-Kevin/edumonitor-ai/pull/3) | [HU-04](docs/evidencias/sprint-1/HU-04/) |
| HU-05 | Sincronización manual de un curso | Kevin Balseca | 8 | Hecha | [#4](https://github.com/Balseca-Kevin/edumonitor-ai/pull/4), [#13](https://github.com/Balseca-Kevin/edumonitor-ai/pull/13) | [HU-05](docs/evidencias/sprint-1/HU-05/) |
| HU-06 | Pantallas de inicio de sesión y Mis cursos | Santiago Escobar | 5 | Hecha | [#6](https://github.com/Balseca-Kevin/edumonitor-ai/pull/6), [#17](https://github.com/Balseca-Kevin/edumonitor-ai/pull/17), [#18](https://github.com/Balseca-Kevin/edumonitor-ai/pull/18) | [HU-06](docs/evidencias/sprint-1/HU-06/) |
| HU-07 | Spike del agente con FastAPI, LangGraph y Ollama | Jeremy Guamán | 3 | Hecha ([#16](https://github.com/Balseca-Kevin/edumonitor-ai/pull/16), con ajustes, se cerró sin fusionar el 11 nov porque lo reemplazó [#21](https://github.com/Balseca-Kevin/edumonitor-ai/pull/21)) | [#7](https://github.com/Balseca-Kevin/edumonitor-ai/pull/7) | [HU-07](docs/evidencias/sprint-1/HU-07/) |

Índice de evidencias: [docs/evidencias/sprint-1/](docs/evidencias/sprint-1/README.md).

### Sprint 2

> **Sprint Goal:** El docente abre EduMonitor, ve el panel de indicadores de su curso y las alertas que requieren atención con su evidencia, y pregunta al asistente desde la misma aplicación; EduMonitor se levanta con un solo comando y el aula virtual corre aparte.

6 HU · 34 TA · 39 SP. Estado al 12 nov 2026, día de la Review, según los Pull Request fusionados en `develop`: las 6 HU y las 34 TA hechas. Quién hizo cada tarea, en [docs/sprint-2/PLAN.md](docs/sprint-2/PLAN.md).

Cambio de diseño del 12 nov 2026, 08:14 (decisión de Kevin):
- La interfaz pasó al **diseño D**, sobre el PR #35 de Santiago, para que HU-19 siga siendo suya.
- Motivo: el Product Owner vio genérico el inicio de sesión y pidió una herramienta que hable como docente.
- El panel y las alertas usan lenguaje de aula, sin términos estadísticos ni códigos de regla, y el asistente es un chat con historial.
- Reglas y motivo en [docs/sprint-2/diseno.md](docs/sprint-2/diseno.md).

| HU | Historia | Responsable | SP | Estado | PR |
|---|---|---|---|---|---|
| HU-20 | Entorno único de ejecución con Docker Compose | Kevin Balseca | 5 | Hecha | [#27](https://github.com/Balseca-Kevin/edumonitor-ai/pull/27), [#29](https://github.com/Balseca-Kevin/edumonitor-ai/pull/29) |
| HU-19 | Identidad visual y pantallas de acceso | Santiago Escobar (diseño D aplicado por Kevin) | 5 | Hecha | [#35](https://github.com/Balseca-Kevin/edumonitor-ai/pull/35) |
| HU-09 | Panel de indicadores del curso | Kevin Balseca | 8 | Hecha | [#23](https://github.com/Balseca-Kevin/edumonitor-ai/pull/23), [#26](https://github.com/Balseca-Kevin/edumonitor-ai/pull/26), [#35](https://github.com/Balseca-Kevin/edumonitor-ai/pull/35) |
| HU-12 | Alertas académicas con reglas configurables | Sebastián Freire (revisión y corrección de Kevin) | 5 | Hecha | [#38](https://github.com/Balseca-Kevin/edumonitor-ai/pull/38) |
| HU-13 | Herramientas del agente con trazas | Jeremy Guamán (TA-13.1, TA-13.2 y la ejecución de TA-13.7: Kevin) | 8 | Hecha | [#21](https://github.com/Balseca-Kevin/edumonitor-ai/pull/21), [#24](https://github.com/Balseca-Kevin/edumonitor-ai/pull/24), [#28](https://github.com/Balseca-Kevin/edumonitor-ai/pull/28), [#37](https://github.com/Balseca-Kevin/edumonitor-ai/pull/37), [#39](https://github.com/Balseca-Kevin/edumonitor-ai/pull/39), [#40](https://github.com/Balseca-Kevin/edumonitor-ai/pull/40) |
| HU-14 | Consulta al asistente desde la aplicación | Kevin Balseca | 8 | Hecha | [#25](https://github.com/Balseca-Kevin/edumonitor-ai/pull/25), [#36](https://github.com/Balseca-Kevin/edumonitor-ai/pull/36), [#35](https://github.com/Balseca-Kevin/edumonitor-ai/pull/35) |

Evidencias: [docs/evidencias/sprint-2/](docs/evidencias/sprint-2/README.md). Guía para comprobar cada HU en el navegador: [docs/PRUEBAS.md](docs/PRUEBAS.md).

## Contribuciones

Commits sin merges en `develop` por sprint (`git shortlog -sn --no-merges origin/develop` con `--before` o `--since`). Corte: 11 nov 2026, 21:55, después de [#30](https://github.com/Balseca-Kevin/edumonitor-ai/pull/30). Desde el Sprint 2 cada Pull Request entra con squash, así que cuenta como un commit.

Las firmas «Kevin-Balseca» y «Balseca-Kevin» son la misma persona y se suman como Kevin Balseca; el archivo [.mailmap](.mailmap) las une en `git shortlog`.

**Sprint 1 (commits hasta el 30 oct 2026)**

| Integrante | Commits en `develop` | PR fusionados |
|---|---|---|
| Kevin Balseca | 29 | #1 a #5, #11 a #15, #17 y #18 |
| Sebastián Freire | 1 | #10 |
| Santiago Escobar | 1 | #6 |
| Jeremy Guamán | 1 | #7 |

**Sprint 2 (commits desde el 2 nov 2026)**

| Integrante | Commits en `develop` | PR fusionados |
|---|---|---|
| Kevin Balseca | 9 | #22 a #30 |
| Jeremy Guamán | 1 | #21 |
| Sebastián Freire | 0 | — |
| Santiago Escobar | 0 | — |

## Puesta en marcha

Requisitos: Git y Docker Desktop con WSL 2. Para el modelo con GPU, una tarjeta NVIDIA con su controlador al día; sin ella, ver [Equipos sin GPU NVIDIA](#equipos-sin-gpu-nvidia). Todos los comandos se ejecutan desde la raíz del repositorio.

1. **Variables de entorno.** Copia `.env.example` a `.env` y cambia las contraseñas (`MSSQL_SA_PASSWORD`, `MOODLE_ADMIN_PASSWORD`, `MOODLE_DB_PASSWORD`, `Jwt__Secret`, `Semillas__Contrasena`). La de SQL Server necesita mayúscula, minúscula, número, símbolo y al menos 8 caracteres. `.env` nunca se sube.
   ```powershell
   Copy-Item .env.example .env
   ```
2. **Aula virtual** (otro entorno; detalle en [moodle-lab/README.md](moodle-lab/README.md)):
   ```powershell
   docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d --wait
   ```
   La primera vez en cada equipo, crea el servicio `edumonitor_ws`, el usuario técnico de solo lectura y su token (los cursos y los datos de laboratorio se cargan con «Restaurar desde cero» de [moodle-lab/README.md](moodle-lab/README.md#restaurar-desde-cero-ta-016)):
   ```powershell
   docker cp moodle-lab/scripts/configurar_servicios_web.php aula-virtual-moodle-1:/tmp/
   docker exec aula-virtual-moodle-1 php /var/www/html/admin/cli/cfg.php --name=enablewebservices --set=1
   docker exec aula-virtual-moodle-1 php /var/www/html/admin/cli/cfg.php --name=webserviceprotocols --set=rest
   docker exec aula-virtual-moodle-1 php /tmp/configurar_servicios_web.php
   ```
   Copia el token que imprime a `Moodle__Token` en `.env`, o pégalo después en la pantalla «Configuración de Moodle» como `admin`.
3. **EduMonitor con su agente, un solo comando:**
   ```powershell
   docker compose up -d --wait
   ```
   La primera vez construye las imágenes y descarga `qwen2.5:7b` (unos 4,7 GB) al volumen `edumonitor_ollama-models`; `--wait` termina cuando los servicios están sanos y el modelo quedó cargado en la GPU. La API aplica las migraciones y crea los usuarios semilla (`admin`, `doc01`, `doc02`) con la contraseña de `Semillas__Contrasena`. Después de cambiar código: `docker compose up -d --build --wait`.
4. **Abrir** <http://localhost:5173>. Como `admin`, «Estado del sistema» muestra la API, la base, Moodle, el agente y el modelo en verde.

   | Usuario de prueba | Rol | Qué ve |
   |---|---|---|
   | `admin` | ADMIN | Configuración de Moodle, Estado del sistema y todos los cursos |
   | `doc01` | DOCENTE | AGIL-4B: Mis cursos, panel y asistente |
   | `doc02` | DOCENTE | BDD-4B: Mis cursos, panel y asistente |

   Los tres usan la contraseña de `Semillas__Contrasena` del `.env`.

En Windows, `scripts\iniciar-sistema.bat` hace los pasos 2 y 3 en orden (enciende Docker Desktop si hace falta) y abre <http://localhost:5173>; `scripts\detener-sistema.bat` apaga los dos entornos sin borrar datos.

Apagar sin perder datos:

```powershell
docker compose down
docker compose -f moodle-lab/docker-compose.yml --env-file .env down
```

Nunca `down -v` ni `docker volume rm`: borran la base, el token de Moodle, las llaves de Data Protection y los modelos.

### Prueba de reinicio

La configuración de Moodle se guarda en la tabla `ConfiguracionMoodle`, con el token protegido por ASP.NET Core Data Protection; las llaves viven en el volumen `edumonitor_api-keys`. Si la tabla está vacía, la API usa `Moodle__Url` y `Moodle__Token` del entorno.

1. Entra como `admin`. En «Configuración de Moodle» deja la URL `http://localhost:8080`, pega el token (o deja el campo vacío para conservar el actual) y pulsa «Guardar». Anota la hora de «Última actualización».
2. Reinicia la API con `docker compose restart api`, o reinicia Docker Desktop o el equipo.
3. Vuelve a entrar como `admin`. «Última actualización» muestra la misma hora del paso 1, la clave actual termina en los mismos 4 caracteres y «Probar conexión» responde «Conexión correcta» con 8/8 funciones. Si la API hubiera vuelto a leer el entorno, la hora sería la del arranque.

### Cómo llega la API a Moodle

Moodle solo acepta llamadas a su `SITE_URL` (`http://localhost:8080`) y redirige las demás. Dentro de Docker, `localhost` es la API, así que el servicio `moodle-puente` (socat) lleva ese puerto al Moodle publicado en el equipo (`host.docker.internal:8080`). La API usa la red del puente; por eso el puerto 5000 y el nombre `api` se publican desde `moodle-puente`. El puente es el dueño de la red para que un reinicio de la API no lo rompa. Ningún servicio de EduMonitor depende de `moodle-lab/`: con el aula virtual apagada, EduMonitor arranca y «Estado del sistema» muestra Moodle como no disponible.

### Equipos sin GPU NVIDIA

`docker-compose.cpu.yml` quita la reserva de la GPU y Ollama corre en la CPU:

```powershell
docker compose -f docker-compose.yml -f docker-compose.cpu.yml up -d --wait
```

En la CPU las respuestas son lentas; `OLLAMA_MODEL=llama3.2:3b` en `.env` responde más rápido que `qwen2.5:7b`. Si una respuesta pasa de 60 s, la pantalla del asistente muestra «El asistente no está disponible».

### Pruebas

```powershell
dotnet test backend/EduMonitor.sln
cd frontend; npm ci; npm test -- --run; cd ..
cd agent; python -m pytest; cd ..
```

Las pruebas de `MoodleClientIntegracionTests` y `SincronizacionMoodleRealTests` llaman al Moodle real y necesitan el aula virtual encendida.

## Documentación

- [CONTRIBUTING.md](CONTRIBUTING.md): Git Flow, commits, revisiones y Definition of Done.
- [docs/CONTEXTO_IA.md](docs/CONTEXTO_IA.md): contexto del proyecto, stack, reglas del dominio y funciones de Moodle permitidas.
- [docs/contratos-api.md](docs/contratos-api.md): rutas, JSON y códigos de respuesta del backend.
- [docs/diccionario-datos.md](docs/diccionario-datos.md): tablas y columnas de `EduMonitorDB`, con las migraciones del Sprint 2.
- [docs/planificacion/](docs/planificacion/): planificación Scrum del Sprint 1 y especificación del docente.
- [docs/sprint-2/](docs/sprint-2/PLAN.md): plan del Sprint 2 ([PLAN.md](docs/sprint-2/PLAN.md)), sistema de diseño ([diseno.md](docs/sprint-2/diseno.md)) y registro del daily ([daily.md](docs/sprint-2/daily.md)).
- [docs/PRUEBAS.md](docs/PRUEBAS.md): cómo comprobar cada historia en el navegador y cuántas pruebas automatizadas pasan.
- [docs/evidencias/sprint-1/](docs/evidencias/sprint-1/README.md) y [docs/evidencias/sprint-2/](docs/evidencias/sprint-2/README.md): evidencias de cada Sprint Review.
- [frontend/README.md](frontend/README.md): rutas, servicios y pruebas de la interfaz.
- [moodle-lab/README.md](moodle-lab/README.md): Moodle de laboratorio en Docker.
