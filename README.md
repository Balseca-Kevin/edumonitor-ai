# EduMonitor

[![CI](https://github.com/Balseca-Kevin/edumonitor-ai/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/Balseca-Kevin/edumonitor-ai/actions/workflows/ci.yml)

Aplicación web separada de Moodle que lee cursos, matriculados y calificaciones de un Moodle de laboratorio y calcula indicadores en el backend. Proyecto final de Metodologías Ágiles · UTA · FISEI · Software 4to «B» · julio–diciembre 2026.

## Equipo

El proyecto se desarrolla con **Scrum**: 3 sprints de 2 semanas, Product Backlog de historias de usuario con Story Points, Sprint Review al cierre de cada sprint y un tablero en Jira (Por hacer, En curso, En revisión, Finalizada).

**Product Owner:** Ing. Hernán Fabricio Naranjo Ávalos (docente).

| Integrante | Usuario de GitHub | Rol Scrum | Área técnica |
|---|---|---|---|
| Kevin David Balseca Tumbaco | [Balseca-Kevin](https://github.com/Balseca-Kevin) | Scrum Master y Developer | Backend y Moodle |
| Sebastián Alexander Freire Ortega | [sebastianftr](https://github.com/sebastianftr) | Developer | Datos y analítica |
| Santiago David Escobar Chico | [kaijin7](https://github.com/kaijin7) | Developer | Interfaz |
| Jeremy Alexander Guamán Tuquerres | [levi06516-ui](https://github.com/levi06516-ui) | Developer | Interfaz |

## Arquitectura

Monolito modular:

```
React (SPA) ──HTTPS + JWT──► ASP.NET Core API ──► SQL Server (EduMonitorDB)
                                   │
                                   ├──REST token solo lectura──► Moodle de laboratorio
```

- El frontend solo habla con el backend; solo el backend habla con Moodle y con SQL Server.
- Moodle es de solo lectura. Los cálculos se hacen en el backend con código probado.

| Servicio | Puerto local | Carpeta |
|---|---|---|
| Moodle 4.5 LTS | 8080 | `moodle-lab/` |
| PostgreSQL de Moodle | sin publicar (red interna de Docker) | `moodle-lab/` |
| SQL Server 2022 (`EduMonitorDB`) | 1433 | `moodle-lab/` (contenedor), `backend/` (migraciones) |
| Backend ASP.NET Core .NET 8 (Swagger en `/swagger`) | 5000 | `backend/` |
| Frontend React 18 + Vite | 5173 | `frontend/` |

## Sprints

Datos de la planificación (sección 5.6). Duración total: 19 oct – 27 nov 2026.

| Sprint | Período | Objetivo | SP | Estado |
|---|---|---|---|---|
| 1 | 19 oct – 30 oct 2026 | Requisitos, prototipos, modelo de datos, contratos de API, Moodle de laboratorio, autenticación, cursos y sincronización inicial | 39 | En curso |
| 2 | 2 nov – 13 nov 2026 | Mapeo temático, panel, ficha individual, indicadores, análisis por tema y reglas de alerta | 42 | Por iniciar |
| 3 | 16 nov – 27 nov 2026 | Recomendaciones, tareas de refuerzo, informe, auditoría, pruebas, documentación y sustentación | 29 | Por iniciar |

### Sprint 1

> **Sprint Goal:** Tener el Moodle de laboratorio configurado y conectado por servicios web, el modelo de datos creado, la autenticación con autorización por curso, la pantalla de cursos y la primera sincronización verificable.

7 HU · 35 TA · 39 SP. Estado según los PR de GitHub al 22 oct 2026: «Finalizada» si todos sus PR están fusionados en `develop`, «En revisión» si hay un PR abierto, «Parcial» si hay TA hechas y otras no, «Pendiente» si no hay nada.

| HU | Historia | Responsable | SP | Estado | PR | Evidencia |
|---|---|---|---|---|---|---|
| HU-01 | Moodle de laboratorio con la estructura UTA | Sebastián Freire | 8 | Parcial: TA-01.1 hecha; faltan TA-01.2 a TA-01.6 | [#1](https://github.com/Balseca-Kevin/edumonitor-ai/pull/1) | [HU-01](docs/evidencias/sprint-1/HU-01/) |
| HU-02 | Servicios web de Moodle y conexión segura | Kevin Balseca | 5 | Parcial: TA-02.1 y TA-02.4 hechas; faltan TA-02.2, TA-02.3 y TA-02.5 | [#2](https://github.com/Balseca-Kevin/edumonitor-ai/pull/2) | [HU-02](docs/evidencias/sprint-1/HU-02/) |
| HU-03 | Modelo de datos con migraciones y semillas | Sebastián Freire | 5 | Pendiente: ninguna TA-03.x cerrada (entidades mínimas creadas en #3 y #4 como base) | — | [HU-03](docs/evidencias/sprint-1/HU-03/) |
| HU-04 | Autenticación JWT y autorización por rol y por curso | Kevin Balseca | 5 | Finalizada | [#3](https://github.com/Balseca-Kevin/edumonitor-ai/pull/3) | [HU-04](docs/evidencias/sprint-1/HU-04/) |
| HU-05 | Sincronización manual de un curso | Kevin Balseca | 8 | Parcial: TA-05.2, TA-05.3 y TA-05.5 hechas; faltan TA-05.1, TA-05.4 y TA-05.6 | [#4](https://github.com/Balseca-Kevin/edumonitor-ai/pull/4) | [HU-05](docs/evidencias/sprint-1/HU-05/) |
| HU-06 | Pantallas de inicio de sesión y Mis cursos | Santiago Escobar | 5 | Pendiente | — | [HU-06](docs/evidencias/sprint-1/HU-06/) |

Índice completo de evidencias: [docs/evidencias/sprint-1/](docs/evidencias/sprint-1/README.md).

## Contribuciones del Sprint 1

Commits sin merges en `develop` (`git shortlog -sn --no-merges origin/develop`, 22 oct 2026) y TA cerradas según los PR fusionados.

| Integrante | Commits en `develop` | TA cerradas (PR) |
|---|---|---|
| Kevin Balseca | 17 | TA-01.1 (#1) · TA-02.1, TA-02.4 y base de la API (#2) · TA-04.1, TA-04.2, TA-04.3, TA-04.4 (#3) · TA-05.2, TA-05.3, TA-05.5 (#4) |
| Sebastián Freire | 0 | Ninguna en `develop` todavía · aprobó la revisión de #1 a #4 |
| Santiago Escobar | 0 | Ninguna en `develop` todavía |
| Jeremy Guamán | 0 | Ninguna en `develop` todavía |

## Puesta en marcha local

Requisitos: Git, Docker Desktop, .NET 8 SDK y Node.js LTS. Todos los comandos se ejecutan desde la raíz del repositorio.

1. **Variables de entorno.** Copia `.env.example` a `.env` y cambia las contraseñas. `.env` nunca se sube.
   ```powershell
   Copy-Item .env.example .env
   ```
2. **Moodle y SQL Server** (detalle en [moodle-lab/README.md](moodle-lab/README.md)):
   ```powershell
   docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d
   ```
3. **Servicios web de Moodle** (una vez por equipo). Crea el servicio `edumonitor_ws`, el usuario técnico de solo lectura y su token. Copia el token que imprime a `Moodle__Token` en `.env`:
   ```powershell
   docker cp moodle-lab/scripts/configurar_servicios_web.php edumonitor-moodle-1:/tmp/
   docker exec edumonitor-moodle-1 php /var/www/html/admin/cli/cfg.php --name=enablewebservices --set=1
   docker exec edumonitor-moodle-1 php /var/www/html/admin/cli/cfg.php --name=webserviceprotocols --set=rest
   docker exec edumonitor-moodle-1 php /tmp/configurar_servicios_web.php
   ```
4. **Backend.** En Development, la API lee `.env`, aplica las migraciones de EF Core y crea los usuarios semilla (`admin`, `doc01`, `doc02`) con la contraseña de `Semillas__Contrasena`:
   ```powershell
   dotnet run --project backend/EduMonitor.Api
   ```
   Swagger: <http://localhost:5000/swagger>. Para aplicar las migraciones sin arrancar la API: `dotnet ef database update --project backend/EduMonitor.Infrastructure --startup-project backend/EduMonitor.Api`.
5. **Pruebas:** `dotnet test backend/EduMonitor.sln`.

El cliente real de Moodle (TA-02.2) aún no está; mientras tanto la sincronización responde 502 y queda registrada como `FALLIDA`.

## Documentación

- [CONTRIBUTING.md](CONTRIBUTING.md): Git Flow, commits, revisiones y Definition of Done.
- [docs/contratos-api.md](docs/contratos-api.md): rutas, JSON y códigos de respuesta del backend.
- [docs/planificacion/](docs/planificacion/): planificación Scrum del Sprint 1 y especificación del docente.
- [docs/evidencias/sprint-1/](docs/evidencias/sprint-1/README.md): evidencias para la Sprint Review.
- [moodle-lab/README.md](moodle-lab/README.md): Moodle de laboratorio en Docker.
