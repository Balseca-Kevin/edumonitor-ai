# EduMonitor AI

Sistema de seguimiento y análisis del rendimiento académico integrado con Moodle, con un agente de IA para la detección temprana y el refuerzo académico. Proyecto final de Metodologías Ágiles, UTA · FISEI · Software 4to «B», julio–diciembre 2026.

Equipo: Kevin Balseca (Scrum Master, backend y Moodle) · Sebastián Freire (datos y analítica) · Santiago Escobar (interfaz) · Jeremy Guamán (agente de IA). Product Owner: Ing. Hernán Fabricio Naranjo Ávalos.

## Estructura

| Carpeta | Contenido |
|---|---|
| `moodle-lab/` | Docker Compose de Moodle y SQL Server, respaldos `.mbz`, CSV de datos ficticios, guía de restauración |
| `backend/` | ASP.NET Core Web API .NET 8 por capas: Api, Application, Domain, Infrastructure, Tests |
| `agent/` | Servicio FastAPI + LangGraph con Ollama |
| `frontend/` | React 18 + Vite |
| `docs/` | Contexto para IA, contratos de API, planificación, diagramas, evidencias |

## Requisitos

Git, Docker Desktop, .NET 8 SDK, Node.js LTS, Python 3.11, Ollama. Enlaces y versiones en la guía del equipo.

## Puesta en marcha local

```
git clone <url-del-repo>
cd edumonitor-ai
copy .env.example .env        # en PowerShell: Copy-Item .env.example .env
docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d
dotnet ef database update --project backend/EduMonitor.Infrastructure --startup-project backend/EduMonitor.Api
dotnet run --project backend/EduMonitor.Api
```

Los pasos del frontend y del agente se agregan en este README con sus primeras historias (HU-06 y HU-07).

## Puertos

| Servicio | Puerto |
|---|---|
| Moodle | 8080 |
| SQL Server | 1433 |
| Backend | 5000 |
| Frontend | 5173 |
| Agente | 8000 |
| Ollama | 11434 |

## Forma de trabajo

- Git Flow: `main`, `develop`, `feature/HU-xx-descripcion`, `release/sprint-n`. Todo entra a `develop` por Pull Request revisado y con CI en verde.
- Commits: Conventional Commits en español, `tipo(ámbito): descripción`, pie `Refs: SCRUM-n`.
- Contratos de API: `docs/contratos-api.md`. Contexto para asistentes de IA: `docs/CONTEXTO_IA.md`.
