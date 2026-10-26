# Evidencia HU-05 · Pruebas automatizadas de la sincronización

- **HU:** HU-05 · Sincronización manual de un curso
- **Tareas cubiertas:** TA-05.2 (matriculados), TA-05.3 (ítems y notas), TA-05.5 (registro idempotente). PR [#4](https://github.com/Balseca-Kevin/edumonitor-ai/pull/4), fusionado en `develop`.
- **Fecha:** 22 oct 2026, 13:46 (-05:00), sobre el código de `develop` (`5389485`)
- **Comando:** `dotnet test backend/EduMonitor.sln`

> La sincronización contra el Moodle real **depende de TA-02.2** (cliente Moodle con HttpClient, Sebastián). Hasta entonces la API usa `MoodleClientPendiente`, `POST /api/cursos/{id}/sync` responde 502 y el intento queda registrado como `FALLIDA`. Por eso estas pruebas usan `MoodleClientFalso`, un Moodle en memoria con 2 estudiantes, 1 docente, las categorías Exámenes y Tareas, una nota 0 y una tarea sin nota. La tabla de 10 notas comparadas con Moodle es TA-05.6 (Sebastián).

## Resumen

```
Correctas! - Con error: 0, Superado: 64, Omitido: 0, Total: 64 - EduMonitor.Tests.dll (net8.0)
```

## Conteo por clase de prueba

| Clase | Correctas | Con error | Qué cubre |
|---|---|---|---|
| `Sincronizaciones.ImportadorMatriculadosTests` | 5 | 0 | TA-05.2: solo rol student, sin campos de contacto, idempotencia, estudiante en dos cursos, fallo de Moodle |
| `Sincronizaciones.ImportadorCalificacionesTests` | 10 | 0 | TA-05.3: categorías y pesos, notas iguales a Moodle, nota 0 ≠ sin nota, idempotencia, no matriculados, derivación de estado |
| `Sincronizaciones.SincronizacionServiceTests` | 6 | 0 | TA-05.5: COMPLETADA, idempotencia (dos sincronizaciones no duplican), PARCIAL con el error, FALLIDA, PARCIAL conserva datos previos |
| `Api.SincronizacionApiTests` | 5 | 0 | `POST` y `GET /api/cursos/{id}/sync`: formato del contrato, 403 curso ajeno, 401 sin token, historial, 502 FALLIDA, 404 |
| `Api.PermisosTests` | 14 | 0 | HU-04: login, 401, 403, 200, 404, tools |
| `Api.HealthTests` | 3 | 0 | Health, ProblemDetails, CORS |
| `Api.TokenMoodleEnLogsTests` | 1 | 0 | TA-02.4: token enmascarado en el log |
| `Auth.AuthServiceTests` | 4 | 0 | TA-04.1 |
| `Cursos.CursoServiceTests` | 4 | 0 | TA-04.3 |
| `Persistencia.ModeloIdentidadTests` | 2 | 0 | Entidades e índices únicos |
| `Seguridad.EnmascaradorSecretosTests` | 7 | 0 | TA-02.4 |
| `Seguridad.JwtServiceTests` | 3 | 0 | TA-04.2 |
| **Total** | **64** | **0** | |

Las clases de HU-05 suman 26 pruebas.

## Historial de GET /api/cursos/{id}/sync

Generado el 22 oct 2026, 14:01 (-05:00), contra la API real con el token de doc01 sobre AGIL-4B (id 1). Como TA-02.2 aún no está, la sincronización responde **502** y queda registrada como **FALLIDA**, sin inventar datos ni mostrar ceros como notas:

```
POST /api/cursos/1/sync  (Authorization: Bearer ••••)
```

Respuesta **HTTP 502**:

```json
{
  "type": "https://httpstatuses.io/502",
  "title": "Moodle no disponible",
  "status": 502,
  "detail": "Moodle no respondió; no se importó ningún dato. El intento quedó registrado en el historial.",
  "sincronizacion": {
    "id": 1,
    "cursoId": 1,
    "estado": "FALLIDA",
    "inicio": "2026-10-22T14:01:53.149562-05:00",
    "fin": "2026-10-22T14:01:53.1632716-05:00",
    "origen": "http://localhost:8080",
    "registros": {
      "unidades": 0,
      "actividades": 0,
      "estudiantes": 0,
      "calificaciones": 0
    },
    "errores": [
      {
        "funcion": "core_enrol_get_enrolled_users",
        "mensaje": "El cliente de Moodle aún no está implementado (TA-02.2)."
      },
      {
        "funcion": "gradereport_user_get_grade_items",
        "mensaje": "El cliente de Moodle aún no está implementado (TA-02.2)."
      }
    ]
  }
}
```

```
GET /api/cursos/1/sync  (Authorization: Bearer ••••)
```

Respuesta **HTTP 200**:

```json
[
  {
    "id": 1,
    "cursoId": 1,
    "estado": "FALLIDA",
    "inicio": "2026-10-22T14:01:53.149562-05:00",
    "fin": "2026-10-22T14:01:53.1632716-05:00",
    "origen": "http://localhost:8080",
    "registros": {
      "unidades": 0,
      "actividades": 0,
      "estudiantes": 0,
      "calificaciones": 0
    },
    "errores": [
      {
        "funcion": "core_enrol_get_enrolled_users",
        "mensaje": "El cliente de Moodle aún no está implementado (TA-02.2)."
      },
      {
        "funcion": "gradereport_user_get_grade_items",
        "mensaje": "El cliente de Moodle aún no está implementado (TA-02.2)."
      }
    ]
  }
]
```
