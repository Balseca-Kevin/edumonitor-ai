# Sprint 3 · plan de trabajo por bloques

Último sprint del proyecto. Las fechas, horas y tiempos del informe los define Kevin; este archivo no lleva fechas ni plazos.
Plan del equipo (documento compartido): https://claude.ai/artifact/GtZsV4WVnhjnQN7QJWYXjm

## Sprint Goal

El docente ve la nota real de cada estudiante en cada actividad, recorre su curso por parcial, unidad y actividad, y el asistente, con el contexto de la pantalla y la memoria de la conversación, explica en qué temas falla cada estudiante y prepara su retroalimentación y su refuerzo como documentos descargables. Si el modelo externo falla, responde el modelo local.

## Por qué cambia el plan

En la Review del Sprint 2 el Product Owner señaló un solo error: la aplicación no muestra las notas completas de los estudiantes, solo cálculos. Las calificaciones están en la base desde HU-05; falta la pantalla. Por eso las notas reales van primero. El Product Owner también autorizó un modelo de IA externo, con cautela, tolerancia a fallos y plan de respaldo.

## Cómo se trabaja en este sprint

- El trabajo se divide en bloques. Cada bloque tiene un responsable, pero cualquier integrante puede adelantar cualquier bloque. Kevin adelanta los bloques que no avancen para no depender de entregas de último momento.
- Quien hace una tarea queda registrado en la columna «Hizo». Cada responsable debe poder explicar su HU en la Review aunque otro la haya adelantado.
- El Product Owner revisa el proyecto como carpeta (OneDrive o copia local), no en GitHub. Todo lo que se demuestra tiene que funcionar desde esa carpeta siguiendo el README, y las evidencias van dentro de ella en `docs/evidencias/sprint-3/HU-xx/`.
- Los bloques se ejecutan en el orden de la tabla. Dentro de un bloque, una tarea por sesión de Claude Code.

## Historias del sprint

| HU | Historia | SP | Responsable | RF | Estado |
|---|---|---|---|---|---|
| HU-21 | Libro de calificaciones: nota de cada estudiante en cada actividad, agrupada por parcial y unidad | 5 | Santiago (pantalla); Kevin (endpoint) | RF-03, RF-05, RF-06 | Endpoint y pantalla hechos (pantalla adelantada por Kevin) |
| HU-10 | Ficha del estudiante: notas por actividad, estados, evolución frente a 7,0, temas débiles y comentarios del docente | 5 | Santiago | RF-04, RF-06 | Por hacer |
| HU-08 | Mapeo de actividades y criterios de rúbrica a temas | 8 | Sebastián | RF-13 | Rúbricas y funciones de Moodle en PR #49; falta backend y pantalla |
| HU-22 | Mapa del curso (parcial, unidad, actividad) y panel de actividad; absorbe HU-11 | 8 | Kevin (pantallas); Sebastián (cálculos) | RF-08, RF-14 | Por hacer |
| HU-15 | Retroalimentación y refuerzo por estudiante, descargables en PDF y DOCX | 8 | Jeremy; Kevin (generador de documentos) | RF-10, RF-15 | Por hacer |
| HU-23 | Asistente con contexto de pantalla, memoria, filtros en el backend, verificación de cifras y respaldo local | 5 | Kevin; Jeremy (nodos del grafo) | RF-09, RNF-03, RNF-08 | Filtros en PR #51; falta el resto |
| HU-16 | Informe del curso en PDF (solo el resumen del asistente, por la línea de corte) | 3 | Santiago | RF-11 | Por hacer |
| HU-17 | Trazas visibles («Cómo llegó a esta respuesta») y auditoría sin secretos | 3 | Kevin | RF-12 | Por hacer |
| HU-18 | Pruebas, README, matriz RF/RNF, evaluación del agente, paquete de entrega y ensayo | 5 | Todo el equipo | RNF-05, RNF-06 | Por hacer |

HU-11 se cierra en Jira como fusionada en HU-22. Formato de las HU: «Como [rol], se requiere … para …».

## Decisión de la revisión del burndown

Se activó la línea de corte (más de la mitad de los SP pendientes):

- La rúbrica se queda en las actividades que ya tiene el aula virtual (PR #49).
- HU-16 exporta solo el resumen del asistente.
- Salen TA-22.A1 y TA-22.A2 (diagnóstico por criterio).
- No entra ningún agregado TA-xx.B (resumen al abrir el curso, grupos de refuerzo, estilo del docente). Después, Kevin reincorporó las diferencias entre sincronizaciones (antes TA-23.B1) como TA-21.A1; ver «Datos reales y tareas del rediseño».

## Datos reales y tareas del rediseño

Regla de Kevin: todo dato que se ve en la aplicación es real. Fechas, semanas, unidades, temas, notas, entregas, cambios entre sincronizaciones e historial de refuerzos salen de Moodle o de lo que el docente registra en EduMonitor. Nada queda de adorno ni de ejemplo. El origen de cada dato está en `docs/sprint-3/diseno.md`, «De dónde sale cada dato». Si el Moodle de laboratorio no tiene un dato que la pantalla necesita, se agrega en Moodle con sus scripts (`moodle-lab/scripts/`), nunca en el frontend ni en una semilla de EduMonitor.

Lo que pide el prototipo y no estaba en el plan entra como tareas de HU que ya existen, no como HU nuevas:

| TA | HU | Tarea | Responsable | Hizo |
|---|---|---|---|---|
| TA-08.A1 | HU-08 | Sílabos en el Moodle de laboratorio (`docs/sprint-3/silabos.md`): nombre de cada sección y sus temas en los 3 cursos; sección 5 de BDD-4B; enunciados y criterios de rúbrica de AGIL-4B alineados con los temas de su unidad, sin cambiar las notas ni los casos de la demo (est04 falla el mismo tema en 3 actividades; casi todo el curso falla un criterio de Tarea 3). La sincronización importa el nombre de la sección y HU-08 toma los temas como temas iniciales de cada unidad | Kevin | Kevin |
| TA-22.A3 | HU-22 | Actividades de AGIL-4B que vencen después de la Review, para que la línea de tiempo y el plan de refuerzo tengan evaluaciones futuras. Nombres, categorías y fechas límite los da Kevin antes de empezar; se crean con el script de carga y se mapean a temas | Kevin | Kevin (actividades en el aula virtual; el mapeo a temas queda para la pantalla de HU-08) |
| TA-22.A4 | HU-22 | Estado del período: `GET /api/cursos/{id}/periodo` (semana actual y total, unidades con su nombre y fechas, actividades con fecha límite y estado, vencidas, entregas que faltan, vence esta semana, siguiente) y la herramienta del asistente `GET /api/tools/estado-periodo`, con pruebas | Kevin; Sebastián (cálculos) | Kevin |
| TA-22.A5 | HU-22 | Cabecera del curso con la línea de tiempo, los tres bloques y la leyenda, según `diseno.md` | Kevin | Kevin (también la semana en las tarjetas de Mis cursos y en «Sabe: …» del asistente) |
| TA-21.A1 | HU-21 | Diferencias entre sincronizaciones: la sincronización guarda qué calificaciones y entregas cambiaron frente a la anterior; `GET /api/cursos/{id}/sync/{sid}/cambios` con los conteos y las celdas | Kevin | Kevin (la pantalla que lo muestra es TA-21.A2) |
| TA-21.A2 | HU-21 | Pantalla de carga («Abriendo curso…» y «Sincronizando curso…»), aviso grande al terminar con las cifras de TA-21.A1 y celdas nuevas marcadas en el libro | Santiago | Kevin (pantalla de carga, aviso y celdas nuevas marcadas en el libro) |
| TA-15.A3 | HU-15 | Pantalla «Plan de refuerzo»: actividad evaluada, temáticas donde falló con el porcentaje que domina cada tema y quién quedó bajo 6,0, estudiantes, dónde se vuelven a evaluar esos temas, nota necesaria; «Enviar el plan al asistente» guarda el plan (`POST /api/cursos/{id}/refuerzos`) | Jeremy | |
| TA-15.A4 | HU-15 | El asistente recibe el plan como foco, redacta el borrador por estudiante (tareas por tema, entrega sugerida antes de la próxima evaluación de esos temas, qué está en juego) en una conversación propia; «Aprobar borrador» habilita PDF y DOCX; «Historial de refuerzos» en el plan y en el asistente con su estado y el resultado de TA-15.A1 | Jeremy; Kevin (contrato del chat) | |

TA-21.A1 es el agregado TA-23.B1 que la línea de corte había dejado fuera; Kevin lo reincorpora como tarea de HU-21. La línea de corte sigue vigente para TA-22.A1, TA-22.A2, TA-15.B1, TA-15.B2 y TA-15.B3.

## Bloques

| Orden | Bloque | Responsable | Depende de |
|---|---|---|---|
| 0 | Integración de lo que ya está en PR | Kevin | — |
| M | Moodle alineado con los sílabos y actividades futuras (TA-08.A1, TA-22.A3) | Kevin | 0 |
| R | Rediseño de la interfaz según `docs/sprint-3/diseno.md` | Kevin | 0 |
| 1 | Notas reales: libro y ficha | Santiago | 0, R |
| 2 | Rúbricas y temas | Kevin (sincronización), Sebastián (mapeo) | 0 |
| 3 | Asistente con contexto y respaldo | Kevin, Jeremy | 0 |
| 4 | Retroalimentación, refuerzo y documentos | Jeremy, Kevin, Sebastián, Santiago | 2, 3 |
| 5 | Curso y actividad | Kevin, Sebastián | 2, R |
| 6 | Trazas y auditoría | Kevin | 3 |
| 7 | Cierre y entrega en carpeta | Todo el equipo | 1 a 6 |

### Bloque 0 · Integración (Kevin)

- Fusionar en `develop`, en este orden: #48 (contexto), #49 (rúbricas en el aula virtual), #50 (endpoint del libro, TA-21.1) y #51 (filtros de calificaciones, HU-23).
- Corregir `SincronizacionMoodleRealTests`: espera la nota 0 de est05 en Tarea 3, que se cambió a mano a 5,5 en Moodle. Se devuelve la nota en Moodle o se actualiza la prueba. Hizo: Kevin. Se quitó la sobrescritura del libro con `moodle-lab/scripts/restaurar_casos_demo.php`: la tarea ya tenía 0, y el libro vuelve a mostrarlo.

### Bloque R · Rediseño de la interfaz (Kevin)

Guía: `docs/sprint-3/diseno.md` y el prototipo `docs/sprint-3/prototipo/EduMonitor_Sprint3.dc.html`, aprobados por Kevin. Reemplaza la propuesta anterior de índigo y slate. Este bloque va antes de las pantallas nuevas para no hacerlas dos veces. Se mantienen todas las animaciones del diseño D y las del prototipo.

- R.1 Tokens del Sprint 3 en `tokens.css`: paleta oscura del diseño D para inicio, barra lateral, cabecera, carga y aviso; papel `#F4F2ED` para el contenido; verde petróleo `#0E6E60` para las acciones del asistente y del plan de refuerzo. Hizo: Kevin.
- R.2 Barra lateral oscura con la navegación del curso y el contador de alertas; barra blanca con el control segmentado que filtra de verdad, «Sincronizar» y «Preguntar al asistente». Hizo: Kevin. El filtro llega al backend con el parámetro `tramo` (contrato «Tramo del período»); Libro, Mapa y Plan de refuerzo se suman a la barra cuando exista su pantalla.
- R.3 Mis cursos en tarjetas con «Abrir curso» y «Sincronizar y abrir» (la pantalla de carga es TA-21.A2). Hizo: Kevin. La semana, la unidad actual y lo que vence esta semana se sumaron a la tarjeta con TA-22.A5.
- R.4 Panel: 4 indicadores, histograma con la línea de 7,0 que filtra la tabla, ficha lateral del estudiante con «Armar plan de refuerzo» y «Preguntar al asistente por este estudiante». Hizo: Kevin. La ficha suma el desglose 40/30/30 y «aún no vence» calculados en el backend; «Para llegar a 7,0» llega con TA-10.A1.
- R.5 Alertas en tarjetas con chips por regla, «Debe: …», «Armar plan de refuerzo», «Marcar revisada» y el modal «Umbrales de alerta». Hizo: Kevin.
- R.6 Asistente: historial con los grupos «Planes de refuerzo» y «Conversaciones», contexto visible y el desplegable cerrado «Cómo llegó a esta respuesta». Hizo: Kevin. El grupo «Planes de refuerzo» aparece con TA-15.A4; la semana del período ya está en «Sabe: …» (TA-22.A5).
- R.7 Inicio de sesión del diseño D con lo que suma el prototipo. Hizo: Kevin.

Límites que no cambian: las alertas dicen «Requiere atención», nunca «riesgo» ni «reprobación»; sin emoji; sin términos estadísticos. Las reglas del sistema (nada se envía a Moodle ni al estudiante, seudonimización) se cumplen pero no se escriben en pantalla.

### Bloque M · Moodle alineado con los sílabos (Kevin)

- TA-08.A1 Sílabos en el Moodle de laboratorio (`docs/sprint-3/silabos.md`).
- TA-22.A3 Actividades de AGIL-4B que vencen después de la Review; nombres, categorías y fechas los da Kevin.
- Los scripts actualizan el aula virtual sin borrarla: nunca `docker compose down -v` ni `docker volume rm`. Después de cargar, se sincroniza desde la aplicación y se corre `SincronizacionMoodleRealTests`.

### Bloque 1 · Notas reales (Santiago)

- TA-21.2 Pantalla `Libro.jsx`, primera del curso, con cero, sin entrega y pendiente diferenciados y «no disponible» cuando falta el dato. Hizo: Kevin, con «aún no vence», el tramo, la búsqueda, el orden, la ficha al hacer clic y las celdas nuevas de TA-21.A2.
- TA-21.3 Prueba de interfaz del libro. Hizo: Kevin (`Libro.test.jsx`).
- HU-10 Ficha del estudiante (`GET /api/cursos/{id}/estudiantes/{eid}`; se detalla el contrato antes).
- TA-10.A1 Cálculo de la nota necesaria para llegar a 7,0, con pruebas y herramienta del asistente (Kevin).
- TA-10.A2 Bloque «Para llegar a 7,0» en la ficha (Santiago).
- TA-21.A1 Diferencias entre sincronizaciones (Kevin) y TA-21.A2 pantalla de carga, aviso y celdas nuevas en el libro (Santiago).

### Bloque 2 · Rúbricas y temas (Kevin, Sebastián)

- Kevin: tres métodos nuevos en `IMoodleClient` (rúbricas, calificación por criterio, estado de cada entrega con el comentario del docente), entidad y migración de los criterios de rúbrica, importación en la sincronización.
- Sebastián, HU-08: temas y contenidos mínimos por unidad, mapeo actividad → tema y criterio → tema, desempeño por tema con pruebas, pantalla de mapeo.

### Bloque 3 · Asistente con contexto y respaldo (Kevin, Jeremy)

- Kevin: contrato de `POST /api/agente/chat` con `conversacionId`, `foco`, `respaldo` y `cifras`; foco de la pantalla y últimos 4 turnos hacia el agente; seudonimización (E-001…); modelo externo con respaldo local (20 s, 3 fallos en 5 minutos, variables en `.env.example`); `GET/PUT /api/admin/modelo` y el interruptor en «Estado del sistema»; «Respondió: modelo externo / local».
- Jeremy: verificador de cifras (TA-23.A1); preguntas fuera de alcance (TA-23.A3).
- Kevin: cada cifra de la respuesta abre la celda del libro de donde sale (TA-23.A2).

### Bloque 4 · Retroalimentación, refuerzo y documentos

- Kevin: generador de documentos PDF (QuestPDF) y DOCX.
- Jeremy, HU-15: borradores de retroalimentación y refuerzo por estudiante, editables, aprobados por el docente y descargables.
- Sebastián, TA-15.A1: promedio del tema antes y después del refuerzo aprobado.
- Jeremy, TA-15.A2: panel «Refuerzos aplicados» y su herramienta.
- Jeremy, TA-15.A3 y TA-15.A4: pantalla «Plan de refuerzo», envío del plan al asistente, borrador por estudiante, aprobación e historial.
- Santiago, HU-16: informe del curso con el resumen del asistente; TA-16.A1 si entra TA-15.A1.

### Bloque 5 · Curso y actividad (Kevin, Sebastián)

- Kevin: `GET /api/cursos/{id}/actividades/{aid}` (se detalla el contrato antes), panel de actividad (enunciado, archivos, entregas, distribución, dominio por criterio, alertas) y mapa del curso.
- Sebastián: cálculos por actividad, criterio y tema; mapa de calor estudiante × tema.
- Kevin, TA-22.A4 y TA-22.A5: estado del período (endpoint y herramienta) y cabecera del curso con la línea de tiempo.

### Bloque 6 · Trazas y auditoría (Kevin)

- HU-17: «Cómo llegó a esta respuesta» (herramientas, parámetros, modelo y duración) y pantalla de auditoría para ADMIN.

### Bloque 7 · Cierre y entrega en carpeta (todo el equipo)

- README que se sigue desde la carpeta entregada, sin GitHub.
- Matriz RF/RNF y registro de contribuciones.
- Jeremy: TA-18.A1 y TA-18.A2, evaluación de 18 preguntas con los dos modelos y pantalla «Calidad del asistente».
- Capturas de cada criterio en `docs/evidencias/sprint-3/HU-xx/`, con la interfaz final.
- Paquete para OneDrive: carpeta del proyecto sin `node_modules`, `bin`, `obj`, `.env` ni secretos, más el informe y el video.
- Congelamiento, ensayo con cronómetro, video de respaldo y Review.

## Criterio que se muestra en la Review

| HU | Criterio |
|---|---|
| HU-21 | 3 notas elegidas por el Product Owner coinciden con Moodle abierto al lado; cero, sin entrega y pendiente se distinguen. |
| HU-10 | La ficha muestra las mismas notas y estados que el libro. |
| HU-08 | Actividades con rúbrica asociadas a temas; desempeño por tema con pruebas unitarias. |
| HU-22 | Cabecera con la semana real del período, las actividades vencidas y la siguiente evaluación de AGIL-4B. Panel de actividad con enunciado, archivos, entregas, distribución, dominio por criterio y alertas; mapa de calor estudiante × tema. |
| HU-15 | Plan de refuerzo con los temas marcados en una actividad real, enviado al asistente; borrador con criterios fallados y notas reales, editable, aprobado por el docente y descargado en PDF y DOCX. |
| HU-23 | Una pregunta de seguimiento responde bien; con «Usar solo el modelo local» activo responde el local y lo indica. |
| HU-16 | PDF del informe descargado desde la aplicación. |
| HU-17 | «Cómo llegó a esta respuesta» muestra herramientas, parámetros, modelo y duración. |
| HU-18 | README, matriz RF/RNF, pruebas en verde y evaluación del agente en «Calidad del asistente». |

## Definition of Done del Sprint 3

La del Sprint 2 (`docs/sprint-2/PLAN.md`) sigue vigente, con estos cambios:

- La HU se demuestra con los datos de AGIL-4B y el caso de la demo (un estudiante que falla el mismo tema en 3 actividades).
- Las capturas de evidencia se toman con la interfaz final.
- «Hizo» dice quién hizo de verdad cada tarea.
- Pull Request de 30 archivos como máximo, salvo migraciones.
- Antes del Pull Request, quien hizo la tarea recorre la HU en la aplicación con `docs/PRUEBAS.md`.
