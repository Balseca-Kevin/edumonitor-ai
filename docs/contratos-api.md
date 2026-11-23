# Contratos de API · Sprints 1, 2 y 3 (versión 1.0 del 21 oct 2026; bloques de los Sprints 2 y 3 al final)

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

---

# Contratos de API · Sprint 3 (versión 3.0, propuesta del 16 nov 2026)

Lo de arriba sigue vigente. Este bloque tiene dos partes:

- Contratos detallados, que se pueden programar desde hoy: el libro de calificaciones (HU-21) y los filtros de la herramienta de calificaciones (HU-23).
- Rutas propuestas, que su responsable detalla aquí (petición, respuesta y códigos) antes de programarlas. Kevin aprueba cada cambio, como en el Sprint 2.

Reglas que valen para todo el bloque:

- Todos los endpoints `/api/cursos/{id}/*` usan la política `CursoAutorizado`: 401 sin token, 403 en curso ajeno, 404 si el curso no existe, 409 si el curso nunca se sincronizó.
- Escala 0 a 10, aprobación 7,0. Solo las notas `CALIFICADO` entran a promedios. Un dato que no se pudo obtener va en `noDisponibles`, nunca como 0.
- Fechas, semanas y estados del período se calculan en el backend con la fecha del servidor (zona `America/Guayaquil`) y las fechas de Moodle (`startdate`, `enddate`, fecha límite de cada tarea). El frontend no calcula fechas.
- Las respuestas de `/api/tools/*` que van al modelo externo llevan códigos (E-001, E-002…) en lugar de nombres, sin correos ni ids de Moodle (HU-23). La pantalla siempre recibe nombres.

## Libro de calificaciones (HU-21)

### GET /api/cursos/{id}/libro · DOCENTE

Parámetros opcionales: `busqueda` (nombre o apellido), `orden` (`apellido`, por defecto, o `promedio`).

Se arma con los datos guardados de la última sincronización; no llama a Moodle. Las notas son las mismas de Moodle, en escala 0 a 10.

```json
{
  "cursoId": 1,
  "escala": 10,
  "aprobacion": 7.0,
  "sincronizacion": { "id": 12, "fecha": "2026-11-16T07:10:00-05:00", "estado": "COMPLETADA", "origen": "http://localhost:8080" },
  "parciales": [
    {
      "numero": 1,
      "unidades": [
        {
          "numero": 1,
          "nombre": "Unidad 1",
          "actividades": [
            { "id": 101, "nombre": "Tarea 1", "categoria": "Tareas", "fechaLimite": "2026-10-23T23:59:00-05:00" },
            { "id": 102, "nombre": "APE 1", "categoria": "APE", "fechaLimite": "2026-10-25T23:59:00-05:00" }
          ]
        }
      ]
    }
  ],
  "estudiantes": [
    {
      "id": 5,
      "nombre": "Ana",
      "apellido": "Pérez",
      "promedioPonderado": 6.1,
      "parcial": false,
      "notas": [
        { "actividadId": 101, "nota": 6.25, "estado": "CALIFICADO" },
        { "actividadId": 102, "nota": null, "estado": "SIN_ENTREGA" }
      ]
    }
  ],
  "noDisponibles": []
}
```
- Los valores del ejemplo son ilustrativos.
- `categoria`: el nombre de la categoría del libro de Moodle (exámenes, tareas o APE), con el mismo valor que devuelve `GET /api/tools/calificaciones`.
- Cada estudiante tiene una entrada en `notas` por cada actividad de `parciales`, en el mismo orden. `estado`: `CALIFICADO` (con `nota`, incluido 0), `SIN_ENTREGA` o `PENDIENTE` (con `nota` en `null`).
- `promedioPonderado` es el mismo valor de `GET /api/cursos/{id}/indicadores`; `parcial: true` cuando faltó una categoría.

Detalle del 16 nov 2026 (TA-21.1, Kevin):
- `orden=promedio` va de menor a mayor promedio, con los que no tienen promedio al final y empate por apellido, como `GET /api/tools/calificaciones`. Otro valor de `orden` responde 400.
- `busqueda` usa la misma regla que el parámetro `nombre` de `GET /api/tools/calificaciones` (cada palabra es el inicio de una palabra del nombre completo, sin distinguir tildes ni mayúsculas). Sin coincidencias, `estudiantes` va vacío con 200; las columnas no cambian.
- Solo aparecen las unidades con actividades y los parciales con unidades. Las actividades van en el orden del curso: unidad, fecha límite y orden del ítem en Moodle.
- Una actividad que en Moodle no está en ninguna sección va al final, en el parcial `0` y la unidad `0` «Sin unidad».
- `estado` es `null` cuando el estudiante no tiene registro para esa actividad o Moodle no dio el dato; la pantalla lo muestra como «no disponible».
- `noDisponibles` lleva `"estadosEntrega"` cuando la última sincronización no pudo leer `mod_assign_get_assignments` o `mod_assign_get_submissions`. Así la pantalla avisa que «sin entrega» y «pendiente» pueden no estar al día.

## Aún no vence y desglose por categoría (R.4)

Cambios en dos rutas:
- `GET /api/cursos/{id}/indicadores`:
  - `notas` de cada estudiante suma `aunNoVence`: actividades sin entrega con fecha límite futura según la fecha del servidor. Ya no cuentan en `sinEntrega`, que queda solo con las vencidas.
  - Cada estudiante suma `categorias`: el promedio de cada categoría ponderada, con su peso y cuántas notas tiene. Una categoría sin notas va con `promedio: null`, nunca 0.
- `GET /api/cursos/{id}/libro`: una celda sin entrega cuya fecha límite todavía no llega tiene `estado: "AUN_NO_VENCE"`. Los demás estados no cambian.

```json
{
  "id": 5, "nombre": "Ana", "apellido": "Pérez", "promedioPonderado": 6.1,
  "notas": { "calificadas": 12, "ceros": 1, "sinEntrega": 2, "pendientes": 1, "aunNoVence": 3 },
  "categorias": [
    { "categoria": "Exámenes", "peso": 0.40, "promedio": 5.4, "notas": 1 },
    { "categoria": "Tareas", "peso": 0.30, "promedio": 6.8, "notas": 9 },
    { "categoria": "APE", "peso": 0.30, "promedio": null, "notas": 0 }
  ]
}
```

`GET /api/tools/calificaciones` con `estudianteId` también usa `AUN_NO_VENCE`, para que el asistente no diga que falta una entrega que todavía no vence.

## Estado del período (TA-22.A4)

### GET /api/cursos/{id}/periodo · DOCENTE

Semana del período, unidades, actividades con su fecha límite y su estado, y los tres bloques de la cabecera del curso (vencidas, vence esta semana y siguiente). Se calcula en el backend con la fecha del servidor en `America/Guayaquil` y lo que guardó la última sincronización; no llama a Moodle.

```json
{
  "cursoId": 1,
  "hoy": "2026-11-22T10:00:00-05:00",
  "inicio": "2026-09-01T02:00:00-05:00",
  "fin": "2026-12-11T23:59:00-05:00",
  "finOrigen": "ULTIMA_FECHA_LIMITE",
  "estado": "EN_CURSO",
  "semana": { "actual": 12, "total": 15 },
  "unidadActual": { "numero": 4, "nombre": "Herramientas ágiles del mercado", "parcial": 2 },
  "unidades": [
    { "numero": 1, "nombre": "Principios fundamentales de las metodologías ágiles", "parcial": 1, "semanaDesde": 1, "semanaHasta": 4 }
  ],
  "actividades": [
    {
      "id": 101, "nombre": "Tarea 1", "sigla": "T1", "categoria": "Tareas", "unidad": 1,
      "fechaLimite": "2026-09-08T17:59:00-05:00", "semana": 2, "estado": "VENCIDA",
      "matriculados": 32, "entregaron": 31, "faltan": 1
    }
  ],
  "vencidas": { "actividades": 12, "entregasQueFaltan": 18, "conFaltas": [ { "id": 113, "nombre": "Tarea 5", "faltan": 4 } ] },
  "venceEstaSemana": [ { "id": 117, "nombre": "Examen Parcial 2", "fechaLimite": "2026-11-25T18:59:00-05:00", "entregaron": 0, "matriculados": 32 } ],
  "siguiente": { "id": 118, "nombre": "Tarea 9", "categoria": "Tareas", "semana": 13, "fechaLimite": "2026-11-28T23:59:00-05:00", "temas": [] },
  "noDisponibles": []
}
```

Reglas del cálculo:
- **Inicio y fin:** `inicio` es el `startdate` del curso en Moodle (`core_enrol_get_users_courses`), que la sincronización guarda. `fin` es el `enddate`; si Moodle lo tiene en 0, es la fecha límite más tardía del curso y `finOrigen` dice `ULTIMA_FECHA_LIMITE` (si no, `MOODLE`).
- **Semana:** la semana n va del día `inicio + 7·(n−1)` al día anterior a `inicio + 7·n`, en fechas de Ecuador. `semana.total` es la semana de `fin`. `semana.actual` es la semana de `hoy`, acotada entre 1 y `total`. `estado` es `ANTES` (antes de `inicio`), `EN_CURSO` o `TERMINADO` (después de `fin`).
- **Unidad actual:** la unidad de la siguiente actividad que no ha vencido; si todas vencieron, la última unidad con actividades.
- **Unidades:** `semanaHasta` es la semana de la última fecha límite de la unidad, y `semanaDesde` es la semana siguiente a la de la unidad anterior (1 en la primera). Una unidad sin actividades va con las dos en `null`.
- **Estado de cada actividad:**
  - `VENCIDA`: la fecha límite ya pasó.
  - `VENCE_ESTA_SEMANA`: no ha pasado y cae en la semana actual.
  - `PROXIMA`: vence en una semana posterior.
  - `SIN_FECHA`: no tiene fecha límite.
- **Entregas:** `entregaron` cuenta las notas calificadas y las entregas por calificar; `faltan` cuenta las que no se entregaron, solo en las vencidas (0 en las demás).
- **Siguiente:** la primera actividad `PROXIMA`. `temas` son los temas que el mapeo de HU-08 asocia a esa actividad (vacío si todavía no se asociaron).
- **Sigla:** `Tarea n` → `Tn`, `Taller n` → `TLn`, `APE n` → `APEn`, `Examen Parcial n` → `EX Pn`. Las demás llevan las iniciales en mayúsculas («Proyecto integrador» → `PI`).

`noDisponibles` lleva `"inicio"` si la sincronización no pudo leer la fecha de inicio. En ese caso `semana`, `estado`, `unidades[].semanaDesde/semanaHasta` y `actividades[].semana` van en `null`; los estados de las actividades y los bloques siguen, porque dependen solo de las fechas límite. Sin semanas, «vence esta semana» son las que vencen entre hoy y los 6 días siguientes.

Códigos: 401, 403, 404 y 409 como en el resto del bloque.

### GET /api/tools/estado-periodo?cursoId=1 · DOCENTE, AGENTE

Herramienta `consultar_estado_periodo` del asistente: lo mismo de la ruta anterior en forma corta, para que el modelo sepa en qué punto del período está el curso.

```json
{
  "herramienta": "consultar_estado_periodo",
  "generado": "2026-11-22T10:00:00-05:00",
  "curso": { "id": 1, "nombreCorto": "AGIL-4B" },
  "hoy": "2026-11-22",
  "semana": 12, "totalSemanas": 15,
  "unidadActual": "U4 · Herramientas ágiles del mercado",
  "vencidas": [ { "nombre": "Tarea 5", "faltan": 4 } ],
  "venceEstaSemana": [ { "nombre": "Examen Parcial 2", "fechaLimite": "2026-11-25", "entregaron": 0, "matriculados": 32 } ],
  "siguiente": { "nombre": "Tarea 9", "semana": 13, "fechaLimite": "2026-11-28", "temas": [] }
}
```

`vencidas` lleva solo las que tienen entregas que faltan. Un curso sin sincronizar responde 200 con `semana` en `null` y las listas vacías. Responde 400 sin `cursoId`, 403 en un curso ajeno y 404 si el curso no existe.

## Tramo del período (R.2)

El control segmentado de la barra del curso (Todo el ciclo, Parcial 1, Parcial 2, U1 … Un) filtra el libro, el panel y las alertas. El backend calcula con las actividades del tramo, con el mismo código del curso completo; el frontend solo elige el tramo.

Parámetro opcional `tramo` en tres rutas:
- `GET /api/cursos/{id}/indicadores?tramo=`
- `GET /api/cursos/{id}/libro?tramo=`
- `GET /api/cursos/{id}/alertas?tramo=`

| Valor | Actividades que entran |
|---|---|
| `todo` (por defecto, o sin el parámetro) | Todas, incluidas las que no están en una unidad |
| `p1`, `p2` | Las de las unidades de ese parcial |
| `u1` … `un` | Las de esa unidad |

Respuestas:
- **Indicadores:** la forma no cambia y suma `"tramo": "u1"`. `curso`, `distribucion`, `grupos` y `estudiantes` se calculan con las actividades del tramo. `unidades` sigue con todas las unidades del curso, porque el control segmentado las necesita.
- **Libro:** suma `"tramo"`. `parciales` trae solo las columnas del tramo, y `notas` y `promedioPonderado` salen de esas columnas (el mismo promedio de los indicadores del tramo).
- **Alertas:** la misma lista sin campos nuevos. Con un tramo que no es `todo`, quedan las alertas del curso cuya regla también se cumple con las actividades de ese tramo, con los mismos umbrales. Cada una conserva su `id` y su `estado` (se puede marcar revisada) y trae la `evidencia` del tramo.

Códigos:
- 400 si `tramo` no es un código válido (`semana`, `x1`, `u0`) o si el curso no tiene ese parcial o esa unidad (`u9`).
- 401, 403, 404 y 409 como en el resto del bloque.

## Cambios entre sincronizaciones (TA-21.A1)

Cada sincronización compara las calificaciones y entregas del curso antes y después de importar, y guarda las celdas que cambiaron. La compara con lo que había en la base, que es lo que dejó la sincronización anterior.

### GET /api/cursos/{id}/sync/{sid}/cambios · DOCENTE

```json
{
  "sincronizacionId": 13,
  "cursoId": 1,
  "fecha": "2026-11-22T09:20:07-05:00",
  "anteriorId": 12,
  "conteos": { "total": 4, "notasNuevas": 2, "notasCambiadas": 1, "entregasNuevas": 1, "otros": 0 },
  "celdas": [
    {
      "actividadId": 101, "actividad": "Tarea 1",
      "estudiante": { "id": 5, "nombre": "Ana", "apellido": "Pérez" },
      "tipo": "NOTA_CAMBIADA",
      "antes": { "nota": 6.25, "estado": "CALIFICADO" },
      "ahora": { "nota": 7.0, "estado": "CALIFICADO" }
    }
  ],
  "noDisponibles": []
}
```

`tipo` de cada celda:

| Tipo | Antes | Ahora |
|---|---|---|
| `NOTA_NUEVA` | Sin nota (sin entrega, por calificar, sin dato o sin registro) | `CALIFICADO` |
| `NOTA_CAMBIADA` | `CALIFICADO` | `CALIFICADO` con otra nota |
| `ENTREGA_NUEVA` | Sin entrega, sin dato o sin registro | `PENDIENTE` (entregada, por calificar) |
| `OTRO` | Cualquier otro cambio de estado (por ejemplo, una nota que Moodle ya no da) | |

Reglas:
- Pasar de «sin dato» a «sin entrega», o crearse una celda sin entrega (actividad o estudiante nuevos), no es un cambio: el docente no tiene nada nuevo que mirar.
- `anteriorId` es la sincronización anterior del curso. En la primera, `anteriorId` va en `null`, `celdas` vacío y los conteos en 0: todo es nuevo y no se marca.
- Las celdas van en el orden del libro: unidad, fecha límite y orden del ítem; dentro de la actividad, por apellido.
- `noDisponibles` lleva `"cambios"` cuando la sincronización se hizo antes de que existiera este registro. En ese caso, conteos en 0 y `celdas` vacío: no se sabe, no es que no haya cambios.
- Una sincronización `FALLIDA` no cambia nada y responde con conteos en 0.

Códigos: 401 sin token, 403 en curso ajeno, 404 si el curso no existe o la sincronización `sid` no es de ese curso.

## Rúbricas y comentarios en la sincronización (HU-08, bloque 2)

`POST /api/cursos/{id}/sync` suma un paso después de las notas. La forma de la respuesta no cambia.

1. `mod_assign_get_assignments` da el cmid de cada tarea.
2. `core_grading_get_definitions` (`cmids`, `areaname=submissions`, `activeonly=0`) da las rúbricas. Solo cuentan las áreas con `activemethod: "rubric"`; con `activeonly=1` Moodle responde `invalid_method_name` en las tareas sin método avanzado.
3. `core_grading_get_gradingform_instances` (`definitionid`, `since=0`) da los niveles elegidos en cada nota. Vale la instancia activa (`status: 1`) más reciente de cada `itemid`.
4. `mod_assign_get_submission_status` (`assignid`, `userid`), solo para los estudiantes con nota en una tarea con rúbrica, enlaza `feedback.grade.id` (el `itemid`) con el estudiante y trae el comentario del docente.

Se guarda:

- `CriterioRubrica`: actividad, id del criterio en Moodle, orden, descripción en texto y puntaje máximo. El `TemaId` lo asocia el docente (HU-08) y la sincronización no lo cambia.
- `CalificacionCriterio`: criterio, estudiante, puntaje y nombre del nivel.
- `Calificacion.Comentario`: el comentario del docente en texto; cada elemento de lista empieza con «• » en su propia línea.

Si una de estas funciones falla, la sincronización queda `PARCIAL` con su error. Las notas y entregas se guardan igual, y los criterios quedan como estaban.

## Mapeo temático (HU-08)

El docente asocia a cada tema de la unidad las actividades que lo evalúan y, en las tareas con rúbrica, cada criterio. Los temas iniciales de cada unidad vienen del sílabo cargado en Moodle (TA-08.A1), y el docente puede agregar los suyos. Nada de esto se escribe en Moodle.

### GET /api/cursos/{id}/mapeo · DOCENTE

```json
{
  "cursoId": 1,
  "unidades": [
    { "id": 7, "numero": 1, "nombre": "Principios fundamentales de las metodologías ágiles", "parcial": 1,
      "temas": [ { "id": 21, "nombre": "1.3 Manifiesto Ágil" } ] }
  ],
  "actividades": [
    {
      "id": 101, "nombre": "Tarea 1", "categoria": "Tareas", "unidad": 1, "fechaLimite": "2026-09-08T23:59:00-05:00",
      "temas": [21],
      "criterios": [ { "id": 5, "orden": 1, "descripcion": "Redacción de historias de usuario", "puntajeMaximo": 2.5, "temaId": 21 } ]
    }
  ]
}
```

- Las actividades van en el orden del libro. `temas` lleva los temas de la actividad, incluidos los que llegan por sus criterios.
- `criterios` va vacío si la tarea no tiene rúbrica.
- Códigos: 401, 403, 404 y 409, como en el resto del bloque.

### GET /api/cursos/{id}/unidades/{uid}/temas · DOCENTE

Devuelve los temas de la unidad: `[ { "id": 21, "nombre": "1.3 Manifiesto Ágil" } ]`. Responde 404 si la unidad no es del curso.

### POST /api/cursos/{id}/unidades/{uid}/temas · DOCENTE

Petición: `{ "nombre": "Historias de usuario" }`. Responde 201 con `{ "id": 30, "nombre": "Historias de usuario" }`.

- 400 si el nombre está vacío, pasa de 200 caracteres o ya existe en la unidad (sin distinguir mayúsculas ni espacios).
- 404 si la unidad no es del curso.

### PUT /api/cursos/{id}/actividades/{aid}/temas · DOCENTE

Petición:
```json
{ "temas": [21], "criterios": [ { "id": 5, "temaId": 21 }, { "id": 6, "temaId": null } ] }
```

- Reemplaza los temas de la actividad. Los temas de los criterios se suman solos a los de la actividad.
- Responde 200 con la actividad en la forma de `GET /api/cursos/{id}/mapeo`.
- 400 si un tema no es de una unidad del curso o un criterio no es de esa actividad.
- 404 si la actividad no es del curso.

## Desempeño por tema (HU-08, HU-22)

### GET /api/cursos/{id}/tematicas?actividadId= · DOCENTE

Cuánto domina el curso cada tema, quiénes quedaron bajo 6,0 y dónde se vuelve a evaluar. Sin `actividadId` va el curso completo; con `actividadId`, solo los temas de esa actividad y solo con sus notas. Se calcula con lo que guardó la última sincronización y la fecha del servidor; no llama a Moodle.

```json
{
  "cursoId": 1,
  "actividadId": 103,
  "temas": [
    {
      "id": 30, "nombre": "Historias de usuario", "unidad": { "numero": 1, "nombre": "Principios fundamentales de las metodologías ágiles" },
      "evaluaciones": [
        { "actividadId": 103, "actividad": "Tarea 3", "fuente": "CRITERIOS", "criterios": ["Criterios de aceptación de las historias"] }
      ],
      "estudiantesConNota": 31,
      "promedio": 6.42,
      "domina": 0.55,
      "bajo6": [ { "id": 8, "nombre": "Diego", "apellido": "Díaz", "nota": 4.0 } ],
      "estudiantes": [ { "id": 8, "nombre": "Diego", "apellido": "Díaz", "nota": 4.0, "notas": [ { "actividadId": 103, "nota": 4.0 } ] } ],
      "proximas": [ { "actividadId": 118, "nombre": "Tarea 9", "fechaLimite": "2026-11-28T23:59:00-05:00" } ]
    }
  ],
  "sinTemas": [ { "actividadId": 104, "nombre": "Tarea 4" } ]
}
```

Reglas del cálculo, todas en Application y con pruebas:

- **Nota de un estudiante en un tema y una actividad:**
  - Si la actividad tiene criterios de rúbrica asociados al tema y el estudiante tiene sus niveles, es la suma de sus puntajes en esos criterios entre la suma de los puntajes máximos, por 10 (`fuente: CRITERIOS`).
  - Si no, cuando la actividad está asociada al tema y la nota del estudiante es `CALIFICADO`, es su nota (`fuente: NOTA`).
  - Sin entrega, por calificar o sin dato no dan nota, nunca 0.
- **Nota del estudiante en el tema:** el promedio simple de sus notas en las actividades del tema.
- **`promedio`:** el promedio de esas notas. **`domina`:** la parte de los estudiantes con nota que tiene 7,0 o más, de 0 a 1. Si nadie tiene nota, los dos van en `null`.
- **`bajo6`:** los estudiantes con nota menor a 6,0, de menor a mayor.
- **`proximas`:** las actividades asociadas al tema cuya fecha límite todavía no llega.
- **`sinTemas`:** las actividades con nota que no tienen ningún tema asociado (en el curso, o la actividad pedida).

Códigos: 401, 403, 404 (curso, o `actividadId` que no es del curso) y 409, como en el resto del bloque.

## Filtros de la herramienta de calificaciones (HU-23)

### GET /api/tools/calificaciones?cursoId=1&promedioMenorA=7&orden=sinEntrega

Parámetros nuevos, opcionales, solo para el resumen sin `estudianteId`:

- `promedioMenorA`: número de 0 a 10. Devuelve solo los estudiantes con `promedioPonderado` menor a ese valor; los que no tienen promedio quedan fuera. 400 si sale de 0 a 10.
- `orden`: `promedio` (por defecto, de menor a mayor) o `sinEntrega` (de mayor a menor cantidad de actividades sin entrega; empate por apellido).

La forma de la respuesta no cambia. Corrige las preguntas 5, 6 y 9 de la evaluación de TA-13.7, donde el modelo filtraba u ordenaba la lista de 32 estudiantes.

## Rutas propuestas (se detallan antes de programarlas)

Los nombres de la planificación (tabla 4) se conservan. Las rutas marcadas «nueva» no están en la planificación.

| Método | Ruta | Rol | HU | Origen |
|---|---|---|---|---|
| GET | `/api/cursos/{id}/estudiantes/{eid}` | DOCENTE | HU-10 | Planificación |
| GET | `/api/cursos/{id}/estudiantes/{eid}/nota-necesaria` | DOCENTE | HU-10 (TA-10.A1) | nueva |
| GET/POST | `/api/cursos/{id}/unidades/{uid}/temas` | DOCENTE | HU-08 | Planificación (detallada en «Mapeo temático», con `GET /api/cursos/{id}/mapeo`) |
| PUT | `/api/cursos/{id}/actividades/{aid}/temas` | DOCENTE | HU-08 | Planificación (suma criterios de rúbrica) |
| GET | `/api/cursos/{id}/tematicas` | DOCENTE | HU-22 (antes HU-11) | Planificación (detallada en «Desempeño por tema») |
| GET | `/api/cursos/{id}/analisis-grupal` | DOCENTE | HU-22 (antes HU-11) | Planificación |
| GET | `/api/cursos/{id}/actividades/{aid}` | DOCENTE | HU-22 | nueva |
| GET/PATCH | `/api/cursos/{id}/refuerzos` | DOCENTE | HU-15 | Planificación |
| GET | `/api/cursos/{id}/refuerzos/{rid}/documento?formato=pdf\|docx` | DOCENTE | HU-15 | nueva |
| GET | `/api/cursos/{id}/refuerzos/aplicados` | DOCENTE | HU-15 (TA-15.A2) | nueva |
| GET | `/api/cursos/{id}/informe?formato=pdf` | DOCENTE | HU-16 | Planificación |
| GET | `/api/auditoria` | ADMIN | HU-17 | Planificación |
| GET/PUT | `/api/admin/modelo` | ADMIN | HU-23 | nueva |
| POST/GET | `/api/admin/evaluacion-agente` | ADMIN | HU-18 (TA-18.A1) | nueva |
| GET | `/api/cursos/{id}/periodo` | DOCENTE | HU-22 (TA-22.A4) | nueva |
| GET | `/api/cursos/{id}/sync/{sid}/cambios` | DOCENTE | HU-21 (TA-21.A1) | nueva |
| POST | `/api/cursos/{id}/refuerzos` | DOCENTE | HU-15 (TA-15.A3): guarda el plan (actividad, temas marcados, estudiantes) | nueva |
| GET | `/api/tools/estado-periodo` | DOCENTE, AGENTE | HU-22 (TA-22.A4) | nueva |
| GET | `/api/tools/actividad`, `/api/tools/desempeno-tematico`, `/api/tools/retroalimentacion`, `/api/tools/refuerzos-aplicados`, `/api/tools/nota-necesaria` | DOCENTE, AGENTE | HU-15, HU-22, HU-23 | nueva |

Cambios propuestos en rutas existentes (se detallan en la primera tarea de HU-23):

- `POST /api/agente/chat` suma `conversacionId` (para seguir una conversación) y `foco` (`actividadId` y `estudianteId` opcionales). La respuesta suma `respaldo` (true si respondió el modelo local por falla del externo) y `cifras` (valor, herramienta, fila y si quedó verificada).
- El `foco` también acepta `refuerzoId` (TA-15.A4): el asistente lee el plan guardado y responde con el borrador en esa conversación.
- `POST /chat` del agente recibe el foco, el estado del período (TA-22.A4) y los últimos 4 turnos.
- `GET /salud` del agente y `GET /api/admin/estado` muestran el modelo externo y el local por separado.
