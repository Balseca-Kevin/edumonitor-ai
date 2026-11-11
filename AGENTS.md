# EduMonitor AI · instrucciones para agentes de código

Este archivo lo leen GitHub Copilot (modo agente y revisión de código) y otros agentes. Claude Code usa `CLAUDE.md`, que carga los mismos documentos.

Antes de cualquier cambio, lee:

1. `docs/CONTEXTO_IA.md`: qué es el proyecto, stack fijo, arquitectura, reglas del dominio, funciones de Moodle permitidas y convenciones.
2. `docs/contratos-api.md`: rutas, JSON y códigos de respuesta de los Sprints 1 y 2. Manda sobre cualquier suposición.
3. `docs/sprint-2/PLAN.md`: HU y tareas del sprint activo.
4. El `AGENTS.md` de la carpeta donde vas a trabajar: `backend/AGENTS.md`, `frontend/AGENTS.md` o `agent/AGENTS.md`.

Reglas que no se negocian:

- Stack: .NET 8 con EF Core 8 y SQL Server, React 18 + Vite, Python 3.11 con FastAPI + LangGraph + Ollama. Paquetes `Microsoft.*` en 8.0.x.
- Todo se demuestra en la aplicación. Nada por consola, Postman, Swagger ni archivos.
- Moodle es solo lectura y solo con las 8 funciones listadas en el contexto.
- El agente de IA nunca calcula notas ni lee la base: usa `/api/tools/*` del backend con el token de servicio.
- Secretos solo en variables de entorno. Nunca leer, crear ni modificar `.env`.
- Cada cambio de comportamiento con pruebas (xUnit, Vitest o pytest).
- Commits: Conventional Commits en español, `tipo(ámbito): descripción`, pie `Refs: SCRUM-n`.
- Sprint activo: 2. No construir todavía HU-08 (mapeo temático), HU-10 (ficha del estudiante), HU-11 (desempeño por tema), HU-15 (tareas de refuerzo), HU-16 (informe), HU-17 (auditoría) ni HU-18 (cierre).
