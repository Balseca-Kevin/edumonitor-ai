# Cómo contribuir a EduMonitor

Trabajamos con Scrum: cada cambio sale de una historia de usuario (HU) y una tarea técnica (TA) del tablero de Jira, y entra a `develop` por Pull Request revisado.

## Ramas (Git Flow)

| Rama | Uso |
|---|---|
| `main` | Lo entregado al cierre de cada sprint. Solo recibe `release/*` y `hotfix/*`. |
| `develop` | Integración del sprint. Rama por defecto. |
| `feature/HU-xx-descripcion` | Una por historia, sale de `develop` (ej. `feature/HU-04-auth-jwt`). |
| `release/sprint-n` | Preparación de la entrega del sprint n; se fusiona en `main` y `develop`. |
| `hotfix/descripcion` | Corrección urgente sobre `main`. |

El ruleset `proteger-main-develop` exige en `main` y `develop`: Pull Request con **1 aprobación**, CI en verde (`backend` y `frontend`), **sin force push** y sin borrar la rama. Nadie sube directo a `main` ni a `develop`.

## Commits

Conventional Commits en español e imperativo, con el pie de la tarjeta de Jira:

```
feat(auth): agregar POST /api/auth/login con contraseñas cifradas

Refs: SCRUM-n
```

- **Tipos:** `feat`, `fix`, `test`, `docs`, `refactor`, `chore`, `ci`, `build`.
- **Ámbitos:** `moodle`, `db`, `backend`, `auth`, `sync`, `frontend`, `docs`, `ci`.

## Pull Requests y revisión

- Usa la plantilla `.github/pull_request_template.md` completa: qué hace, cómo probarlo, evidencia y Definition of Done.
- Pares de revisión: **Kevin ↔ Sebastián** (backend, Moodle y datos) y **Santiago ↔ Jeremy** (interfaz).
- Un cambio en `docs/contratos-api.md` va en un commit `docs(backend): ...` y se avisa en el grupo antes de fusionarlo.
- Cada cambio trae pruebas: xUnit en backend y Vitest en frontend.

## Definition of Done (planificación, sección 5.4)

Una historia de usuario está terminada cuando:

- El código está en una rama feature, revisado por al menos otro integrante y fusionado en `develop` siguiendo GitFlow.
- Compila y funciona en local sin errores, siguiendo el README.
- Tiene pruebas automatizadas que pasan en la integración continua: unitarias para los cálculos, de integración para Moodle y de permisos para los endpoints.
- Cumple los criterios de aceptación y está demostrada con datos del Moodle de laboratorio.
- No expone secretos ni datos identificables fuera del sistema.
- La documentación afectada (endpoints, README, diagramas) está actualizada y la evidencia MK está adjunta.
- La tarjeta está en Finalizada en el tablero de Jira.

## Secretos

Solo en `.env` (nunca versionado). `.env.example` lista las variables con valores de prueba. Ni tokens ni contraseñas en código, pruebas, logs, capturas o evidencias: se tapan como `••••` más los últimos 4 caracteres.
