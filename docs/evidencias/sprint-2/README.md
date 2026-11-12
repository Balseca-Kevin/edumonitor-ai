# Evidencias del Sprint 2

Capturas reales de la aplicación en Docker (`http://localhost:5173`), una por criterio de aceptación, con nombre `CA-n_descripcion.png`. Cada carpeta tiene un `README.md` con el criterio, la captura y el dato observado. Estado al 12 nov 2026: **Completa** (una captura por criterio), **Parcial** (faltan criterios) o **Falta** (la HU no está en `develop`).

Los criterios salen de la planificación (RF-07, RF-13, RNF-03, RNF-08 y Tabla 9) y de `docs/sprint-2/PLAN.md`. La redacción oficial en Jira queda por confirmar. Tokens y contraseñas no aparecen; la clave de Moodle se ve enmascarada (`••••` más los últimos 4 caracteres), como en la app.

| HU | Responsable | Criterio | Archivo de evidencia | Estado |
|---|---|---|---|---|
| [HU-09](HU-09/README.md) | Kevin Balseca | CA-1 Promedio con escala, mediana, desviación y fecha de sincronización | [CA-1](HU-09/CA-1_promedio-mediana-desviacion-y-fecha.png) | Completa |
| | | CA-2 Distribución con la línea de 7,0 y grupos | [CA-2](HU-09/CA-2_distribucion-y-grupos.png) | |
| | | CA-3 Cero, sin entrega y pendiente diferenciados | [CA-3](HU-09/CA-3_cero-sin-entrega-y-pendiente.png) | |
| | | CA-4 «No disponible», nunca 0 | [CA-4](HU-09/CA-4_dato-no-disponible-nunca-cero.png) | |
| | | CA-5 Curso ajeno: «Acceso no permitido» | [CA-5](HU-09/CA-5_curso-ajeno-acceso-no-permitido.png) | |
| [HU-13](HU-13/README.md) | Jeremy Guamán (TA-13.1 y TA-13.2: Kevin) | CA-1 Herramienta con datos reales y traza | [CA-1](HU-13/CA-1_herramienta-con-datos-reales-y-traza.png) | Completa. La evaluación de TA-13.7 se ejecutó el 12 nov: 10 de 10 herramientas correctas, 5 respuestas correctas, 2 parciales y 3 incorrectas, sin datos inventados |
| | | CA-2 Herramienta que falla, sin inventar datos | [CA-2](HU-13/CA-2_herramienta-falla-sin-inventar-datos.png) | |
| | | CA-3 Curso ajeno: «No tienes acceso a ese curso» | [CA-3](HU-13/CA-3_curso-ajeno-sin-acceso.png) | |
| | | TA-13.7 Evaluación con 10 preguntas al asistente real | [evaluacion.md](HU-13/evaluacion.md) | |
| [HU-14](HU-14/README.md) | Kevin Balseca | CA-1 «Consultando…» con el botón deshabilitado | [CA-1](HU-14/CA-1_pregunta-consultando.png) | Completa |
| | | CA-2 Respuesta con las herramientas usadas | [CA-2](HU-14/CA-2_respuesta-con-herramientas-usadas.png) | |
| | | CA-3 Agente caído: «El asistente no está disponible» | [CA-3](HU-14/CA-3_asistente-no-disponible.png) | |
| | | CA-4 Curso ajeno: «Acceso no permitido» | [CA-4](HU-14/CA-4_curso-ajeno-acceso-no-permitido.png) | |
| [HU-20](HU-20/README.md) | Kevin Balseca | CA-1 «Estado del sistema» en verde | [CA-1](HU-20/CA-1_estado-del-sistema-en-verde.png) | Completa |
| | | CA-2 Un servicio caído no detiene a los demás | [CA-2](HU-20/CA-2_servicio-caido-no-detiene-a-los-demas.png) | |
| | | CA-3 La conexión de Moodle sigue guardada tras reiniciar | [CA-3](HU-20/CA-3_conexion-de-moodle-conservada-tras-reinicio.png) | |
| | | CA-4 Un docente no ve «Estado del sistema» | [CA-4](HU-20/CA-4_docente-sin-acceso-a-estado.png) | |
| [HU-12](HU-12/README.md) | Sebastián Freire (revisión de Kevin) | CA-1 «Requiere atención» en el panel con el conteo de alertas activas | [CA-1](HU-12/CA-1_requiere-atencion-en-el-panel.png) | Completa |
| | | CA-2 Alertas con su evidencia, agrupadas por estudiante | [CA-2](HU-12/CA-2_alertas-con-evidencia.png) | |
| | | CA-3 Cambio de umbral y recálculo | [CA-3](HU-12/CA-3_umbral-promedio-bajo-en-5.png) | |
| | | CA-4 Alerta revisada fuera de las activas | [CA-4](HU-12/CA-4_alertas-revisadas.png) | |
| [HU-19](HU-19/README.md) | Santiago Escobar (diseño D: Kevin) | CA-1 Inicio de sesión propio del sistema | [CA-1](HU-19/CA-1_inicio-de-sesion.png) | Completa |
| | | CA-2 Barra lateral, cabecera y tira del período | [CA-2](HU-19/CA-2_estructura-cabecera-y-tira.png) | |
| | | CA-3 Mis cursos, Moodle y Estado con la misma identidad | [CA-3](HU-19/CA-3_mis-cursos.png) | |
| | | CA-4 «Acceso no permitido» | [CA-4](HU-19/CA-4_acceso-no-permitido.png) | |
| | | CA-5 «Tu sesión venció» | [CA-5](HU-19/CA-5_sesion-vencida.png) | |
| | | CA-6 Panel y alertas en lenguaje docente | [CA-6](HU-19/CA-6_panel-lenguaje-docente.png) | |
| | | CA-7 Asistente como chat con historial | [CA-7](HU-19/CA-7_asistente-respuesta-y-datos-usados.png) | |
| | | CA-8 Sin movimiento y en móvil | [CA-8](HU-19/CA-8_panel-movil.png) | |

Evidencias del Sprint 1: [docs/evidencias/sprint-1/](../sprint-1/README.md). Guía para repetir cada prueba en el navegador: [docs/PRUEBAS.md](../../PRUEBAS.md).
