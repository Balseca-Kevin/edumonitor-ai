# Instrucciones para GitHub Copilot · EduMonitor AI

Proyecto: aplicación web que lee un Moodle de laboratorio por servicios web REST (solo lectura), guarda los datos en SQL Server, calcula indicadores y alertas en el backend y tiene un agente de IA que consulta el backend con herramientas. Contexto completo en `docs/CONTEXTO_IA.md`; rutas y JSON en `docs/contratos-api.md`; sprint activo en `docs/sprint-2/PLAN.md`; interfaz en `docs/sprint-2/diseno.md`.

Stack fijo:
- `backend/`: ASP.NET Core Web API .NET 8 (`net8.0`), controladores, EF Core 8 + SQL Server, JWT, xUnit. Paquetes `Microsoft.*` en 8.0.x. Capas: Api → Application → Domain, Infrastructure implementa interfaces de Application. Reglas en `backend/AGENTS.md`.
- `frontend/`: React 18 + Vite, React Router, Axios, Recharts, Vitest. Solo llama al backend. Reglas en `frontend/AGENTS.md`.
- `agent/`: Python 3.11, FastAPI, LangGraph, Pydantic, Ollama, pytest. Solo llama a `/api/tools/*` y a Ollama. Reglas en `agent/AGENTS.md`.
- Dos entornos Docker: EduMonitor con su agente en la raíz (`docker compose up -d`, una sola dirección `http://localhost:5173`) y el aula virtual aparte en `moodle-lab/`. EduMonitor solo conoce Moodle por su URL y su token.

Al sugerir código:
- Nombres del dominio en español sin tildes (`Curso`, `Calificacion`). Estados de calificación: `SIN_ENTREGA`, `PENDIENTE`, `CALIFICADO`.
- Errores HTTP como `ProblemDetails`. 401 sin token, 403 para curso ajeno. En la interfaz, 401 y 403 llevan a sus pantallas.
- Toda funcionalidad termina en una pantalla. Nada que solo se pueda probar por consola.
- Secretos solo desde variables de entorno; nunca valores literales de tokens o contraseñas.
- Moodle: solo `core_webservice_get_site_info`, `core_enrol_get_users_courses`, `core_course_get_contents`, `core_enrol_get_enrolled_users`, `gradereport_user_get_grade_items`, `mod_assign_get_assignments`, `mod_assign_get_submissions`, `core_completion_get_activities_completion_status`.
- Incluye pruebas para lógica nueva.

Integración: todo Pull Request hacia `develop` o `main` necesita la aprobación de Kevin (CODEOWNERS). Kevin es el único que fusiona; sus propios Pull Request los fusiona con el bypass del ruleset, porque GitHub no deja aprobar el Pull Request propio.

Copilot revisa un Pull Request solo si Kevin lo pide. En ese caso comprueba: que apunte a `develop`, que no haya secretos ni `.env`, que no se rompan las capas del backend, que las rutas y JSON coincidan con `docs/contratos-api.md`, que la interfaz use los tokens de `diseno.md` y que los commits sigan Conventional Commits en español.
