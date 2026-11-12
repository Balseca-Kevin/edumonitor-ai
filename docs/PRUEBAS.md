# Pruebas en el navegador · EduMonitor AI

Guía para comprobar en la aplicación cada historia entregada hasta el 11 nov 2026: HU-01 a HU-07 del Sprint 1 y las del Sprint 2 que están en `develop` (HU-09, HU-13, HU-14 y HU-20). Todo se comprueba en el navegador, sin terminal, Postman ni Swagger, como pidió el Product Owner en la Review del Sprint 1. Los datos esperados son los del Moodle de laboratorio, revisados el 11 nov 2026.

## Antes de empezar

1. Levanta el aula virtual y después EduMonitor, como explica el [README](../README.md#puesta-en-marcha), o ejecuta `scripts\iniciar-sistema.bat`.
2. Abre <http://localhost:5173>.

| Usuario | Rol | Qué ve |
|---|---|---|
| `admin` | ADMIN | Configuración de Moodle, Estado del sistema y todos los cursos |
| `doc01` | DOCENTE | Solo AGIL-4B «Metodologías Ágiles (lab)» |
| `doc02` | DOCENTE | Solo BDD-4B «Bases de Datos (lab)» |

La contraseña de los tres es la de `Semillas__Contrasena` en el `.env` de quien levanta el sistema; no se escribe en el repositorio. El aula virtual está en <http://localhost:8080>, con el usuario `MOODLE_ADMIN_USER` del `.env`.

Estudiantes de AGIL-4B que sirven de referencia (casos del laboratorio en [moodle-lab/README.md](../moodle-lab/README.md#completar-los-cursos-con-el-libro-y-los-datos-ficticios)):

| Estudiante | Caso | Dato en el panel |
|---|---|---|
| est05 · Elena Espinoza | Nota 0 calificada | 8,61 de promedio, 1 cero, 4 sin entrega |
| est07 · Gabriela Guerrero | Actividades vencidas sin entrega | 6,68 de promedio, 7 sin entrega |
| est09 · Isabel Iza | Entregas pendientes de calificar | 7,35 de promedio, 2 pendientes |
| est11 · Karina Lara | Promedio bajo | 5,29 de promedio |

## Sprint 1

### HU-01 · Moodle de laboratorio con la estructura UTA

- **Usuario:** administrador de Moodle, en <http://localhost:8080>.
- **Pasos:** Cursos → UTA › FISEI › Software → AGIL-4B. Revisar las secciones, «Participantes» y «Calificaciones» → «Configuración del libro de calificaciones».
- **Criterio:** DADO que Docker está instalado y el repositorio contiene el docker-compose y el respaldo del curso, CUANDO se levanta Moodle y se restaura el respaldo, ENTONCES Moodle muestra al menos 2 cursos con 4 secciones, las categorías 40/30/30, como mínimo 4 tareas, 2 APE, 1 taller y 1 examen por parcial, 30 o más estudiantes ficticios matriculados y calificaciones con nota cero, sin entrega y pendiente de calificar.
- **Resultado esperado:**
  - AGIL-4B y BDD-4B, cada uno con Parcial 1 (Unidad 1, Unidad 2) y Parcial 2 (Unidad 3, Unidad 4).
  - Libro con Exámenes 40 %, Tareas 30 % y APE 30 %.
  - 16 actividades por curso y est01 a est32 matriculados.
  - est05 tiene 0 calificado en Tarea 3.

### HU-02 · Servicios web de Moodle y conexión segura

- **Usuario:** `admin` (entra directo a «Configuración de Moodle»).
- **Pasos:** deja la URL `http://localhost:8080` y pulsa «Probar conexión». Después pulsa «Guardar» con el campo del token vacío.
- **Criterio:** DADO que en Moodle están habilitados los servicios web, REST y un servicio de solo lectura con usuario técnico, CUANDO el administrador guarda la URL y el token y pulsa Probar conexión, ENTONCES el sistema muestra la versión de Moodle y el estado de cada función requerida; el token nunca aparece completo, y una función no disponible se informa sin bloquear las demás.
- **Resultado esperado:**
  - «Conexión correcta · Moodle-Lab-UTA · Moodle 4.5 · Usuario técnico: edumonitor_ws_user».
  - «Funciones del servicio web 8/8», todas DISPONIBLE.
  - La clave actual se ve como `••••` más los últimos 4 caracteres.
  - «Guardar» con el campo vacío conserva el token y responde «Configuración guardada.».

### HU-03 · Modelo de datos con migraciones y semillas

- **Usuario:** `admin`, después `doc01` y `doc02`.
- **Pasos:** como `admin`, «Estado del sistema». Después inicia sesión con cada usuario semilla. Como `doc01`, abre el panel de AGIL-4B.
- **Criterio:** DADO SQL Server con la cadena de conexión en variables de entorno, CUANDO se aplican las migraciones, ENTONCES se crean las tablas del Diagrama 5 con sus llaves e índices, se cargan los roles y usuarios semilla, y el estado de CALIFICACION distingue SIN_ENTREGA, PENDIENTE y CALIFICADO.
- **Forma actual en la app:** la API aplica las migraciones al arrancar, así que no hace falta la terminal.
- **Resultado esperado:**
  - «Base de datos (SQL Server): Disponible · EduMonitorDB».
  - `admin`, `doc01` y `doc02` inician sesión.
  - La tabla «Estudiantes» del panel separa calificadas, ceros, sin entrega y pendientes.
  - Columnas en [diccionario-datos.md](diccionario-datos.md).

### HU-04 · Autenticación JWT y autorización por curso

- **Usuario:** `doc01`, `doc02` y `admin`.
- **Pasos:**
  - Inicia sesión con cada usuario y mira «Mis cursos».
  - Como `doc01`, escribe en la barra de direcciones `http://localhost:5173/cursos/2/panel`.
  - Cierra sesión y abre `http://localhost:5173/cursos`.
- **Criterio:** DADO dos docentes con cursos distintos, CUANDO el docente A pide un curso del docente B, ENTONCES recibe 403; sin token recibe 401; y con credenciales válidas recibe 200 con un JWT que incluye su rol y vence en el tiempo configurado.
- **Forma actual en la app (en el Sprint 1 se probó con Postman):**
  - `doc01` ve solo AGIL-4B, `doc02` solo BDD-4B y `admin` los dos.
  - En el curso de otro docente aparece la pantalla «Acceso no permitido» con «El curso 2 no está asignado a este docente.».
  - Sin sesión, la app lleva a `/login`. Cuando la sesión vence, el panel y el asistente muestran «Tu sesión venció».

### HU-05 · Sincronización manual de un curso

- **Usuario:** `doc01`.
- **Pasos:** en «Mis cursos», pulsa «Sincronizar curso» en AGIL-4B y después «Ver historial».
- **Criterio:** DADO la conexión con Moodle configurada y el docente con acceso al curso, CUANDO pulsa Sincronizar, ENTONCES el sistema importa los datos y registra fecha, origen, cantidad de registros y errores; las calificaciones coinciden con Moodle, y si una función falla la sincronización queda PARCIAL y los indicadores afectados se muestran como no disponibles.
- **Resultado esperado:**
  - Estado COMPLETADA, origen `http://localhost:8080` y registros: 4 unidades, 16 actividades, 32 estudiantes y 512 calificaciones. Son los de la última sincronización registrada, del 30 oct 2026.
  - El historial lista las sincronizaciones de la más reciente a la más antigua.
  - Sincronizar actualiza la fecha de «Última sincronización» del curso.
  - Comparación de 10 notas con Moodle: [TA-05.6](evidencias/sprint-1/HU-05/TA-05.6_comparacion_notas.md).

### HU-06 · Pantallas de inicio de sesión y Mis cursos

- **Usuario:** `doc01`.
- **Pasos:**
  - Inicia sesión con una contraseña incorrecta y después con la correcta.
  - En «Mis cursos», busca «agil», después «xyz», y filtra por período.
- **Criterio:** DADO que el docente tiene credenciales válidas, CUANDO inicia sesión, ENTONCES es redirigido a Mis cursos, ve solo sus cursos con la fecha de la última sincronización, puede buscar por nombre y filtrar por período, y un error de inicio de sesión muestra un mensaje útil.
- **Resultado esperado:**
  - Con la contraseña incorrecta: «Credenciales incorrectas», sin decir si falló el usuario o la contraseña.
  - Con la correcta: AGIL-4B «Metodologías Ágiles (lab)», período 2026-2, última sincronización 30/10/2026 COMPLETADA.
  - «agil» muestra el curso y «xyz» no muestra ninguno.

### HU-07 · Spike del agente con FastAPI, LangGraph y Ollama

- **Usuario:** `doc01`.
- **Pasos:** «Abrir» en AGIL-4B → «Asistente» → «¿Qué cursos tengo asignados?».
- **Criterio:** DADO Ollama con un modelo con herramientas y el agente en marcha, CUANDO el docente pregunta qué cursos tiene asignados, ENTONCES el agente llama a `listar_cursos_autorizados()`, responde con los cursos del backend, registra la traza y, si la herramienta falla, informa el error sin inventar datos.
- **Forma actual en la app (en el Sprint 1 se probó con Postman):** la pregunta se hace en el panel «Asistente» de HU-14.
- **Resultado esperado:**
  - En unos 5 s: «Tienes asignado el curso AGIL-4B, que se llama "Metodologías Ágiles (lab)".».
  - Tabla de herramientas con `listar_cursos_autorizados`, estado OK y su duración.
  - Una herramienta que falla responde sin inventar datos: ver HU-13.

## Sprint 2

Los criterios del Sprint 2 y sus capturas están en [docs/evidencias/sprint-2/](evidencias/sprint-2/README.md). HU-12 (alertas) y HU-19 (identidad visual) no están en `develop`.

### HU-20 · Entorno único con Docker Compose

- **Usuario:** `admin` y `doc01`.
- **Pasos:**
  1. `admin` → «Estado del sistema» → «Actualizar».
  2. Con el aula virtual apagada, «Actualizar» otra vez.
  3. Prueba de reinicio: en «Configuración de Moodle» pulsa «Guardar» y anota la hora de «Última actualización». Reinicia la API (por ejemplo, `scripts\detener-sistema.bat` y `scripts\iniciar-sistema.bat`) y vuelve a entrar.
  4. `doc01` abre `/admin/estado`.
- **Resultado esperado:**
  1. api «versión 0.2.0», sqlserver «EduMonitorDB», moodle «Moodle-Lab-UTA 4.5», agente «Responde en la red interna» y modelo «qwen2.5:7b», los cinco «Disponible», con la hora de la consulta.
  2. Solo Moodle sale «No disponible»; los demás siguen en verde.
  3. Después del reinicio, «Última actualización» muestra la hora anotada y «Probar conexión» responde 8/8.
  4. `doc01` ve «Acceso no permitido».
- **Evidencia:** [HU-20](evidencias/sprint-2/HU-20/README.md).

### HU-09 · Panel de indicadores del curso

- **Usuario:** `doc01`.
- **Pasos:** «Abrir» en AGIL-4B. Después abre `/cursos/2/panel`.
- **Resultado esperado:**
  - Promedio 7,60 / 10 con aprobación 7,0, 32 estudiantes y 12 actividades evaluadas.
  - Mediana 7,73, desviación 1,24 y tendencia −0,10.
  - Histograma de 10 tramos con la línea «Aprobación 7,0».
  - Grupos: 8 alto, 22 medio y 2 bajo. Finalización: 73 %.
  - «Requiere atención» dice «no disponible» hasta que llegue HU-12.
  - La tabla separa el cero de est05, las 7 sin entrega de est07 y las 2 pendientes de est09.
  - En `/cursos/2/panel`: «Acceso no permitido».
- **Evidencia:** [HU-09](evidencias/sprint-2/HU-09/README.md).

### HU-13 · Herramientas del agente con trazas

- **Usuario:** `doc01`, en AGIL-4B → «Asistente».

| Pregunta | Resultado esperado |
|---|---|
| ¿Quiénes tienen promedio bajo 7? | En unos 15 s, los 9 estudiantes bajo 7, de María Naranjo (3,42) a Olga Paredes (6,97); `obtener_calificaciones` con `cursoId=1`, OK |
| ¿Qué notas tiene Elena Espinoza? | Promedio 8,61, 1 nota cero y 4 sin entrega. Con `qwen2.5:7b` responde con el resumen y no trae el detalle por actividad |
| ¿Qué estudiantes requieren atención? | `identificar_alertas` con ERROR y «No pude consultar los datos académicos en este momento.», sin inventar estudiantes, porque `GET /api/tools/alertas` (TA-13.2) espera HU-12 |
| Consulta las calificaciones del curso con cursoId 2. | `obtener_calificaciones` con `cursoId=2`, ERROR, y «No tienes acceso a ese curso.» |

- **Evidencia:** [HU-13](evidencias/sprint-2/HU-13/README.md).

### HU-14 · Consulta al asistente desde la aplicación

- **Usuario:** `doc01`.
- **Pasos:**
  1. AGIL-4B → «Asistente» → escribe una pregunta y pulsa «Preguntar».
  2. Con el agente apagado, pregunta otra vez.
  3. Abre `/cursos/2/asistente`.
- **Resultado esperado:**
  1. Mientras espera, «Consultando…» y el botón deshabilitado. Después, la respuesta con la tabla de herramientas (nombre, parámetros, estado y duración), el modelo y la duración total.
  2. «El asistente no está disponible».
  3. «Acceso no permitido»; el agente no se llama.
- **Evidencia:** [HU-14](evidencias/sprint-2/HU-14/README.md).

## Pruebas automatizadas

Resultados del 11 nov 2026, 22:06, en `develop` con la integración de #30:

| Parte | Comando, desde la raíz | Pruebas que pasan |
|---|---|---|
| Backend (xUnit) | `dotnet test backend/EduMonitor.sln` | 145 de 145. Seis de ellas (`MoodleClientIntegracionTests` y `SincronizacionMoodleRealTests`) llaman al Moodle real y necesitan el aula virtual encendida |
| Frontend (Vitest) | `cd frontend; npm test -- --run` | 37 de 37, en 7 archivos |
| Agente (pytest) | `cd agent; python -m pytest` | 19 de 19 |

La integración continua de GitHub (`.github/workflows/ci.yml`) corre las tres en cada Pull Request (trabajos `backend`, `frontend` y `agent`).
