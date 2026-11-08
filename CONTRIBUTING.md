# Cómo contribuir a EduMonitor AI

Trabajamos con Scrum: cada cambio sale de una historia de usuario (HU) y una tarea técnica (TA) del tablero de Jira, y entra a `develop` por Pull Request aprobado y fusionado por Kevin.

## Ramas (Git Flow)

| Rama | Uso |
|---|---|
| `main` | Lo entregado al cierre de cada sprint. Solo recibe `release/*` y `hotfix/*`. |
| `develop` | Integración del sprint. Rama por defecto. |
| `feature/HU-xx-descripcion` | Una por historia, sale de `develop` (ej. `feature/HU-04-auth-jwt`). |
| `release/sprint-n` | Preparación de la entrega del sprint n; se fusiona en `main` y `develop`. |
| `hotfix/descripcion` | Corrección urgente sobre `main`. |

El ruleset `proteger-main-develop` exige en `main` y `develop`: Pull Request con **aprobación del code owner**, CI en verde (`backend`, `frontend` y `agent`), **sin force push** y sin borrar la rama. Solo quien está en la lista de bypass del ruleset puede actualizar `develop` y `main`. Nadie sube directo a `main` ni a `develop`.

## Commits

Conventional Commits en español e imperativo, con el pie de la tarjeta de Jira:

```
feat(auth): agregar POST /api/auth/login con contraseñas cifradas

Refs: SCRUM-n
```

- **Tipos:** `feat`, `fix`, `test`, `docs`, `refactor`, `style`, `chore`, `ci`, `build`.
- **Ámbitos:** `moodle`, `db`, `backend`, `auth`, `sync`, `indicadores`, `alertas`, `frontend`, `agent`, `docker`, `docs`, `ci`.

## Pull Requests e integración (Sprint 2)

La política de integración del Sprint 2 reemplaza la revisión por pares del Sprint 1 (Kevin ↔ Sebastián, Santiago ↔ Jeremy).

- Cada integrante empuja su rama `feature/HU-xx-descripcion` y abre un Pull Request hacia `develop`.
- **Kevin es el único que aprueba** y el único que fusiona en `develop` y `main`.
- `.github/CODEOWNERS` pide la aprobación de Kevin en todo Pull Request hacia `develop` o `main`.
- El Pull Request se fusiona solo con la CI en verde y la aprobación de Kevin, como exige el ruleset `proteger-main-develop`.
- GitHub no deja aprobar el Pull Request propio, así que Kevin fusiona los suyos con el bypass del ruleset, también con la CI en verde.
- Usa la plantilla `.github/pull_request_template.md` completa: qué hace, cómo probarlo, evidencia y Definition of Done.
- Un cambio en `docs/contratos-api.md` va en un commit `docs(backend): ...` y se avisa en el grupo antes de fusionarlo.
- Cada cambio trae pruebas: xUnit en backend, Vitest en frontend y pytest en agente.

## Definition of Done (Sprint 2, `docs/sprint-2/PLAN.md`)

Una HU está terminada cuando:

- Funciona dentro de la aplicación levantada con `docker compose up -d` en la raíz, con el aula virtual de `moodle-lab/` encendida aparte y sus datos de laboratorio.
- Cada criterio de aceptación se puede mostrar en pantalla, sin terminal, Postman, Swagger ni archivos.
- Tiene pruebas automatizadas que pasan en la integración continua.
- Respeta `docs/contratos-api.md` y, si toca la interfaz, `docs/sprint-2/diseno.md`.
- No expone secretos ni datos identificables.
- Está fusionada en `develop` por Kevin y su tarjeta está en «Finalizada» en Jira.
- Su responsable puede explicar qué pide la historia, qué tablas lee, qué endpoint llama la pantalla y qué prueba la cubre.

## Secretos

Solo en `.env` (nunca versionado). `.env.example` lista las variables con valores de prueba. Ni tokens ni contraseñas en código, pruebas, logs, capturas o evidencias: se tapan como `••••` más los últimos 4 caracteres.

## Asistentes de IA

- `CLAUDE.md` (Claude Code), `AGENTS.md` y `backend/AGENTS.md` (Codex y otros agentes) y `.github/copilot-instructions.md` (Copilot) cargan el contexto del repo.
- Para una IA sin acceso al repo, pega [docs/CONTEXTO_IA.md](docs/CONTEXTO_IA.md) al inicio de la conversación.
- Lo que genere una IA se revisa igual que cualquier código: pruebas en verde, aprobación de Kevin y nada de secretos. Las notas personales van en `CLAUDE.local.md`, que está en `.gitignore`.
