# EduMonitor AI · instrucciones para agentes de código

Este archivo lo leen GitHub Copilot (modo agente y revisión de código) y otros agentes. Claude Code usa `CLAUDE.md`, que carga los mismos documentos.

Antes de cualquier cambio, lee:

1. `docs/CONTEXTO_IA.md`: qué es el proyecto, stack fijo, arquitectura, reglas del dominio, funciones de Moodle permitidas y convenciones.
2. `docs/contratos-api.md`: rutas, JSON y códigos de respuesta del Sprint 1. Manda sobre cualquier suposición.
3. El `AGENTS.md` de la carpeta donde vas a trabajar, si existe (`backend/AGENTS.md`).

Reglas que no se negocian:

- Stack: .NET 8 con EF Core 8 y SQL Server, React 18 + Vite, Python 3.11 con FastAPI + LangGraph + Ollama. Paquetes `Microsoft.*` en 8.0.x.
- Moodle es solo lectura y solo con las 8 funciones listadas en el contexto.
- El agente de IA nunca calcula notas ni lee la base: usa `/api/tools/*` del backend.
- Secretos solo en variables de entorno. Nunca leer, crear ni modificar `.env`.
- Cada cambio de comportamiento con pruebas (xUnit, Vitest o pytest).
- Commits: Conventional Commits en español, `tipo(ámbito): descripción`, pie `Refs: SCRUM-n`.
- No construir funcionalidades de los Sprints 2 y 3 (dashboard, alertas, informes) mientras el sprint activo sea el 1.
