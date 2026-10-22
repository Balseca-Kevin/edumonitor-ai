# Contratos de API · Sprint 1 (versión 1.0, 21 oct 2026)

Este archivo define lo que el frontend y el agente pueden pedir al backend en el Sprint 1. Santiago y Jeremy trabajan contra estos JSON con datos simulados mientras el backend se termina. Un cambio aquí se hace con un commit `docs(backend): ...` y se avisa en el grupo antes de fusionarlo.

## Reglas generales

- Base local del backend: `http://localhost:5000`. Swagger en `/swagger`.
- Autenticación: cabecera `Authorization: Bearer <jwt>` en todo excepto `POST /api/auth/login`.
- JSON en camelCase. Fechas en ISO 8601 con zona (`2026-10-22T09:15:00-05:00`).
- Errores con `ProblemDetails` (RFC 7807):

```json
{
  "type": "https://httpstatuses.io/403",
  "title": "Acceso denegado",
  "status": 403,
  "detail": "El curso 7 no está asignado a este docente."
}
```

| Código | Cuándo |
|---|---|
| 200 / 204 | Correcto |
| 400 | Datos de entrada inválidos |
| 401 | Sin token, token vencido o credenciales incorrectas |
| 403 | Token válido pero sin permiso (curso ajeno, rol insuficiente) |
| 404 | Recurso inexistente |
| 502 | Moodle no responde o devuelve error |

- Roles en el JWT: `DOCENTE` o `ADMIN` (claim `role`). El id del usuario va en `sub`.

## Autenticación (HU-04)

### POST /api/auth/login · público

Petición:
```json
{ "usuario": "doc01", "contrasena": "valor-de-prueba" }
```

Respuesta 200:
```json
{
  "token": "eyJhbGciOi...",
  "expiraEn": "2026-10-22T11:15:00-05:00",
  "usuario": { "id": 2, "usuario": "doc01", "nombre": "Docente Uno", "rol": "DOCENTE" }
}
```

Respuesta 401: `ProblemDetails` con `title: "Credenciales incorrectas"`. El mensaje no dice si falló el usuario o la contraseña.

## Cursos (HU-06, HU-04)

### GET /api/cursos · DOCENTE

Parámetros opcionales: `busqueda` (texto en nombre o nombre corto), `periodo` (ej. `2026-2`).

Respuesta 200 (solo cursos asignados al docente del token; un ADMIN recibe todos):
```json
[
  {
    "id": 1,
    "moodleId": 3,
    "nombreCorto": "AGIL-4B",
    "nombre": "Metodologías Ágiles (lab)",
    "periodo": "2026-2",
    "ultimaSincronizacion": {
      "id": 5,
      "fecha": "2026-10-22T09:15:00-05:00",
      "estado": "COMPLETADA"
    }
  }
]
```
`ultimaSincronizacion` es `null` si el curso nunca se sincronizó.

### GET /api/cursos/{id} · DOCENTE

Respuesta 200: el mismo objeto de la lista. 403 si el curso no es del docente. 404 si no existe.

## Sincronización (HU-05)

### POST /api/cursos/{id}/sync · DOCENTE

Sin cuerpo. Ejecuta la sincronización y responde al terminar.

Respuesta 200:
```json
{
  "id": 6,
  "cursoId": 1,
  "estado": "PARCIAL",
  "inicio": "2026-10-22T09:20:01-05:00",
  "fin": "2026-10-22T09:20:07-05:00",
  "origen": "http://localhost:8080",
  "registros": { "unidades": 4, "actividades": 16, "estudiantes": 32, "calificaciones": 480 },
  "errores": [
    { "funcion": "core_completion_get_activities_completion_status", "mensaje": "Acceso denegado para el usuario técnico" }
  ]
}
```
`estado`: `COMPLETADA` (sin errores), `PARCIAL` (al menos una función falló y otras no), `FALLIDA` (no se importó nada). 403 si el curso es ajeno. 502 si Moodle no responde.

### GET /api/cursos/{id}/sync · DOCENTE

Historial, de la más reciente a la más antigua. Respuesta 200: lista de objetos con la misma forma que la respuesta del POST.

## Configuración de Moodle (HU-02)

### GET /api/admin/moodle-config · ADMIN

```json
{ "url": "http://localhost:8080", "servicio": "edumonitor_ws", "tokenEnmascarado": "••••••••3f9a", "actualizado": "2026-10-21T22:10:00-05:00" }
```
El token completo nunca sale del backend ni aparece en registros.

### PUT /api/admin/moodle-config · ADMIN

```json
{ "url": "http://localhost:8080", "token": "token-real-del-usuario-tecnico" }
```
Respuesta 204. 400 si la URL no es válida.

### POST /api/admin/moodle-config/test · ADMIN

Sin cuerpo. Llama a `core_webservice_get_site_info` y revisa qué funciones de la lista permitida están habilitadas.

Respuesta 200:
```json
{
  "conectado": true,
  "sitio": "Moodle Lab UTA",
  "version": "4.5",
  "usuarioTecnico": "edumonitor_ws_user",
  "funciones": [
    { "nombre": "core_webservice_get_site_info", "disponible": true },
    { "nombre": "core_completion_get_activities_completion_status", "disponible": false }
  ]
}
```
Si Moodle no responde: 200 con `"conectado": false` y `"error": "texto útil"`. Una función no disponible no bloquea a las demás.

## Herramientas para el agente (HU-07)

En el Sprint 1 el agente reenvía el JWT del docente que hace la pregunta. En el Sprint 2 se cambia por el proxy `POST /api/agente/chat` y un token de servicio, como dice la planificación.

### GET /api/tools/cursos-autorizados · DOCENTE

Respuesta 200:
```json
{
  "herramienta": "listar_cursos_autorizados",
  "generado": "2026-10-22T09:30:00-05:00",
  "cursos": [
    { "id": 1, "nombreCorto": "AGIL-4B", "nombre": "Metodologías Ágiles (lab)", "ultimaSincronizacion": "2026-10-22T09:15:00-05:00" }
  ]
}
```
401 sin token. El backend registra cada llamada (herramienta, usuario, fecha, resultado) sin guardar el token.

## Servicio de agente (HU-07)

Base local: `http://localhost:8000`.

### POST /chat

Petición:
```json
{ "mensaje": "¿Qué cursos tengo asignados?", "token": "<jwt del docente>" }
```

Respuesta 200:
```json
{
  "respuesta": "Tienes 1 curso asignado: Metodologías Ágiles (lab), sincronizado el 22/10/2026 a las 09:15.",
  "herramientas": [
    { "nombre": "listar_cursos_autorizados", "parametros": {}, "estado": "OK", "duracionMs": 84 }
  ],
  "modelo": "qwen2.5:7b",
  "duracionMs": 6120
}
```
Si la herramienta falla, `estado` es `"ERROR"`, la respuesta dice que no pudo consultar los cursos y no lista cursos inventados.
