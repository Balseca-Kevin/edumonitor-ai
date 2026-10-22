# EduMonitor AI · instrucciones para Claude Code

@docs/CONTEXTO_IA.md
@docs/contratos-api.md

## Documentos fuente

- `docs/planificacion/Planificacion_Proyecto_Scrum_Sprint1.pdf`: historias de usuario, criterios DADO / CUANDO / ENTONCES, tareas técnicas TA con responsable y horas, Definition of Done. Léelo antes de empezar una HU.
- `docs/planificacion/Proyecto_EduMonitor_AI_Especificacion.pdf`: requisitos RF y RNF del docente.

## Cómo trabajar en este repositorio

1. Cada sesión trabaja una tarea TA a la vez. Antes de escribir código, presenta un plan con los archivos que vas a crear o modificar y espera confirmación.
2. Respeta las capas del backend (ver `backend/CLAUDE.md`). Si una tarea parece pedir romperlas, detente y pregunta.
3. No inventes rutas, campos, entidades ni funciones de Moodle. Si falta algo en `docs/contratos-api.md`, propón el cambio del contrato primero.
4. Todo cambio de comportamiento lleva pruebas. Corre `dotnet test backend/EduMonitor.sln` antes de dar una tarea por terminada.
5. No leas ni escribas `.env`. Usa `.env.example` para saber qué variables existen. No pongas secretos en código, pruebas, logs ni capturas.
6. No hagas `git push`, `git push --force`, merges a `develop` o `main`, ni cambies de rama sin que te lo pida. Puedes preparar commits en la rama actual con Conventional Commits en español y el pie `Refs: SCRUM-n` que te indique.
7. Paquetes NuGet `Microsoft.*` en versión 8.0.x. No subas el proyecto a .NET 9 o 10.
8. Al terminar una tarea, resume: qué archivos cambiaron, cómo probarlo a mano (comando o petición de Postman) y qué evidencia capturar para la Sprint Review.
