# HU-13 · TA-13.7 · Evaluación del asistente con 10 preguntas

Ejecutada por Kevin el jueves 12 nov 2026, de 07:47 a 07:52, contra el asistente real de la aplicación (`POST /api/agente/chat`, el mismo que usa la pantalla «Asistente»):
- Docente `doc01` en el curso AGIL-4B, Metodologías Ágiles (lab), con 32 estudiantes.
- Modelo `qwen2.5:7b` en Ollama con GPU.
- Código de `develop` en 36e051f: herramientas con `nombre` (#37) y `GET /api/tools/alertas` (#39).

Cada respuesta se comparó con lo que devuelve la API para el mismo docente y curso. Fuentes:
- Para notas y promedios: `GET /api/tools/calificaciones` con y sin `nombre`, que da los mismos números que el panel.
- Para las alertas: `GET /api/cursos/1/alertas`.

Las conversaciones quedaron guardadas con los números 41 a 50.

Criterios:
- **Correcta:** Sí, Parcial o No.
  - Sí: todos los datos y la conclusión coinciden con la API.
  - Parcial: los datos que da son reales, pero omite o confunde alguno.
  - No: la conclusión es falsa o deja fuera a la mayoría.
- **Inventó datos:** Sí cuando da una nota, un estudiante o una actividad que no existe en la API.

| # | Tipo | Pregunta | Herramienta esperada | Herramienta usada | Correcta | Inventó datos | Tiempo |
|---|---|---|---|---|---|---|---|
| 1 | Cursos | ¿Qué cursos tengo asignados? | `listar_cursos_autorizados` | `listar_cursos_autorizados` · OK | Sí | No | 5,0 s |
| 2 | Notas por nombre | ¿Qué notas tiene Elena Espinoza? | `obtener_calificaciones` con `nombre` | `obtener_calificaciones` (`nombre=Elena Espinoza`) · OK | Sí | No | 20,6 s |
| 3 | Notas por nombre | ¿Qué notas tiene Isabel Iza? | `obtener_calificaciones` con `nombre` | `obtener_calificaciones` (`nombre=Isabel Iza`) · OK | Parcial | No | 20,9 s |
| 4 | Notas por nombre | ¿Cómo le va a Karina Lara? | `obtener_calificaciones` con `nombre` | `obtener_calificaciones` (`nombre=Karina Lara`) · OK | Sí | No | 10,0 s |
| 5 | Promedio bajo un umbral | ¿Quiénes tienen promedio bajo 7? | `obtener_calificaciones` | `obtener_calificaciones` (`cursoId=1`) · OK | No | No | 15,6 s |
| 6 | Promedio bajo un umbral | ¿Quiénes tienen promedio bajo 6? | `obtener_calificaciones` | `obtener_calificaciones` (`cursoId=1`) · OK | No | No | 23,3 s |
| 7 | Otro curso | ¿Qué estudiantes de BDD-4B tienen promedio bajo 7? | Ninguna: la API responde sin llamar al agente | Ninguna (`modelo: ninguno`) | Sí | No | 0,1 s |
| 8 | Alertas | ¿Qué estudiantes requieren atención? | `identificar_alertas` | `identificar_alertas` (`cursoId=1`) · OK | Sí | No | 23,1 s |
| 9 | Entregas | ¿Quién tiene más actividades sin entregar? | `obtener_calificaciones` | `obtener_calificaciones` (`cursoId=1`) · OK | No | No | 9,3 s |
| 10 | Entregas | ¿Qué actividades le faltan entregar a Gabriela Guerrero? | `obtener_calificaciones` con `nombre` | `obtener_calificaciones` (`nombre=Gabriela Guerrero`) · OK | Parcial | No | 10,8 s |

## Resultado

- **Herramienta correcta en 10 de 10.**
  - Cuando el docente escribe un nombre, el modelo usa `nombre` y no adivina el id.
  - La pregunta sobre otro curso no llega al modelo.
- **Respuestas:** 5 correctas, 2 parciales y 3 incorrectas.
- **Datos inventados en 0 de 10.**
  - Ninguna respuesta da una nota, un estudiante o una actividad que no esté en la API.
  - Los errores son de omisión o de razonamiento sobre datos reales.
- **Tiempo:** 15,4 s en promedio en las 9 preguntas que llegan al modelo; mínimo 5,0 s y máximo 23,3 s. Ninguna se acercó al límite de 60 s.

## Respuesta observada y diferencia con la API

1. «Tienes asignado el curso AGIL-4B, cuyo nombre completo es Metodologías Ágiles (lab).» Coincide.
2. Elena Espinoza:
   - Las 12 notas, con el 0 de la Tarea 3, coinciden; también el promedio 8,61.
   - Dice bien que las Tareas 7 y 8, el APE 4 y el Examen Parcial 2 están sin entregar.
3. Isabel Iza:
   - Las 10 notas y el promedio 7,35 coinciden.
   - La Tarea 6 y el Taller 2 están **por calificar** (`PENDIENTE`), pero el asistente dice «Sin entregar»: confunde pendiente con sin entrega.
4. Karina Lara:
   - El promedio 5,29 y las 4 actividades sin entregar de la Unidad 4 coinciden.
5. Promedio bajo 7:
   - Da 6 de los 9: María Naranjo, Úrsula Vaca, Gabriela Guerrero, Camila Endara, Ana Acosta y Olga Paredes.
   - Faltan Karina Lara (5,29), Nicolás Ortiz (6,14) y Bernardo Duque (6,44).
   - El 11 nov la misma pregunta sacó a los 9 (captura CA-1 de HU-13): el modelo no es constante con listas largas.
6. Promedio bajo 6:
   - Empieza bien con María Naranjo (3,42).
   - Después lista a 4 estudiantes que «no cumplen el criterio».
   - Termina con «No hay estudiantes con promedio ponderado bajo 6», que contradice su primera línea.
   - Faltan María Naranjo y Karina Lara (5,29) en la conclusión.
7. «No tienes acceso a BDD-4B.» La API detecta el código del otro curso y responde sin llamar al agente (HU-14, #36).
8. Requiere atención:
   - Lista a los 14 estudiantes con alertas activas, cada uno con su motivo (8 «Promedio bajo», 4 «Atrasos» y 2 «Descenso reciente»), con los promedios correctos.
   - No dice que Olga Paredes y Gabriela Guerrero también tienen «Atrasos» además de «Promedio bajo»: el curso tiene 16 alertas para 14 estudiantes.
   - Al final nombra los códigos de regla R4, R2 y R1.
9. Más actividades sin entregar:
   - Responde Ana Acosta con 4. Lo correcto es Olga Paredes con 11, seguida de Gabriela Guerrero con 7.
   - Ana tiene 4 de verdad, pero no es la que más tiene.
10. Gabriela Guerrero:
    - Nombra bien las Tareas 5 a 8, el APE 3 y el Examen Parcial 2.
    - Omite el APE 4.
    - Menciona el Taller 2 «ya entregado» dentro de la lista de lo que falta: tiene nota 6,5.

## Qué se aprende

- Con un solo estudiante el asistente es fiable, porque la herramienta con `nombre` le da solo los datos de esa persona. Las fallas aparecen cuando tiene que **filtrar u ordenar** la lista de 32 estudiantes: las preguntas 5, 6 y 9.
- Falla también al distinguir `PENDIENTE` de `SIN_ENTREGA` (pregunta 3), aunque el mensaje de sistema y la descripción de la herramienta lo explican.
- La seguridad se cumple en todas: ningún dato inventado y la pregunta por otro curso se corta antes del modelo.

Propuestas para el Sprint 3, que no se implementaron en este sprint:
- Un filtro `promedioMenorA` y un orden por `sinEntrega` en `GET /api/tools/calificaciones`, para que el backend filtre y ordene con código probado y el modelo solo explique.
- Volver a correr esta tabla después de ese cambio.
