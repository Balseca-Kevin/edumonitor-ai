# HU-14 · Consulta al asistente desde la aplicación

Capturas de la aplicación en Docker (`http://localhost:5173`) del 11 nov 2026, entre las 21:45 y las 21:50, con doc01 en AGIL-4B, sección «Asistente». Modelo `qwen2.5:7b` en la GPU.

Los criterios salen del plan del Sprint 2 (TA-14.1 a 14.4), del contrato de `POST /api/agente/chat` y de `docs/sprint-2/diseno.md`. La redacción oficial en Jira queda por confirmar.

| Criterio | Captura | Dato observado |
|---|---|---|
| CA-1 · CUANDO el docente envía una pregunta, ENTONCES ve «Consultando…» y el botón deshabilitado mientras espera (diseno.md) | [CA-1_pregunta-consultando.png](CA-1_pregunta-consultando.png) | «¿Qué notas tiene Elena Espinoza?» con «Consultando…» y los botones deshabilitados |
| CA-2 · DADO doc01 en AGIL-4B, CUANDO pregunta, ENTONCES ve la respuesta y la tabla de herramientas usadas con parámetros, estado y duración (TA-14.3) | [CA-2_respuesta-con-herramientas-usadas.png](CA-2_respuesta-con-herramientas-usadas.png) | Respuesta en 8 s: promedio 8,61, 1 nota cero y 4 sin entrega; `obtener_calificaciones` con `cursoId=1`, estado OK |
| CA-3 · DADO el agente apagado, CUANDO el docente pregunta, ENTONCES ve «El asistente no está disponible» (503, TA-14.4) | [CA-3_asistente-no-disponible.png](CA-3_asistente-no-disponible.png) | Con el contenedor `agent` detenido: «El asistente no está disponible. Revisa que el servicio del agente esté encendido.» |
| CA-4 · DADO un curso de otro docente, CUANDO doc01 abre su asistente, ENTONCES ve «Acceso no permitido» y el agente no se llama (403, TA-14.4) | [CA-4_curso-ajeno-acceso-no-permitido.png](CA-4_curso-ajeno-acceso-no-permitido.png) | `/cursos/2/asistente` (BDD-4B): «El curso 2 no está asignado a este docente.» |

Límite observado: con `qwen2.5:7b`, la pregunta por las notas de un estudiante se responde con el resumen (promedio y conteos), sin la segunda consulta con `estudianteId` que traería cada actividad.
