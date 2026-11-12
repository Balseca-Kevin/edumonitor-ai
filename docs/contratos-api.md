# Contratos de API · Sprints 1 y 2 (versión 1.0 del 21 oct 2026; bloque del Sprint 2 al final)

Este archivo define lo que el frontend y el agente pueden pedir al backend en los Sprints 1 y 2. Santiago y Jeremy trabajan contra estos JSON con datos simulados mientras el backend se termina. Un cambio aquí se hace con un commit `docs(backend): ...` y se avisa en el grupo antes de fusionarlo.

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
| 409 | Curso sin sincronizar (Sprint 2, indicadores) |
| 502 | Moodle no responde o devuelve error |
| 503 | Asistente no disponible (Sprint 2, chat) |

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
{ "url": "http://localhost:8080", "servicio": "edumonitor_ws", "tokenEnmascarado": "••••3f9a", "actualizado": "2026-10-21T22:10:00-05:00" }
```
`tokenEnmascarado` es «••••» más los últimos 4 caracteres del token (vacío si no hay token). El token completo nunca sale del backend ni aparece en registros.

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

---

# Contratos de API · Sprint 2 (versión 2.0, propuesta del 8 nov 2026)

Lo de arriba sigue vigente salvo estos cambios:

- La configuración de Moodle es persistente: se guarda en la base (HU-20).
- Las herramientas del agente aceptan el rol `AGENTE` con el token de servicio (HU-13, HU-14).
- `POST /chat` del agente tiene cuerpo nuevo: lleva `cursoId` y el token de servicio en lugar del JWT del docente.
- El agente no publica puerto: solo lo llama la API por la red interna.
- Códigos nuevos: 409 (curso sin sincronizar) y 503 (asistente no disponible).

Los cambios de este bloque se aprueban en la Sprint Planning del domingo 8 nov; después, cualquier cambio se avisa en el grupo antes de fusionarlo.

Base local con Docker: la aplicación en `http://localhost:5173` (nginx pasa `/api` a la API) y la API directa en `http://localhost:5000`.

## Configuración de Moodle persistente (HU-20, TA-20.4)

Mismas rutas y JSON que en el Sprint 1. Cambia el almacenamiento:

- `PUT /api/admin/moodle-config` guarda URL y token en la tabla `ConfiguracionMoodle` (una fila). El token se protege con ASP.NET Core Data Protection; las llaves viven en el volumen `api-keys` (`/keys`).
- Al arrancar, la API usa la fila de la base si existe; si no, usa `Moodle__Url` y `Moodle__Token` del entorno.
- `tokenEnmascarado` sigue siendo «••••» más los últimos 4 caracteres.

## Estado del sistema (HU-20, TA-20.5)

### GET /api/admin/estado · ADMIN

```json
{
  "generado": "2026-11-11T10:00:00-05:00",
  "servicios": [
    { "nombre": "api", "estado": "OK", "detalle": "versión 0.2.0" },
    { "nombre": "sqlserver", "estado": "OK", "detalle": "EduMonitorDB" },
    { "nombre": "moodle", "estado": "OK", "detalle": "Moodle-Lab-UTA 4.5" },
    { "nombre": "agente", "estado": "NO_DISPONIBLE", "detalle": "No respondió en 3 s" },
    { "nombre": "modelo", "estado": "NO_DISPONIBLE", "detalle": "Sin dato: el agente no respondió" }
  ]
}
```
`estado`: `OK` o `NO_DISPONIBLE`. Cada servicio se consulta con un tiempo máximo de 3 s y un servicio caído no detiene a los demás. Para `agente` y `modelo` la API llama a `GET /salud` del agente. 403 para DOCENTE.

## Indicadores del curso (HU-09)

### GET /api/cursos/{id}/indicadores · DOCENTE

Se calcula con los datos guardados de la última sincronización. 403 si el curso es ajeno. 404 si no existe. 409 con `ProblemDetails` («Curso sin sincronizar») si nunca se sincronizó.

```json
{
  "cursoId": 1,
  "escala": 10,
  "aprobacion": 7.0,
  "sincronizacion": { "id": 12, "fecha": "2026-11-09T07:10:00-05:00", "estado": "COMPLETADA" },
  "unidades": [
    { "numero": 1, "nombre": "Unidad 1", "parcial": 1, "actividades": 4, "calificadas": 4 }
  ],
  "curso": {
    "estudiantes": 32,
    "actividadesEvaluadas": 16,
    "promedio": 7.42,
    "mediana": 7.6,
    "desviacion": 1.35,
    "tasaFinalizacion": 0.81,
    "tendencia": -0.4
  },
  "distribucion": [ { "desde": 0, "hasta": 1, "cantidad": 0 } ],
  "grupos": { "alto": 6, "medio": 18, "bajo": 8 },
  "estudiantes": [
    {
      "id": 5,
      "nombre": "Ana",
      "apellido": "Pérez",
      "promedioPonderado": 6.1,
      "parcial": false,
      "z": -0.98,
      "grupo": "MEDIO",
      "notas": { "calificadas": 12, "ceros": 1, "sinEntrega": 2, "pendientes": 1 },
      "tasaFinalizacion": 0.75,
      "tendencia": -1.8
    }
  ],
  "noDisponibles": []
}
```
- Los valores del ejemplo son ilustrativos.
- `distribucion` tiene 10 tramos de 1 punto (0–1 … 9–10). `grupo`: `ALTO`, `MEDIO` o `BAJO`.
- `parcial: true` cuando faltó una categoría y se normalizaron los pesos.
- Si la última sincronización fue `PARCIAL` y faltó la función de finalización, `tasaFinalizacion` va en `null` y `"tasaFinalizacion"` aparece en `noDisponibles`. La interfaz muestra «no disponible», nunca 0.
- `tendencia` = promedio de las últimas 3 calificadas − promedio de las anteriores. `null` si hay menos de 4 notas.

## Reglas y alertas (HU-12)

### GET /api/cursos/{id}/reglas · DOCENTE

Si el curso no tiene reglas, se crean las tres con sus umbrales por defecto en la primera consulta.

```json
[
  { "codigo": "R1", "nombre": "Descenso reciente", "umbral": 1.5, "activa": true, "condicion": "Tendencia menor o igual a −umbral con las últimas 3 notas" },
  { "codigo": "R2", "nombre": "Atrasos", "umbral": 2, "activa": true, "condicion": "Actividades vencidas sin entrega mayor o igual a umbral" },
  { "codigo": "R4", "nombre": "Promedio bajo", "umbral": 7.0, "activa": true, "condicion": "Promedio ponderado menor a umbral con al menos 4 notas" }
]
```

### PUT /api/cursos/{id}/reglas · DOCENTE

```json
[ { "codigo": "R4", "umbral": 5.0, "activa": true } ]
```
Respuesta 204 y recálculo de las alertas del curso. 400 si el umbral sale de rango (R1: 0,5 a 5; R2: 1 a 10; R4: 1 a 10) o el código no existe.

### GET /api/cursos/{id}/alertas?estado=ACTIVA · DOCENTE

`estado`: `ACTIVA` (por defecto) o `REVISADA`. Las alertas se recalculan al terminar una sincronización y al cambiar una regla.

```json
[
  {
    "id": 31,
    "regla": "R4",
    "titulo": "Requiere atención",
    "estudiante": { "id": 5, "nombre": "Ana", "apellido": "Pérez" },
    "estado": "ACTIVA",
    "generada": "2026-11-10T07:12:00-05:00",
    "revisada": null,
    "evidencia": { "promedio": 6.1, "notas": 12, "umbral": 7.0 }
  }
]
```
Forma de `evidencia` por regla:
- R1: `{ "ultimas": [5.0, 4.5, 4.0], "anteriores": [7.5, 8.0], "diferencia": -3.25, "umbral": 1.5 }`
- R2: `{ "actividades": [ { "nombre": "Tarea 5", "fechaLimite": "2026-11-03T23:59:00-05:00" } ], "umbral": 2 }`
- R4: `{ "promedio": 6.1, "notas": 12, "umbral": 7.0 }`

Requiere migración: `Alerta` agrega `Generada` y `Revisada` (fechas), y `Evidencia` guarda el JSON.

### PATCH /api/cursos/{id}/alertas/{alertaId} · DOCENTE

```json
{ "estado": "REVISADA" }
```
Respuesta 200 con la alerta actualizada. 404 si la alerta no es de ese curso.

## Herramientas para el agente (HU-13)

Desde el Sprint 2 las herramientas aceptan dos tipos de token:
- JWT del docente (rol `DOCENTE`), como en el Sprint 1.
- Token de servicio (rol `AGENTE`) que emite la API en `POST /api/agente/chat`: claims `role=AGENTE`, `docente=<id del docente>`, `curso=<id del curso>`, vence en 2 minutos. La autorización por curso se aplica sobre el docente del claim.

Todas responden 401 sin token y 403 si el curso no es del docente. El backend registra cada llamada (herramienta, docente, fecha, resultado) sin guardar el token.

### GET /api/tools/cursos-autorizados

Sin cambios.

### GET /api/tools/calificaciones?cursoId=1&estudianteId=5&nombre=elena

`estudianteId` y `nombre` son opcionales. Cambio del 11 nov 2026 (aprobado por Kevin): el curso completo con todas sus notas ocupaba unos 49 KB y no cabía en el contexto del modelo, así que la consulta del asistente se cortaba a los 60 s.

Sin `estudianteId`: un resumen por estudiante, con los mismos números que el panel (`GET /api/cursos/{id}/indicadores`), de menor a mayor `promedioPonderado` (los que no tienen promedio van al final).

```json
{
  "herramienta": "obtener_calificaciones",
  "generado": "2026-11-10T09:30:00-05:00",
  "curso": { "id": 1, "nombreCorto": "AGIL-4B", "ultimaSincronizacion": "2026-11-10T07:10:00-05:00" },
  "estudiantes": [
    {
      "id": 5, "nombre": "Ana", "apellido": "Pérez", "promedioPonderado": 6.1, "parcial": false,
      "notas": { "calificadas": 12, "ceros": 1, "sinEntrega": 2, "pendientes": 1 }
    }
  ]
}
```

Con `nombre` (desde el 12 nov 2026; nombre o apellido, sin distinguir tildes ni mayúsculas; cada palabra tiene que ser el inicio de una palabra del nombre completo): con una sola coincidencia, el detalle de ese estudiante como con `estudianteId`; con varias, la lista corta en el formato del resumen para que el docente elija; sin ninguna, 404 «Ningún estudiante del curso 1 se llama «…»». El agente lo usa cuando el docente escribe un nombre, para no adivinar ids.

Con `estudianteId`: las notas y estados de ese estudiante por actividad (escala 0 a 10). 404 si no está matriculado en el curso.

```json
{
  "herramienta": "obtener_calificaciones",
  "generado": "2026-11-10T09:30:00-05:00",
  "curso": { "id": 1, "nombreCorto": "AGIL-4B", "ultimaSincronizacion": "2026-11-10T07:10:00-05:00" },
  "estudiantes": [
    {
      "id": 5, "nombre": "Ana", "apellido": "Pérez", "promedioPonderado": 6.1,
      "calificaciones": [
        { "actividad": "Tarea 1", "categoria": "Tareas", "unidad": 1, "nota": 6.25, "estado": "CALIFICADO" },
        { "actividad": "Tarea 5", "categoria": "Tareas", "unidad": 3, "nota": null, "estado": "SIN_ENTREGA" }
      ]
    }
  ]
}
```

En los dos casos, un curso sin sincronizar responde con `estudiantes` vacío y `ultimaSincronizacion` en `null`.

### GET /api/tools/alertas?cursoId=1

```json
{
  "herramienta": "identificar_alertas",
  "generado": "2026-11-10T09:30:00-05:00",
  "curso": { "id": 1, "nombreCorto": "AGIL-4B" },
  "alertas": [
    {
      "id": 31, "regla": "R4", "nombreRegla": "Promedio bajo",
      "estudiante": { "id": 5, "nombre": "Ana", "apellido": "Pérez" },
      "evidencia": { "promedio": 6.1, "notas": 12, "umbral": 7.0 }
    }
  ]
}
```
Solo las ACTIVAS. Desde el 12 nov 2026 (TA-13.2) cada alerta es la de `GET /api/cursos/{id}/alertas` sin `titulo`, `estado`, `generada` ni `revisada`, que son iguales en todas, y con `nombreRegla`; la `evidencia` va corta: R2 solo con los nombres de las actividades (`{ "actividades": ["Tarea 5"], "umbral": 2 }`), R1 sin `anteriores` y R4 igual. Las 16 alertas de AGIL-4B en la forma completa llenaban el contexto de 4096 tokens del modelo y la respuesta se acercaba a los 60 s. La pantalla «Alertas» usa la forma completa de `GET /api/cursos/{id}/alertas`. 400 sin `cursoId`, 403 en curso ajeno, 404 si el curso no existe.

## Chat del asistente (HU-14)

### POST /api/agente/chat · DOCENTE

Petición:
```json
{ "cursoId": 1, "mensaje": "¿Qué estudiantes requieren atención?" }
```
La API valida el curso, emite el token de servicio, llama a `POST /chat` del agente y guarda la conversación con sus trazas.

Respuesta 200:
```json
{
  "conversacionId": 44,
  "respuesta": "Hechos: 4 estudiantes tienen alertas activas ...",
  "herramientas": [
    { "nombre": "identificar_alertas", "parametros": { "cursoId": 1 }, "estado": "OK", "duracionMs": 95 }
  ],
  "modelo": "qwen2.5:7b",
  "duracionMs": 5400
}
```
403 si el curso es ajeno (el agente no se llama). 503 con `ProblemDetails` («Asistente no disponible») si el agente no responde en 60 s. 400 si `mensaje` está vacío o pasa de 500 caracteres.

Desde el 12 nov 2026: si el mensaje nombra el código de otro curso de la base (por ejemplo `BDD-4B` desde AGIL-4B), la API responde 200 sin llamar al agente, con `herramientas` vacío y `modelo: "ninguno"`:
- curso del docente: `"Esta conversación es de AGIL-4B. Abre BDD-4B para preguntar sobre ese curso."`;
- curso ajeno: `"No tienes acceso a BDD-4B."`.
La conversación se guarda igual, para que aparezca en el historial.

Requiere migración: `Conversacion` agrega `Pregunta`, `Respuesta`, `Modelo` y `DuracionMs`; `TrazaHerramienta` agrega `Estado`, `DuracionMs` y `Fecha`.

## Historial del asistente (HU-19, 12 nov 2026)

La barra lateral del asistente muestra las conversaciones guardadas en la base. Cada docente ve solo las suyas; un ADMIN también ve solo las suyas en la lista, pero puede abrir cualquiera.

### GET /api/agente/conversaciones?cursoId=1 · DOCENTE

De la más reciente a la más antigua. 400 sin `cursoId`; 403 si el curso es ajeno; 404 si no existe.

```json
[
  { "id": 44, "fecha": "2026-11-11T21:45:00-05:00", "pregunta": "¿Quiénes tienen promedio bajo 7?", "modelo": "qwen2.5:7b", "duracionMs": 15400, "herramientas": 1 }
]
```
`herramientas` es cuántas se usaron. `modelo` es `"ninguno"` cuando la API respondió sola (otro curso en la pregunta).

### GET /api/agente/conversaciones/{id} · DOCENTE

La misma forma de `POST /api/agente/chat` más `cursoId`, `fecha` y `pregunta`. 403 si el curso es ajeno o la conversación es de otro docente; 404 si no existe.

```json
{
  "id": 44, "cursoId": 1, "fecha": "2026-11-11T21:45:00-05:00",
  "pregunta": "¿Quiénes tienen promedio bajo 7?",
  "respuesta": "Los estudiantes con promedio bajo 7 son: …",
  "modelo": "qwen2.5:7b", "duracionMs": 15400,
  "herramientas": [ { "nombre": "obtener_calificaciones", "parametros": { "cursoId": 1 }, "estado": "OK", "duracionMs": 99 } ]
}
```

## Servicio de agente (HU-13, HU-14)

Solo lo llama la API, por la red interna de Docker (`http://agent:8000`). No se publica puerto.

### POST /chat

```json
{ "mensaje": "¿Qué estudiantes requieren atención?", "cursoId": 1, "token": "<token de servicio>" }
```
Respuesta: la misma forma de la respuesta de `POST /api/agente/chat` sin `conversacionId`. Si una herramienta falla, su `estado` es `"ERROR"` y la respuesta dice qué no pudo consultar, sin datos inventados. Si la herramienta responde 403, el agente dice que no tiene acceso a ese curso.

### GET /salud

```json
{ "estado": "OK", "modelo": "qwen2.5:7b", "ollama": "OK" }
```
`ollama`: `OK` o `NO_DISPONIBLE`.
