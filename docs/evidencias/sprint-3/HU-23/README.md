# HU-23 · Asistente con contexto, memoria y respaldo

Capturas pendientes: las toma quien recorre la historia en la aplicación (`http://localhost:5173`), con doc01 en AGIL-4B o con admin en las pantallas de administración. Los pasos están en [docs/PRUEBAS.md](../../../PRUEBAS.md#sprint-3).

| Criterio | Qué se ve | Archivo |
|---|---|---|
| CA-1 | Pregunta de seguimiento respondida en el mismo hilo, sin repetir el nombre | `CA-1_seguimiento.png` |
| CA-2 | Cifras enlazadas al libro y «Respondió: modelo externo» en «Cómo llegó a esta respuesta» | `CA-2_cifras-y-modelo.png` |
| CA-3 | «Usar solo el modelo local» activo en Estado del sistema y la respuesta que dice «Respondió: modelo local» | `CA-3_solo-modelo-local.png` |
| CA-4 | Pregunta fuera de alcance respondida sin modelo | `CA-4_fuera-de-alcance.png` |
| CA-5 | Con «U2» en la barra, el texto y las tarjetas del asistente dicen las mismas cifras de la unidad 2 | `CA-5_tramo.png` |
| CA-6 | Pregunta general («¿Qué es Scrum?») y pregunta sobre la aplicación («¿Ya puedes generar tareas de refuerzo?») respondidas sin consultar datos | `CA-6_preguntas-generales.png` |

## C7 · Tramo y preguntas generales

Antes, con «U2» elegido, el texto del asistente usaba las alertas de todo el ciclo y las tarjetas las de `?tramo=u2`, así que no coincidían. Ahora la API manda el tramo al agente, y el agente lo agrega a `obtener_calificaciones` e `identificar_alertas`. Además, un enrutador con reglas manda sin herramientas las preguntas generales y las de cómo se usa la aplicación.

Prueba con el modelo local (`qwen2.5:7b`), 12 preguntas sin datos del curso. Todas se respondieron sin herramientas, sin «riesgo» y sin el tramo en el texto.

Las cuatro respuestas débiles de la primera corrida (pasos del plan de refuerzo, «Sí,» en «¿Qué puedes hacer?», «¿Cómo estás?» después de «Gracias» y el saludo «Hola Edu») se corrigieron en la guía de la aplicación, en el prompt base y en `redaccion.pulir`, con pruebas en `agent/tests/test_c7.py`. La tabla muestra la corrida después del cambio; las filas que no se repitieron conservan el resultado anterior.

| Pregunta | Tiempo | Resultado |
|---|---|---|
| ¿Ya puedes generar tareas de refuerzo? | 10,9 s | Empieza con «Sí» y da los 5 pasos reales: «Mis cursos» y «Abrir curso», «Plan de refuerzo» en la barra lateral, pestaña «Por actividad», «Enviar el plan al asistente», aprobar y descargar PDF o DOCX |
| ¿Qué puedes hacer? | 13,4 s | Empieza con «Puedo ayudarte…» y lista lo que hace el asistente |
| ¿Quién eres? | 4,3 s | Correcta |
| ¿Qué es Scrum? | 6,5 s | Correcta |
| Explícame Kanban en dos frases | 3,2 s | Correcta, dos frases |
| Dame ideas de actividades para repasar Kanban | 21,6 s | 7 ideas usables |
| ¿Cuál es la capital de Francia? | 0,6 s | Correcta |
| Hola | 0,9 s | «Hola.» |
| Gracias | 1,5 s | «De nada.», sin preguntar nada |
| ¿Cómo descargo el plan en PDF? | 9,8 s | Pasos correctos hasta «Descargar PDF» |
| Rúbrica de historias de usuario con 3 niveles | 29,2 s | Usable, genérica |
| ¿Qué diferencia hay entre gestión de configuración y control de versiones? | 13,6 s | Correcta, sin saludo |

Al guardar cada captura, se cambia el nombre del archivo por un enlace y se agrega el dato observado.
