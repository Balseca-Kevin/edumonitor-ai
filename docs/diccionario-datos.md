# Diccionario de datos · EduMonitorDB

TA-03.5. Base SQL Server `EduMonitorDB`, creada por las migraciones de EF Core en `backend/EduMonitor.Infrastructure/Persistencia/Migraciones/`. Sigue el Diagrama 5 de la planificación. Actualizado el 29 oct 2026 (migración `Modelo_Completo`).

Convenciones: `PK` llave primaria, `FK` llave foránea, `UQ` índice único. Las fechas son `datetimeoffset` (ISO 8601 con zona). Los estados se guardan como texto. Ninguna tabla guarda tokens, contraseñas en claro ni datos de contacto de estudiantes.

## Datos importados de Moodle

### Curso
Curso importado de Moodle.

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| MoodleCursoId | int | No | UQ. Id del curso en Moodle; la sincronización hace upsert por este valor |
| NombreCorto | nvarchar(100) | No | Nombre corto, p. ej. `AGIL-4B` |
| Nombre | nvarchar(250) | No | Nombre completo |
| Periodo | nvarchar(20) | No | Periodo académico, p. ej. `2026-2` |

### Unidad
Sección de Moodle (la sección 0 «General» no se importa).

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| CursoId | int | No | FK → Curso |
| MoodleSeccionId | int | No | UQ. Id de la sección en Moodle |
| Numero | int | No | Número de la sección (1 a 4) |
| Parcial | int | No | 1 para las unidades 1 y 2; 2 para las unidades 3 y 4 |
| Nombre | nvarchar(250) | No | Nombre de la sección, p. ej. `Unidad 1` |

### Estudiante
Datos mínimos del estudiante. Sin correo ni datos de contacto.

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| MoodleUsuarioId | int | No | UQ. Id del usuario en Moodle |
| Nombre | nvarchar(100) | No | Nombre |
| Apellido | nvarchar(100) | No | Apellido |

### Matricula

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| CursoId | int | No | FK → Curso. UQ junto con EstudianteId |
| EstudianteId | int | No | FK → Estudiante |

### Actividad
Ítem calificable del libro de calificaciones de Moodle.

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| CursoId | int | No | FK → Curso |
| UnidadId | int | Sí | FK → Unidad. Null si la actividad no está en una sección o si no se pudieron leer las secciones |
| MoodleItemId | int | No | UQ. Id del ítem de calificación en Moodle |
| Nombre | nvarchar(250) | No | Nombre de la actividad |
| Categoria | nvarchar(100) | Sí | Categoría del libro: `Exámenes`, `Tareas` o `APE` |
| Peso | decimal(9,5) | Sí | Peso del ítem dentro de su categoría, tal como lo da Moodle |
| NotaMaxima | decimal(9,5) | Sí | Nota máxima (10 en el laboratorio) |
| Modulo | nvarchar(50) | Sí | Tipo de módulo de Moodle (`assign`, `quiz`...) |
| Instancia | int | Sí | Id de la instancia del módulo; enlaza con tareas y entregas |
| FechaLimite | datetimeoffset | Sí | Fecha límite (`mod_assign_get_assignments`) |

### Calificacion
Nota de un estudiante en una actividad.

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| ActividadId | int | No | FK → Actividad. UQ junto con EstudianteId |
| EstudianteId | int | No | FK → Estudiante |
| Nota | decimal(9,5) | Sí | Nota de Moodle. Null = sin nota; 0 es una nota real |
| Estado | nvarchar(20) | Sí | `CALIFICADO` (hay nota, incluida la 0), `PENDIENTE` (entregó y falta calificar), `SIN_ENTREGA` (no entregó). Null = no disponible |
| Finalizada | bit | Sí | Finalización de la actividad en Moodle. Null = sin seguimiento o dato no disponible. **No está en el Diagrama 5**: se agregó en TA-05.4 |

## Datos propios del middleware (desde el Sprint 2)

### Tema

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| UnidadId | int | No | FK → Unidad |
| Nombre | nvarchar(250) | No | Nombre del tema |

### ContenidoMinimo

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| TemaId | int | No | FK → Tema (borrado en cascada) |
| Descripcion | nvarchar(1000) | No | Contenido mínimo del tema |

### ActividadTema

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| ActividadId | int | No | PK, FK → Actividad |
| TemaId | int | No | PK, FK → Tema |
| PesoRelativo | decimal(9,5) | No | Peso del tema dentro de la actividad |

### ReglaAlerta

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| CursoId | int | No | FK → Curso. UQ junto con Codigo |
| Codigo | nvarchar(10) | No | `R1` a `R6` o `G1` (planificación, tabla 6) |
| Umbral | decimal(9,5) | No | Umbral configurable de la regla |
| Activa | bit | No | Si la regla se evalúa |

### Alerta
Se muestra como «Requiere atención», nunca como «reprobará» ni «en riesgo».

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| ReglaId | int | No | FK → ReglaAlerta |
| EstudianteId | int | No | FK → Estudiante |
| Evidencia | nvarchar(max) | No | Datos que justifican la alerta |
| Estado | nvarchar(20) | No | Estado de revisión, p. ej. `ACTIVA` o `REVISADA` |

### TareaRefuerzo

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| TemaId | int | No | FK → Tema |
| EstudianteId | int | Sí | FK → Estudiante. Null en una tarea grupal |
| Contenido | nvarchar(max) | No | Texto de la tarea de refuerzo |
| Estado | nvarchar(20) | No | `BORRADOR`, `APROBADA` o `DESCARTADA` |

## Trazabilidad y auditoría

### Sincronizacion

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| CursoId | int | No | FK → Curso |
| UsuarioId | int | No | Usuario que pulsó Sincronizar |
| Inicio, Fin | datetimeoffset | No | Inicio y fin de la ejecución |
| Estado | nvarchar(20) | No | `COMPLETADA`, `PARCIAL` o `FALLIDA` |
| Origen | nvarchar(250) | No | URL del Moodle de donde vienen los datos |
| RegistrosUnidades, RegistrosActividades, RegistrosEstudiantes, RegistrosCalificaciones | int | No | Cantidad de registros importados |
| Errores | nvarchar(max) | No | JSON con la función de Moodle que falló y su mensaje |

### Conversacion

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| UsuarioId | int | No | FK → Usuario (docente) |
| CursoId | int | No | FK → Curso |
| Fecha | datetimeoffset | No | Inicio de la conversación |

### TrazaHerramienta

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| ConversacionId | int | No | FK → Conversacion (borrado en cascada) |
| Herramienta | nvarchar(100) | No | Nombre de la herramienta, p. ej. `listar_cursos_autorizados` |
| Parametros | nvarchar(max) | No | Parámetros en JSON. Nunca incluye tokens |
| Resultado | nvarchar(max) | No | `OK` o `ERROR` y resumen |

## Identidad y acceso

### Usuario

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| NombreUsuario | nvarchar(50) | No | UQ. Nombre de inicio de sesión, p. ej. `doc01` |
| Nombre | nvarchar(150) | No | Nombre visible |
| Correo | nvarchar(150) | Sí | Correo del docente o administrador (no de estudiantes) |
| HashContrasena | nvarchar(200) | No | Hash de la contraseña (ASP.NET Core Identity); nunca en claro |
| Rol | nvarchar(20) | No | `DOCENTE` o `ADMIN` |

### DocenteCurso

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| UsuarioId | int | No | PK, FK → Usuario |
| CursoId | int | No | PK, FK → Curso. Cursos que tiene autorizados cada docente |

### Auditoria

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| Id | int | No | PK |
| UsuarioId | int | No | FK → Usuario |
| Accion | nvarchar(50) | No | Acción, p. ej. `SINCRONIZAR` |
| Entidad | nvarchar(250) | No | Entidad afectada, p. ej. `Curso 7` |
| Fecha | datetimeoffset | No | Momento de la acción (con índice) |
