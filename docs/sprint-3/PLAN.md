# Sprint 3 · plan de trabajo

Sprint: lunes 16 a viernes 27 nov 2026. Sprint Review final: viernes 27 nov 2026. Es el último sprint del proyecto.
Plan del equipo (documento compartido): https://claude.ai/artifact/GtZsV4WVnhjnQN7QJWYXjm

## Sprint Goal

El docente ve la nota real de cada estudiante en cada actividad, recorre su curso por parcial, unidad y actividad, y el asistente, con el contexto de la pantalla y la memoria de la conversación, explica en qué temas falla cada estudiante y prepara su retroalimentación y su refuerzo como documentos descargables. Si el modelo externo falla, responde el modelo local.

## Por qué cambia el plan

En la Review del Sprint 2 (12 nov 2026) el Product Owner señaló un solo error: la aplicación no muestra las notas completas de los estudiantes. La tabla de estudiantes del panel (HU-09) muestra conteos («Con cero», «Sin entregar», «Por calificar») y promedios, pero no la nota de cada estudiante en cada actividad. Las calificaciones están en la base desde HU-05 (512 en AGIL-4B); falta la pantalla. Por eso HU-21 va primero.

El Product Owner también autorizó un modelo de IA externo, con cautela, tolerancia a fallos y plan de respaldo. Las reglas están en `docs/CONTEXTO_IA.md` y `agent/AGENTS.md`.

## Compromiso propuesto

9 HU · 50 SP antes de los agregados. Los SP se confirman con Planning Poker.

| Orden | HU | Historia | SP | Responsable | RF | Rama |
|---|---|---|---|---|---|---|
| 1 | HU-21 (nueva) | Libro de calificaciones: nota de cada estudiante en cada actividad, agrupada por parcial y unidad | 5 | Santiago (pantalla); Kevin (endpoint) | RF-03, RF-05, RF-06 | `feature/HU-21-libro-calificaciones` |
| 2 | HU-10 | Ficha del estudiante: notas por actividad, estados, evolución frente a 7,0, temas débiles y comentarios del docente | 5 | Santiago | RF-04, RF-06 | `feature/HU-10-ficha-estudiante` |
| 3 | HU-08 | Mapeo de actividades y criterios de rúbrica a temas | 8 | Sebastián | RF-13 | `feature/HU-08-mapeo-tematico` |
| 4 | HU-22 (nueva, absorbe HU-11) | Mapa del curso (parcial, unidad, actividad) y panel de actividad | 8 | Kevin (pantallas); Sebastián (cálculos) | RF-08, RF-14 | `feature/HU-22-mapa-y-actividad` |
| 5 | HU-15 | Retroalimentación y refuerzo por estudiante, descargables en PDF y DOCX | 8 | Jeremy; Kevin (generador de documentos) | RF-10, RF-15 | `feature/HU-15-retroalimentacion-refuerzo` |
| 6 | HU-23 (nueva) | Asistente con contexto de pantalla, memoria, filtros en el backend, verificación de cifras y respaldo local | 5 | Kevin; Jeremy (nodos del grafo) | RF-09, RNF-03, RNF-08 | `feature/HU-23-asistente-contexto` |
| 7 | HU-16 | Informe del curso en PDF | 3 | Santiago | RF-11 | `feature/HU-16-informe` |
| 8 | HU-17 | Trazas visibles («Cómo llegó a esta respuesta») y auditoría sin secretos | 3 | Kevin | RF-12 | `feature/HU-17-trazas-auditoria` |
| 9 | HU-18 | Pruebas, README, matriz RF/RNF, evaluación del agente y ensayo | 5 | Todo el equipo | RNF-05, RNF-06 | `chore/HU-18-entrega-final` |

HU-11 se cierra en Jira como fusionada en HU-22. Formato de las HU: «Como [rol], se requiere … para …».

Las tareas base de cada HU (TA-21.1, TA-21.2, …) las define su responsable en la Sprint Planning, con 4 h o menos cada una, y se agregan a este archivo. Las tareas de los agregados ya están definidas abajo (TA-xx.A).

### Criterio que se muestra en la Review

| HU | Criterio |
|---|---|
| HU-21 | 3 notas elegidas por el Product Owner coinciden con Moodle abierto al lado; cero, sin entrega y pendiente se distinguen. |
| HU-10 | La ficha muestra las mismas notas y estados que el libro. |
| HU-08 | 4 a 6 actividades con rúbrica asociada a temas; desempeño por tema con pruebas unitarias. |
| HU-22 | Panel de actividad con enunciado, archivos, entregas, distribución, dominio por criterio y alertas; mapa de calor estudiante × tema. |
| HU-15 | Borrador con criterios fallados y notas reales, editable, aprobado por el docente y descargado en PDF y DOCX. Nada se envía al estudiante. |
| HU-23 | Una pregunta de seguimiento responde bien; con «Usar solo el modelo local» activo responde el local y lo indica. |
| HU-16 | PDF del informe descargado desde la aplicación. |
| HU-17 | «Cómo llegó a esta respuesta» muestra herramientas, parámetros, modelo y duración. |
| HU-18 | README, matriz RF/RNF, pruebas en verde y evaluación del agente en «Calidad del asistente». |

## Agregados como tareas (26 h, sin HU nuevas)

| TA | HU | Tarea | h | Responsable |
|---|---|---|---|---|
| TA-15.A1 | HU-15 | Al aprobar un refuerzo se guardan estudiante, tema y fecha; después de la siguiente sincronización se compara el promedio del tema antes y después | 3 | Sebastián |
| TA-15.A2 | HU-15 | Panel «Refuerzos aplicados» (subió, igual, bajó, sin notas nuevas) y herramienta del asistente para consultarlo | 3 | Jeremy |
| TA-16.A1 | HU-16 | Refuerzos aplicados y su resultado en el informe del curso | 1 | Santiago |
| TA-22.A1 | HU-22 | Por criterio de rúbrica: porcentaje de dominio del curso y si el criterio separa a quienes van bien del resto (correlación criterio-total), con pruebas | 3 | Sebastián |
| TA-22.A2 | HU-22 | Avisos «revisar cómo se enseñó» y «revisar el enunciado» en el panel de actividad; borrador de enunciado corregido desde el asistente | 3 | Kevin |
| TA-10.A1 | HU-10 | Cálculo de la nota mínima para llegar a 7,0 con 40/30/30, con pruebas y herramienta para el asistente | 2 | Kevin |
| TA-10.A2 | HU-10 | Bloque «Para llegar a 7,0» en la ficha, con la leyenda «cálculo con las notas actuales» | 1 | Santiago |
| TA-23.A1 | HU-23 | El verificador devuelve la herramienta y la fila de donde sale cada cifra | 2 | Jeremy |
| TA-23.A2 | HU-23 | Cada cifra de la respuesta abre la celda del libro o el criterio de donde sale | 2 | Kevin |
| TA-23.A3 | HU-23 | Respuesta a preguntas clínicas o psicológicas (fuera de alcance) y 3 preguntas de ese tipo en la evaluación | 1 | Jeremy |
| TA-18.A1 | HU-18 | Endpoint que corre las 18 preguntas de la evaluación contra un modelo y guarda aciertos, datos inventados y tiempos | 3 | Jeremy |
| TA-18.A2 | HU-18 | Pantalla «Calidad del asistente» (solo ADMIN) con la tabla por modelo | 2 | Jeremy |

Horas extra por integrante: Jeremy 11, Kevin 7, Sebastián 6 y Santiago 2. Si se aplica la línea de corte, salen primero TA-22.A1 y TA-22.A2.

## Decisión del sábado 21 nov 2026

Línea de corte: si el burndown muestra más de la mitad de los SP pendientes, la rúbrica se queda en 4 actividades y HU-16 exporta solo el resumen del asistente.

Si no se activa la línea de corte, cada agregado pendiente entra solo si está listo lo que necesita. Kevin lo revisa en la Daily del 21 y registra la decisión en Jira y aquí.

| Agregado | Debe estar listo el 21 | Depende de | TA si entra |
|---|---|---|---|
| Resumen al abrir el curso (cambios desde la sincronización anterior) | HU-21 terminada; la sincronización guarda la fecha en que cambia cada calificación; HU-23 con modelo externo | Kevin, Jeremy | TA-23.B1 diferencias entre sincronizaciones, 3 h, Kevin; TA-23.B2 tarjeta de resumen, 2 h, Santiago |
| Grupos de refuerzo por tema (menos de 6,0 en el mismo tema) | HU-08 probada; borrador individual de HU-15 con PDF | Sebastián, Jeremy, Kevin | TA-15.B1 agrupación, 2 h, Sebastián; TA-15.B2 borrador grupal, 2 h, Jeremy |
| Borradores con el estilo del docente (2 versiones editadas como ejemplo al modelo) | Edición y aprobación con versión final guardada (HU-15); modelo externo con respaldo | Jeremy | TA-15.B3, 2 h, Jeremy |

## Definition of Done del Sprint 3

La del Sprint 2 (`docs/sprint-2/PLAN.md`) sigue vigente, con estos cambios:

- La HU se demuestra con los datos de AGIL-4B y el caso de la demo (un estudiante que falla el mismo tema en 3 actividades).
- Las capturas de evidencia se toman con la interfaz final, con fecha posterior al último cambio de interfaz, en `docs/evidencias/sprint-3/HU-xx/`.
- «Planificada para» coincide con «Hizo». Una reasignación se dice en la Daily y queda en Jira y en este archivo.
- Pull Request de 30 archivos como máximo, salvo migraciones. Un Pull Request por tarea o par de tareas.
- Antes del Pull Request, el responsable recorre su HU en la aplicación con `docs/PRUEBAS.md` y adjunta las capturas.

## Dependencias que no se pueden mover

| Límite | Qué queda listo | Quién lo necesita |
|---|---|---|
| Lun 16 | Contratos de HU-21, HU-22, HU-23 y del generador de documentos en `docs/contratos-api.md` | Todo el equipo |
| Lun 16 | Funciones nuevas de Moodle en `edumonitor_ws` y rúbricas en 4 a 6 actividades de AGIL-4B | Sebastián (HU-08), Kevin (HU-22) |
| Mar 17 | HU-21 terminada y comparada contra Moodle | Santiago (HU-10), Jeremy (TA-23.A1) |
| Jue 19 | Mapeo y desempeño por tema (HU-08) | Kevin (HU-22), Jeremy (HU-15) |
| Vie 20 | Generador de documentos PDF y DOCX | Jeremy (HU-15), Santiago (HU-16) |

## Calendario

| Fecha | Qué |
|---|---|
| Dom 15 | Retrospectiva del Sprint 2, Sprint Planning con Planning Poker, Jira |
| Lun 16 | Contratos publicados; funciones nuevas de Moodle; rúbricas en el aula virtual |
| Lun 16 – mar 17 | HU-21 |
| Mié 18 – lun 23 | HU-08, HU-10, HU-22, HU-15, HU-23 y agregados |
| Sáb 21 | Revisión del burndown y decisión de agregados |
| Mar 24 | HU-16, HU-17 e integración |
| Mié 25 | Congelamiento: solo correcciones |
| Jue 26 | Ensayo con cronómetro, evaluación del agente, video de respaldo |
| Vie 27 | Sprint Review final |

## Evidencia para el informe

Cada responsable guarda en `docs/evidencias/sprint-3/HU-xx/` las capturas de cada criterio de aceptación. Kevin guarda la captura diaria del tablero de Jira y exporta el burndown el 27. La Daily va en `docs/sprint-3/daily.md`.
