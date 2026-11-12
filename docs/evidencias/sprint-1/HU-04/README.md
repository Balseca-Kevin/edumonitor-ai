# HU-04 · Autenticación JWT y autorización por rol y por curso

**Responsable:** Kevin Balseca · **Estado:** Parcial (falta la captura de Postman)

**Evidencia que pide la planificación (6.3):** capturas con las respuestas 200, 403 y 401.

| Archivo | Quién lo sube | Contenido |
|---|---|---|
| [`TA-04.4_permisos.md`](TA-04.4_permisos.md) | Kevin | Respuestas reales de la API en texto (login 200, contraseña incorrecta 401, sin token 401, curso ajeno 403, curso propio 200) con el token tapado |
| `TA-04.4_postman_200.png` | Kevin | Login de doc01 y `GET /api/cursos/{id de AGIL-4B}` con 200 |
| `TA-04.4_postman_403.png` | Kevin | doc01 pidiendo `GET /api/cursos/{id de BDD-4B}`: 403 |
| `TA-04.4_postman_401.png` | Kevin | `GET /api/cursos` sin token: 401 |

Pruebas automatizadas: `PermisosTests` (14 pruebas) en [HU-05/pruebas.md](../HU-05/pruebas.md).
