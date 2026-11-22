# Sistema de diseño de EduMonitor · Sprint 3

Decisión de Kevin: la interfaz del Sprint 3 sigue el prototipo `docs/sprint-3/prototipo/EduMonitor_Sprint3.dc.html`. Este archivo reemplaza a `docs/sprint-2/diseno.md` como guía de `frontend/`. Lo que este archivo no cambia del diseño D (tipografía, lenguaje docente, accesibilidad, inicio de sesión) sigue vigente.

El prototipo es una referencia visual y de comportamiento. No se sirve ni se copia su código: se reconstruye con los componentes de `frontend/src/components/` y los tokens de `tokens.css`.

## Regla de datos reales

Todos los datos del prototipo son ficticios: 12 estudiantes, 12 actividades, «semana 12 de 16», notas, temas, historial de refuerzos y conteos de la sincronización. En la aplicación no queda ninguno. Cada número, fecha, nombre, tema y estado que se ve sale de Moodle (por la sincronización) o de lo que el docente registra en EduMonitor, y su origen está en la tabla «De dónde sale cada dato».

- Si un dato no está, se muestra «no disponible» o el estado vacío. Nunca un valor de ejemplo, un 0 o un texto fijo que parezca dato.
- La fecha de hoy sale del reloj del servidor de la API (zona `America/Guayaquil`), no del navegador ni de una constante.
- Los únicos valores fijos permitidos son los decorativos que ya marcaba el diseño D: los 32 puntos del inicio de sesión y las 3 preguntas que rotan. Van marcados en el código como decorativos.
- Los textos que redacta el modelo (tareas de refuerzo, borradores, recomendaciones) son borradores: se ven como borrador, el docente los edita y los aprueba. Las cifras que llevan pasan por el verificador de cifras (TA-23.A1).

## Color

Inicio de sesión, barra lateral, cabecera del curso, pantalla de carga y aviso de sincronización usan la paleta oscura del diseño D. El contenido usa papel cálido en lugar del blanco del diseño D.

| Token | Valor | Uso |
|---|---|---|
| `--oscuro-fondo` | `#16181D` | Inicio de sesión, barra lateral, cabecera del curso, pantalla de carga, aviso de sincronización, cabecera de la ficha |
| `--oscuro-superficie` | `#1E2027` | Formulario del inicio, cifras del aviso de sincronización |
| `--oscuro-linea` | `#2A2D35` | Divisiones dentro de lo oscuro, pista de la línea de tiempo |
| `--oscuro-texto` | `#E6E7EA` | Texto sobre oscuro, botón «Ingresar», marcador de hoy |
| `--oscuro-texto-2` | `#9AA0AB` | Texto secundario sobre oscuro |
| `--oscuro-texto-3` | `#8A8F9C` | Etiquetas en letra técnica sobre oscuro |
| `--oscuro-punto` | `#AEB6C6` | Puntos del inicio de 7,0 o más, curva |
| `--oscuro-arena` | `#C9A877` | Puntos bajo 7,0, actividades vencidas con entregas que faltan, contador de alertas en la barra, marca de la pantalla activa |
| `--vivo` | `#8FD0A0` | Actividad que vence esta semana, pasos terminados de la carga, aviso de sincronización completada |
| `--papel` | `#F4F2ED` | Fondo de la aplicación |
| `--hoja` | `#FFFFFF` | Tarjetas y tablas |
| `--hoja-2` | `#FAF8F3` | Encabezados de tabla, filas al pasar el cursor, burbujas del asistente |
| `--linea` | `#E4E0D6` | Bordes de tarjetas |
| `--linea-2` | `#D9D4C7` | Bordes de botones y control segmentado |
| `--tinta` | `#1A1C22` | Texto, reglas de 2 px, botón primario, segmento activo |
| `--tinta-2` | `#6B7080` | Texto secundario |
| `--ia` | `#0E6E60` | Acciones del asistente y del plan de refuerzo, casillas marcadas, celdas nuevas tras sincronizar |
| `--ia-fondo` | `#E3F1EE` | Fondo de la recomendación, del contexto del asistente y de las celdas nuevas |
| `--ia-texto` | `#0A5248` | Texto sobre `--ia-fondo` |
| `--grafico` | `#5B6170` | Barras de 7,0 o más |
| `--arena` | `#D2B48A` | Barras bajo 7,0 |
| `--bajo-fondo` / `--bajo-texto` | `#F3E7D3` / `#7A4E14` | Nota bajo 7,0, «Requiere atención», regla «Promedio bajo» |
| `--sin-entrega-fondo` / `--sin-entrega-texto` | `#F6DDD9` / `#8E2F28` | «Sin entrega» en el libro y las alertas, regla «Atrasos» |
| `--descenso-fondo` / `--descenso-texto` | `#E5E7F3` / `#2E3A8C` | Regla «Descenso reciente» |
| `--ok-fondo` / `--ok` | `#DDEBDD` / `#2F6B3A` | «Al día», refuerzo aprobado |
| `--acento` / `--acento-texto` | `#7B8BD6` / `#3F51B5` | Foco y línea de 7,0 en los gráficos |
| `--error` | `#B3463F` | Sin entrega en la evolución de la ficha, errores |

Se quitan el índigo `#4F46E5` y la paleta slate que proponía el bloque R anterior.

## Tipografía

Igual que el diseño D: Archivo de 300 a 900 y JetBrains Mono para códigos de curso, siglas de actividades, números de semana y datos técnicos. Cifras tabulares en toda la aplicación. Las cifras grandes van en 900 con espaciado −0,04 em: semana actual 64 px, indicadores del panel 60 px, promedio de la ficha 60 px.

## Movimiento

Se mantienen todas las animaciones del diseño D y las del prototipo. Todo queda quieto con `prefers-reduced-motion`.

| Dónde | Animación |
|---|---|
| Inicio de sesión | 32 puntos que suben y flotan, curva que se dibuja en 1,6 s, raya 7,0, entrada escalonada del texto y el formulario, 3 preguntas que rotan cada 3 s, punto «en línea» que late junto al sello |
| Pantalla de carga | Fondo oscuro con los puntos y la curva del inicio, entrada con escala, círculo que gira, barra de avance de arena a verde, cada paso pasa de girando a terminado cuando llega su respuesta real |
| Aviso de sincronización | Baja desde arriba con un pequeño rebote, línea de brillo que recorre el borde superior, se cierra solo a los 8 s |
| Cabecera del curso | Punto «sincronizado» que late; la actividad que vence esta semana lleva un halo verde |
| Libro | Celdas nuevas de la última sincronización con anillo `--ia` |
| Tarjetas | Suben 1 px y oscurecen el borde al pasar el cursor |
| Ficha del estudiante | Entra desde la derecha en 0,28 s sobre un velo |
| Cifras y barras | Las del diseño D: cuentan una vez y crecen una vez, en menos de 3 s |

## Estructura

- Barra lateral oscura de 236 px: «Edu**Monitor**», código del curso y período, navegación (Mis cursos, Libro de calificaciones, Panel del curso, Mapa del curso, Alertas con su contador, Plan de refuerzo, Asistente), usuario y «Cerrar sesión». La pantalla activa lleva una raya de 3 px en `--oscuro-arena`.
- Cabecera oscura del curso a todo el ancho (detalle abajo).
- Barra blanca bajo la cabecera: control segmentado (Todo el ciclo, Parcial 1, Parcial 2, U1 … Un, con tantas unidades como tenga el curso), «Datos de la última sincronización · hace …», botón «Sincronizar» y botón «Preguntar al asistente».
- El control segmentado filtra de verdad el libro, el panel, las alertas y la ficha.

## Cabecera del curso

1. Código, período y cantidad de estudiantes en letra técnica. Nombre del curso en 46 px peso 900.
2. A la derecha, «SEMANA DEL PERÍODO», la semana actual en 64 px y el total de semanas en 30 px gris; debajo, la unidad actual con su nombre del sílabo en `--oscuro-arena`.
3. Línea de tiempo de 108 px de alto:
   - «PARCIAL 1» y «PARCIAL 2» arriba, con una línea punteada entre los dos parciales.
   - Nombre de cada unidad («U3 · Metodologías ágiles del mercado»): la actual en blanco 700, las pasadas en gris, las futuras más oscuras.
   - Pista de 8 px con relleno degradado hasta hoy y cortes entre unidades.
   - Un marcador por actividad en su semana de vencimiento, todos del mismo tamaño: círculo de 18 px para tareas, talleres y APE; rombo de 16 px para exámenes; anillo de 3 px del color del fondo. Color por estado: vencida y todos entregaron (`--oscuro-texto`), vencida con entregas que faltan (`--oscuro-arena`), vence esta semana (`--vivo` con halo), próxima (hueca con borde gris).
   - Números de semana debajo; la semana actual dice «HOY n» en una píldora clara.
   - Sigla de cada actividad debajo de su marcador (T1, APE1, EX P1), del color de su estado. El nombre completo y la fecha van en el `title`.
4. Tres bloques: «VENCIDAS» (cuántas actividades vencieron y cuántas entregas faltan, con los nombres), «VENCE ESTA SEMANA» (actividad y cuántos entregaron, con barra) y «SIGUIENTE» (actividad, semana y temas que evalúa). Si no hay ninguna en un bloque, el bloque lo dice («Nada vence esta semana»).
5. Leyenda de los colores y la forma de los marcadores.

No se muestra «Sincronizado con Moodle · COMPLETADA» ni «Las notas son las mismas del aula virtual» en la cabecera.

## Sincronización

La planificación dice que la sincronización es manual y la inicia el docente (HU-05). Por eso abrir un curso no sincroniza.

- Mis cursos: cada tarjeta tiene «Abrir curso» y «Sincronizar y abrir».
- «Abrir curso» muestra la pantalla de carga «Abriendo curso…» con 3 pasos (Libro de calificaciones, Indicadores y alertas, Estado del período), que avanzan cuando responde cada consulta real. No llama a Moodle.
- «Sincronizar y abrir» y el botón «Sincronizar» de la barra muestran «Sincronizando curso…» con los 5 pasos del mockup MK-05: Cursos y unidades, Matriculados, Ítems y notas por categoría, Entregas y fechas límite, Finalización de actividades. `POST /api/cursos/{id}/sync` responde al terminar, así que la pantalla no inventa un avance por tiempo: mientras espera, los pasos giran; cuando llega la respuesta, cada paso se marca terminado o con su error según `registros` y `errores`, y la barra se llena. La sincronización termina COMPLETADA, PARCIAL o FALLIDA, como ya define HU-05.
- Al terminar baja el aviso «SINCRONIZACIÓN COMPLETADA · AGIL-4B tiene datos nuevos del aula virtual» con 4 cifras reales de las diferencias con la sincronización anterior: notas nuevas, entregas por calificar nuevas, estudiantes que requieren atención, errores. Si no hubo cambios dice «Sin cambios desde la sincronización anterior». Si fue PARCIAL, el aviso usa `--oscuro-arena` y nombra la función que falló.
- «Ver cambios en el libro» abre el libro con las celdas nuevas marcadas. La leyenda del libro suma «Nuevo en la última sincronización».

## Pantallas

| Pantalla | Qué tiene |
|---|---|
| Inicio de sesión | El del diseño D con lo que suma el prototipo: punto «en línea», subtexto que menciona el asistente y el período, etiquetas «Notas tal como en Moodle», «Alertas con evidencia», «Asistente con respaldo local» |
| Mis cursos | Tarjetas: código, nombre, «n requieren atención», promedio del curso, entregas calificadas, semana y unidad actual, barra de avance del período, qué vence esta semana, estado de la última sincronización, «Abrir curso» y «Sincronizar y abrir» |
| Libro de calificaciones | Encabezado de dos filas (unidad con su parcial, y actividad con categoría y semana); cero, sin entrega, por calificar y «aún no vence» diferenciados; promedio 40/30/30; clic en la fila abre la ficha |
| Panel del curso | 4 indicadores con regla de 2 px; el de «Requieren atención» lleva a Alertas. Histograma de 10 tramos con la línea 7,0 que filtra la tabla. Tabla de estudiantes |
| Ficha del estudiante | Panel lateral: promedio, desglose 40/30/30, evolución con la línea 7,0, «Para llegar a 7,0 en el Parcial 2» con la leyenda «cálculo con las notas actuales», sin entregar, «Armar plan de refuerzo» y «Preguntar al asistente por este estudiante» |
| Alertas | Chips por regla, tarjetas con la evidencia en una frase, «Debe: …», «Armar plan de refuerzo» y «Marcar revisada». Modal «Umbrales de alerta» |
| Plan de refuerzo | Tres pasos a la izquierda: actividad evaluada, temáticas donde falló (casillas, con el porcentaje que domina el tema y quién quedó bajo 6,0) y estudiantes. A la derecha, lo que el asistente prepara: dónde se vuelven a evaluar esos temas, tareas de refuerzo, entrega sugerida y qué está en juego para cada estudiante. Botón «Enviar el plan al asistente». Debajo, «Historial de refuerzos» |
| Asistente | Historial con el grupo «Planes de refuerzo» (estado de cada plan) y «Conversaciones». Contexto visible («Sabe: semana n de N · Unidad n · filtro»). Respuestas con Hechos, Inferencia y Recomendación, tarjetas de estudiantes, borradores de plan con «Aprobar borrador» y, ya aprobado, «Descargar PDF» y «Descargar DOCX» |

## Lo que no se muestra en pantalla

Son reglas del sistema, no mensajes para el docente:

- «Nada se envía al estudiante ni a Moodle».
- «Los nombres salen del sistema como códigos (E-001…)».
- Nombres de funciones del agente y del backend.

«Respondió: modelo externo / modelo local (respaldo)», las herramientas usadas, sus parámetros, la duración y las cifras verificadas van dentro del desplegable cerrado «Cómo llegó a esta respuesta» (HU-17). Ahí se cumplen el criterio de HU-23 y el de HU-17 sin cargar la respuesta. Las cifras verificadas son enlaces a la celda del libro de donde salen (TA-23.A2).

## De dónde sale cada dato

| Dato en pantalla | Origen real |
|---|---|
| Semana actual y total de semanas | Fecha del servidor frente a `startdate` y `enddate` del curso (`core_enrol_get_users_courses`). Si `enddate` es 0, el fin es la fecha límite más tardía del curso |
| Unidades, su nombre y su parcial | Secciones de `core_course_get_contents`; el nombre viene del sílabo cargado en Moodle (TA-08.A1); parcial por número de sección, como ya hace `ImportadorUnidades` |
| Unidad actual | La unidad de la siguiente actividad que no ha vencido; si todas vencieron, la última |
| Marcadores de la línea de tiempo | Fecha límite de `mod_assign_get_assignments` y estados de entrega de `mod_assign_get_submissions` guardados en la sincronización |
| Vencidas, faltan entregas, vence esta semana, siguiente | Las mismas fechas límite y estados, contados en el backend |
| Temas de una actividad | Mapeo de HU-08 (actividad → tema y criterio → tema), con los temas iniciales de cada unidad tomados del sílabo cargado en Moodle |
| Porcentaje que domina un tema | Niveles de los criterios de rúbrica (`core_grading_get_gradingform_instances`) mapeados al tema. Sin rúbrica, «no disponible» |
| Quién quedó bajo 6,0 en un tema | Igual que el anterior, por estudiante |
| Dónde se vuelven a evaluar los temas | Actividades que no han vencido y que el mapeo de HU-08 asocia a esos temas, con su fecha límite y su categoría |
| Nota necesaria para 7,0 | Cálculo de TA-10.A1 con 40/30/30 y las actividades pendientes |
| Notas nuevas, entregas nuevas | Diferencia entre la sincronización actual y la anterior (TA-21.A1) |
| Historial de refuerzos y su resultado | Refuerzos guardados en la base (HU-15, TA-15.A1 y TA-15.A2) |
| «hace 2 h» | Fecha de la última sincronización guardada |
