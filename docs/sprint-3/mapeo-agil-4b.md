# Mapeo temático de AGIL-4B (HU-08)

Asociación de las actividades y los criterios de rúbrica de AGIL-4B con los temas del sílabo (`docs/sprint-3/silabos.md`). Se armó leyendo el enunciado de cada tarea y la descripción de cada criterio tal como están en el Moodle de laboratorio (panel de actividad y «Mapa del curso»). No se cambió nada en Moodle.

El mapeo vive en la base de EduMonitor (`ActividadTema` y `CriterioRubrica.TemaId`), no en Moodle. Una base nueva lo trae vacío: se repite con los pasos del final.

## Criterios de rúbrica

Todos los criterios tienen un puntaje máximo de 2,5. «Formato y entrega» no evalúa un tema del sílabo y queda sin tema en las 5 tareas.

| Actividad | Criterio | Tema | Motivo |
|---|---|---|---|
| Tarea 1 | Valores y principios del Manifiesto Ágil | 1.3 Manifiesto Ágil | El criterio nombra el Manifiesto. |
| Tarea 1 | Evolución del software | 1.1 Evolución del software | Mismo nombre que el tema. |
| Tarea 1 | Etapas del proceso de desarrollo de software | 1.2 Etapas del proceso de desarrollo de software | Mismo nombre que el tema. |
| Tarea 2 | Concepto de agilidad | 1.4 Agilidad | El criterio pide el concepto del tema 1.4. |
| Tarea 2 | Proceso tradicional frente a proceso ágil | 1.2 Etapas del proceso de desarrollo de software | El enunciado pide cómo cambia cada etapa del proceso al pasar a un enfoque ágil. |
| Tarea 2 | Ciclo iterativo e incremental | 1.4 Agilidad | El ciclo iterativo es la forma de trabajar que define la agilidad en la unidad 1; la unidad no tiene otro tema más cercano. |
| Tarea 3 | Valores del Manifiesto Ágil en la filosofía del equipo | 1.3 Manifiesto Ágil | El criterio evalúa los valores del Manifiesto. |
| Tarea 3 | Comparación de enfoques ágiles | 2.2 Enfoques ágiles | Mismo tema. |
| Tarea 3 | Filosofía ágil en un caso real | 2.1 Filosofía ágil | Mismo tema. |
| Tarea 5 | Valores del Manifiesto Ágil en las prácticas de Scrum | 1.3 Manifiesto Ágil | El criterio evalúa los valores del Manifiesto, aplicados a Scrum. |
| Tarea 5 | Tablero Kanban y límite WIP | 3.3 Kanban | El tablero y el límite WIP son de Kanban. |
| Tarea 5 | Prácticas de XP | 3.1 XP, Programación Extrema | Mismo tema. |
| Tarea 6 | Eventos y artefactos de Scrum | 3.2 Scrum | Mismo tema. |
| Tarea 6 | Retrospectiva y mejora continua | 3.2 Scrum | La retrospectiva es un evento de Scrum. |
| Tarea 6 | Otras metodologías ágiles | 3.4 Otras metodologías | Mismo tema. |

Los temas de cada una de estas tareas son los de sus criterios: Tarea 1 → 1.1, 1.2, 1.3; Tarea 2 → 1.2, 1.4; Tarea 3 → 1.3, 2.1, 2.2; Tarea 5 → 1.3, 3.1, 3.3; Tarea 6 → 3.2, 3.4.

## Actividades sin rúbrica

| Actividad | Temas | Motivo |
|---|---|---|
| Tarea 9 | 4.1 Herramientas para Scrum, 4.3 Herramientas para Kanban | El enunciado dice «Evalúa 4.1 Herramientas para Scrum y 4.3 Herramientas para Kanban». |
| APE 5 | 4.2 Herramientas para XP, 4.4 Herramientas para otras metodologías | El enunciado dice «Evalúa 4.2 Herramientas para XP y 4.4 Herramientas para otras metodologías». |
| Proyecto integrador | 1.3 a 4.4 (12 temas) | El enunciado dice «Evalúa los temas del curso, del 1.3 Manifiesto Ágil a las herramientas de la unidad 4». |

Quedan sin tema, porque su enunciado en Moodle solo nombra la unidad y no un tema: APE 1, Taller 1, Tarea 4, APE 2, Examen Parcial 1, APE 3, Taller 2, Tarea 7, Tarea 8, APE 4 y Examen Parcial 2. Las 7 que ya tienen notas se cuentan en el aviso «7 actividades con notas no tienen temas» del Mapa del curso. Asociarlas sin un enunciado que lo diga sería inventar el dato; si el docente quiere, las asocia desde la misma pantalla.

## Lo que muestra con los datos reales

- Caso de la demo: est04 (Diego Dávila) tiene 1,33 en 1.3 Manifiesto Ágil, con 0 en Tarea 1, 0 en Tarea 3 y 4,0 en Tarea 5, frente a 8,63 de promedio del curso en ese tema. Su ficha lo lista en «Temas débiles» con las 3 actividades.
- 2.2 Enfoques ágiles es el tema más bajo del curso (promedio 2,32, lo domina el 3 %): sale del criterio «Comparación de enfoques ágiles» de Tarea 3, que casi todo el curso falló.
- Los temas 4.1 a 4.4 todavía no tienen notas; sus próximas evaluaciones son Tarea 9, APE 5 y el Proyecto integrador.

## Cómo repetir el mapeo

1. Entrar como doc01 en `http://localhost:5173`, abrir AGIL-4B y elegir «Mapa del curso».
2. En cada tarea con rúbrica, elegir en cada criterio el tema de la primera tabla.
3. En Tarea 9, APE 5 y Proyecto integrador, marcar los temas de la segunda tabla.
4. Guardar cada actividad. El desempeño por tema, el mapa de calor, la ficha y el plan de refuerzo se actualizan al instante; no hace falta sincronizar.
