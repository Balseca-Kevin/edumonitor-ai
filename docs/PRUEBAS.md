# Pruebas en el navegador · EduMonitor AI

Guía para comprobar en la aplicación cada historia entregada hasta el 12 nov 2026: HU-01 a HU-07 del Sprint 1 y las seis del Sprint 2 (HU-20, HU-19, HU-09, HU-12, HU-13 y HU-14), con el diseño D. Todo se comprueba en el navegador, sin terminal, Postman ni Swagger, como pidió el Product Owner en la Review del Sprint 1. Los datos esperados son los del Moodle de laboratorio, revisados el 12 nov 2026 con la sincronización de las 07:07.

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
| est05 · Elena Espinoza | Nota 0 calificada | 8,61 de promedio, 1 con cero, 4 sin entregar |
| est07 · Gabriela Guerrero | Actividades vencidas sin entrega | 6,68 de promedio, 7 sin entregar |
| est09 · Isabel Iza | Entregas pendientes de calificar | 7,35 de promedio, 2 por calificar |
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

Recorrido de la Review en el orden del guion de `docs/sprint-2/PLAN.md`. Los criterios y sus capturas están en [docs/evidencias/sprint-2/](evidencias/sprint-2/README.md).

Antes de la Review:
- Cierra Brave u otros navegadores pesados: le quitan memoria de video al modelo.
- Haz una pregunta de prueba en «Asistente». Si tarda más de 40 s, Ollama perdió la GPU: reinicia el contenedor `ollama` desde Docker Desktop y espera a que «Estado del sistema» diga «Disponible» en «Modelo de lenguaje».

### HU-20 · Entorno único con Docker Compose

- **Usuario:** `admin` y `doc01`.
- **Pasos:**
  1. `admin` → «Estado del sistema» → «Actualizar».
  2. Con el aula virtual apagada, «Actualizar» otra vez.
  3. Prueba de reinicio: en «Configuración de Moodle» pulsa «Guardar» y anota la hora de «Última actualización». Reinicia la API (por ejemplo, `scripts\detener-sistema.bat` y `scripts\iniciar-sistema.bat`) y vuelve a entrar.
  4. `doc01` abre `/admin/estado`.
- **Resultado esperado:**
  1. API de EduMonitor, base de datos, aula virtual, agente de IA y modelo de lenguaje, los cinco «Disponible», con la hora de la consulta.
  2. Solo el aula virtual sale «No disponible»; los demás siguen en verde.
  3. Después del reinicio, «Última actualización» muestra la hora anotada y «Probar conexión» responde 8/8.
  4. `doc01` ve «Acceso no permitido».
- **Evidencia:** [HU-20](evidencias/sprint-2/HU-20/README.md).

### HU-19 · Identidad visual y pantallas de acceso (diseño D)

- **Usuario:** `doc01`.
- **Pasos:**
  1. Abre <http://localhost:5173> y espera 3 s sin tocar nada.
  2. Entra con una contraseña incorrecta y después con la correcta.
  3. «Abrir» en AGIL-4B y mira la cabecera.
  4. Escribe en la barra de direcciones `/cursos/2/panel`, que es el curso BDD-4B.
  5. Opcional: en otra pestaña, con la sesión abierta por más de lo que dura el token, navega a otra pantalla.
- **Resultado esperado:**
  1. Pantalla oscura con «Edu**Monitor**» y «Seguimiento académico y apoyo a tiempo.».
     - Los puntos suben, la curva se dibuja y aparece la raya punteada de 7,0.
     - Debajo de «Pregúntale a tu curso» rotan tres preguntas cada 3 s.
     - Con «Reducir movimiento» del sistema operativo, todo aparece quieto.
  2. «Credenciales incorrectas», sin decir cuál falló. Con la correcta entra a «Mis cursos».
  3. Cabecera oscura con AGIL-4B · 2026-2 y «Metodologías Ágiles (lab)».
     - Tira: PARCIAL 1 con U1 4/4 y U2 4/4; PARCIAL 2 con U3 4/4 y **U4 0/4 · actual**.
     - A la derecha, «Sincronizado 12/11/2026, 07:07 · COMPLETADA».
     - Barra lateral con «← Mis cursos», Panel, Alertas y Asistente.
  4. Página completa «Acceso no permitido» con «El curso 2 no está asignado a este docente.» y «Volver a Mis cursos».
  5. «Tu sesión venció» con «Iniciar sesión».
- **Evidencia:** [HU-19](evidencias/sprint-2/HU-19/README.md).

### HU-09 · Panel de indicadores del curso

- **Usuario:** `doc01`, AGIL-4B → «Panel».
- **Resultado esperado.** Las cifras cuentan desde 0 una sola vez:
  - **Promedio del curso:** 7,60, «sobre 10 · aprobación 7,0».
  - **«La mitad del curso tiene más de»:** 7,73, «las notas varían unos 1,2 puntos alrededor del promedio».
  - **Actividades completadas:** 73 %.
  - **Requiere atención:** 16 alertas activas, con «ver alertas →».
  - **Distribución de notas:** 10 tramos, la línea «7,0 aprobación» y los tramos bajo 6 en arena.
  - **«Cómo se reparte el curso»:** 8 con «8,5 o más», 22 «Entre 6,0 y 8,4» y 2 con «Menos de 6,0».
  - **Tabla de estudiantes:** columnas «Frente al curso», «Notas registradas», «Con cero», «Sin entregar», «Por calificar» y «Últimas notas».
    - Elena Espinoza: 1 con cero.
    - Gabriela Guerrero: 7 sin entregar.
    - Isabel Iza: 2 por calificar.
    - Karina Lara: 5,29 en arena y «2,3 bajo el promedio».
  - En pantalla no aparecen «z», «desviación», «tendencia» ni «grupo».
- **Evidencia:** [HU-09](evidencias/sprint-2/HU-09/README.md) y [CA-6 de HU-19](evidencias/sprint-2/HU-19/CA-6_panel-lenguaje-docente.png).

### HU-12 · Alertas con reglas configurables

- **Usuario:** `doc01`, AGIL-4B → «Alertas», o «ver alertas →» desde el panel.
- **Pasos:**
  1. Revisa la lista.
  2. En «Cuándo avisar» cambia «Promedio bajo» a 5 y pulsa «Guardar».
  3. Vuelve a 7 y «Guardar».
  4. Pulsa «Marcar como revisada» en una alerta.
  5. Pulsa el filtro «Revisadas».
- **Resultado esperado:**
  1. 16 alertas activas para 14 estudiantes, agrupadas por estudiante con «Requiere atención».
     - Cada una lleva la regla («Promedio bajo», «Atrasos» o «Descenso reciente») y su evidencia en una frase. Por ejemplo, «Promedio 6,97 con 5 notas registradas; el mínimo es 7,0».
     - No aparecen R1, R2 ni R4.
  2. «Umbrales guardados»; quedan 9 activas.
  3. Vuelven a ser 16.
  4. La alerta sale de las activas.
  5. Aparece como «Revisada» con su fecha. Úrsula Vaca ya está revisada desde las 07:12.
- **Evidencia:** [HU-12](evidencias/sprint-2/HU-12/README.md).

### HU-13 y HU-14 · Asistente con herramientas

- **Usuario:** `doc01`, AGIL-4B → «Asistente».
- **Pasos y resultado esperado:**

| Acción | Resultado esperado |
|---|---|
| Abrir «Asistente» | Chat de página completa.<br>A la izquierda, «+ Nueva conversación» y el historial de la base en «Hoy», «Esta semana» y «Antes».<br>Al centro, «Hola, Docente Uno. ¿Qué quieres saber de AGIL-4B?» con cuatro sugerencias. |
| Pulsar «¿Qué estudiantes requieren atención?» | «Consultando…» con brillo y el envío deshabilitado.<br>En unos 25 s, los 14 estudiantes con alertas y su motivo, y una tarjeta por cada estudiante nombrado con su promedio y su motivo. |
| «Datos usados» | «Alertas del curso · … ms», la fecha de sincronización y «Respondió el modelo local qwen2.5:7b en … s». No aparecen nombres de funciones. |
| «¿Cómo le va a Karina Lara?» | En unos 10 a 20 s, promedio 5,29 y las 4 actividades sin entregar de la Unidad 4, con su tarjeta.<br>En «Datos usados»: «Notas de Karina Lara». |
| «¿Qué estudiantes de BDD-4B tienen promedio bajo 7?» | Al instante, «No tienes acceso a BDD-4B.», sin llamar al modelo. |
| Pulsar una pregunta del historial | Se abre la respuesta guardada con sus datos usados. |
| «Abrir en el panel» | Vuelve al panel del curso. |
| Con el agente apagado, preguntar | «El asistente no está disponible». |
| Abrir `/cursos/2/asistente` | «Acceso no permitido». |

Resultados medidos el 12 nov en la evaluación de TA-13.7 ([evaluacion.md](evidencias/sprint-2/HU-13/evaluacion.md)):
- Herramienta correcta en 10 de 10 preguntas.
- 5 respuestas correctas, 2 parciales y 3 incorrectas.
- Ningún dato inventado.

Las listas largas («promedio bajo 6», «quién tiene más sin entregar») pueden salir incompletas: el modelo no siempre filtra bien la lista de 32 estudiantes. Se muestran tal cual en la Review y se proponen para el Sprint 3.

- **Evidencia:** [HU-13](evidencias/sprint-2/HU-13/README.md), [HU-14](evidencias/sprint-2/HU-14/README.md) y [CA-7 de HU-19](evidencias/sprint-2/HU-19/README.md).

## Pruebas automatizadas

Resultados del 12 nov 2026, 08:25, en `develop` después de fusionar #35:

| Parte | Comando, desde la raíz | Pruebas que pasan |
|---|---|---|
| Backend (xUnit) | `dotnet test backend/EduMonitor.sln` | 172 de 172. Seis de ellas (`MoodleClientIntegracionTests` y `SincronizacionMoodleRealTests`) llaman al Moodle real y necesitan el aula virtual encendida |
| Frontend (Vitest) | `cd frontend; npm test -- --run` | 71 de 71, en 12 archivos |
| Agente (pytest) | `cd agent; python -m pytest` | 23 de 23 |

La integración continua de GitHub (`.github/workflows/ci.yml`) corre las tres en cada Pull Request (trabajos `backend`, `frontend` y `agent`).
