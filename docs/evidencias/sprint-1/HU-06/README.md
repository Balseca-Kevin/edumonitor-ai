# HU-06 · Pantallas de inicio de sesión y Mis cursos

**Responsable:** Santiago Escobar · **Estado:** Falta

**Evidencia que pide la planificación (6.3):** pantallas funcionando con búsqueda, filtros y estado de sincronización, con datos del Moodle de laboratorio.

| Archivo | Quién lo sube | Contenido |
|---|---|---|
| `HU-06_login.png` | Santiago | Pantalla de inicio de sesión, incluido el mensaje de error con credenciales incorrectas |
| `HU-06_mis_cursos.png` | Santiago | Mis cursos de doc01 con búsqueda, filtro por periodo y fecha de la última sincronización |
| `HU-06_prueba_interfaz.md` | Santiago (TA-06.5) | Salida de la prueba de Vitest del flujo inicio de sesión → cursos |

El backend necesario ya está en `develop`: `POST /api/auth/login`, `GET /api/cursos` y `POST/GET /api/cursos/{id}/sync` (ver `docs/contratos-api.md`).
