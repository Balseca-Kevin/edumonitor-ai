# EduMonitor AI · instrucciones para agentes de código

Este archivo lo leen GitHub Copilot (modo agente y revisión de código) y otros agentes. Claude Code usa `CLAUDE.md`, que carga los mismos documentos.

Antes de cualquier cambio, lee:

1. `docs/CONTEXTO_IA.md`: qué es el proyecto, stack fijo, arquitectura, reglas del dominio, funciones de Moodle permitidas y convenciones.
2. `docs/contratos-api.md`: rutas, JSON y códigos de respuesta de los Sprints 1, 2 y 3. Manda sobre cualquier suposición.
3. `docs/sprint-3/PLAN.md`: HU y tareas del sprint activo.
4. El `AGENTS.md` de la carpeta donde vas a trabajar: `backend/AGENTS.md`, `frontend/AGENTS.md` o `agent/AGENTS.md`.

Reglas que no se negocian:

- Stack: .NET 8 con EF Core 8 y SQL Server, React 18 + Vite, Python 3.11 con FastAPI + LangGraph. Modelo: proveedor externo autorizado por el Product Owner, con Ollama (`qwen2.5:7b`) como respaldo local. Paquetes `Microsoft.*` en 8.0.x.
- Todo se demuestra en la aplicación. Nada por consola, Postman, Swagger ni archivos.
- La aplicación muestra las notas reales: toda cifra se puede abrir para ver de qué notas sale.
- Moodle es solo lectura y solo con las 11 funciones listadas en el contexto.
- Ningún nombre, correo ni id de Moodle de un estudiante sale al modelo externo: el backend los cambia por códigos (E-001, E-002…).
- El agente de IA nunca calcula notas ni lee la base: usa `/api/tools/*` del backend con el token de servicio.
- Secretos solo en variables de entorno. Nunca leer, crear ni modificar `.env`.
- Cada cambio de comportamiento con pruebas (xUnit, Vitest o pytest).
- Commits: Conventional Commits en español, `tipo(ámbito): descripción`, pie `Refs: SCRUM-n`.
- Sprint activo: 3, del 16 al 27 nov 2026 (Review final el viernes 27). HU del sprint: HU-21, HU-10, HU-08, HU-22 (absorbe HU-11), HU-15, HU-23, HU-16, HU-17 y HU-18. Los agregados son tareas dentro de esas HU, no HU nuevas.
- Fuera de alcance: entrenar modelos propios, multiagentes, microservicios, escribir en Moodle o modificar notas, mensajes automáticos a estudiantes y predicción clínica o psicológica.
