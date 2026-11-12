# EduMonitor AI · instrucciones para Claude Code

@docs/CONTEXTO_IA.md
@docs/contratos-api.md
@docs/sprint-2/PLAN.md

Sprint activo: Sprint 2 hasta la Review del jueves 12 de noviembre de 2026; el siguiente es el Sprint 3. Sprint 2 cerrado con las 6 HU hechas (v0.2.1, 12 nov 2026).

## Documentos fuente

- `docs/sprint-2/PLAN.md`: Sprint Goal, HU del Sprint 2, tareas TA con responsable, horas y fecha límite, orden de trabajo. Léelo antes de empezar una tarea.
- `docs/sprint-2/diseno.md`: sistema de diseño de la interfaz. Obligatorio para cualquier cambio en `frontend/`.
- `docs/planificacion/Planificacion_Proyecto_Scrum_Sprint1.pdf`: requisitos RF y RNF, indicadores (tabla 5), reglas de alerta (tabla 6), herramientas del agente (tabla 3) y endpoints (tabla 4).
- `docs/planificacion/Proyecto_EduMonitor_AI_Especificacion.pdf`: especificación del docente.

## Regla de la Sprint Review

El Product Owner dijo en la Review del Sprint 1: «TODO debe estar en el Software, nada por consola o archivos». Por eso:

- Toda HU termina en una pantalla de la aplicación que la demuestra. Un endpoint sin pantalla no está terminado.
- Ningún criterio de aceptación se demuestra con Postman, Swagger, curl, la terminal o un archivo. Los errores (401, 403, 502, agente caído) también se ven en pantalla.
- Si una tarea solo se puede probar por consola, detente y propón la pantalla o el cambio de pantalla que la vuelve visible.

## Cómo trabajar en este repositorio

1. Una tarea TA por sesión. Antes de escribir código, presenta un plan con los archivos que vas a crear o modificar y espera confirmación.
2. Respeta las capas del backend (`backend/AGENTS.md`) y las reglas de la interfaz (`frontend/AGENTS.md`) y del agente (`agent/AGENTS.md`). Si una tarea parece pedir romperlas, detente y pregunta.
3. No inventes rutas, campos, entidades ni funciones de Moodle. Si falta algo en `docs/contratos-api.md`, propón el cambio del contrato primero.
4. Todo cambio de comportamiento lleva pruebas. Antes de dar una tarea por terminada corre lo que toque: `dotnet test backend/EduMonitor.sln`, `npm test -- --run` en `frontend/`, `python -m pytest` en `agent/`.
5. No leas ni escribas `.env`. Usa `.env.example` para saber qué variables existen. No pongas secretos en código, pruebas, logs ni capturas.
6. No hagas `git push`, merges a `develop` o `main`, ni cambies de rama sin que te lo pida. Puedes preparar commits en la rama actual con Conventional Commits en español y el pie `Refs: SCRUM-n` que te indique.
7. Integración: todo Pull Request hacia `develop` o `main` necesita la aprobación de Kevin (CODEOWNERS). Kevin es el único que fusiona; sus propios Pull Request los fusiona con el bypass del ruleset, porque GitHub no deja aprobar el Pull Request propio. Requisito para fusionar: integración continua en verde y la HU visible en la aplicación.
8. Paquetes NuGet `Microsoft.*` en versión 8.0.x. No subas el proyecto a .NET 9 o 10.
9. Hay dos entornos Docker separados, como pide la especificación (EduMonitor es independiente de Moodle). EduMonitor con su agente: `docker compose up -d` en la raíz, todo en una sola dirección (`http://localhost:5173`). Aula virtual: `docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d`. EduMonitor solo conoce Moodle por su URL y su token; ningún servicio de la raíz depende de un servicio de `moodle-lab/`. Si cambias un puerto, una variable o un servicio, actualiza el compose afectado, `.env.example` y el README en el mismo commit. Nunca `docker compose down -v` ni `docker volume rm`: borran Moodle y su token.
10. Al terminar una tarea, resume: archivos cambiados, cómo verlo en la aplicación (pantalla y pasos) y qué captura guardar para el informe del Sprint 2.
