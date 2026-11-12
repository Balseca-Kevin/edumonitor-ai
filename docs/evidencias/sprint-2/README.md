# Evidencias del Sprint 2

Capturas reales de la aplicación en Docker (`http://localhost:5173`), una por criterio de aceptación, con nombre `CA-n_descripcion.png`. Cada carpeta tiene un `README.md` con el criterio, la captura y el dato observado. Estado al 11 nov 2026: **Completa** (una captura por criterio), **Parcial** (faltan criterios) o **Falta** (la HU no está en `develop`).

Los criterios salen de la planificación (RF-07, RF-13, RNF-03, RNF-08 y Tabla 9) y de `docs/sprint-2/PLAN.md`. La redacción oficial en Jira queda por confirmar. Tokens y contraseñas no aparecen; la clave de Moodle se ve enmascarada (`••••` más los últimos 4 caracteres), como en la app.

| HU | Responsable | Criterio | Archivo de evidencia | Estado |
|---|---|---|---|---|
| [HU-09](HU-09/README.md) | Kevin Balseca | CA-1 Promedio con escala, mediana, desviación y fecha de sincronización | [CA-1](HU-09/CA-1_promedio-mediana-desviacion-y-fecha.png) | Completa |
| | | CA-2 Distribución con la línea de 7,0 y grupos | [CA-2](HU-09/CA-2_distribucion-y-grupos.png) | |
| | | CA-3 Cero, sin entrega y pendiente diferenciados | [CA-3](HU-09/CA-3_cero-sin-entrega-y-pendiente.png) | |
| | | CA-4 «No disponible», nunca 0 | [CA-4](HU-09/CA-4_dato-no-disponible-nunca-cero.png) | |
| | | CA-5 Curso ajeno: «Acceso no permitido» | [CA-5](HU-09/CA-5_curso-ajeno-acceso-no-permitido.png) | |
| [HU-13](HU-13/README.md) | Jeremy Guamán (TA-13.1 y TA-13.2: Kevin) | CA-1 Herramienta con datos reales y traza | [CA-1](HU-13/CA-1_herramienta-con-datos-reales-y-traza.png) | Parcial: falta `identificar_alertas` con datos (TA-13.2 espera HU-12) y la evaluación de 10 preguntas (TA-13.7) |
| | | CA-2 Herramienta que falla, sin inventar datos | [CA-2](HU-13/CA-2_herramienta-falla-sin-inventar-datos.png) | |
| | | CA-3 Curso ajeno: «No tienes acceso a ese curso» | [CA-3](HU-13/CA-3_curso-ajeno-sin-acceso.png) | |
| [HU-14](HU-14/README.md) | Kevin Balseca | CA-1 «Consultando…» con el botón deshabilitado | [CA-1](HU-14/CA-1_pregunta-consultando.png) | Completa |
| | | CA-2 Respuesta con las herramientas usadas | [CA-2](HU-14/CA-2_respuesta-con-herramientas-usadas.png) | |
| | | CA-3 Agente caído: «El asistente no está disponible» | [CA-3](HU-14/CA-3_asistente-no-disponible.png) | |
| | | CA-4 Curso ajeno: «Acceso no permitido» | [CA-4](HU-14/CA-4_curso-ajeno-acceso-no-permitido.png) | |
| [HU-20](HU-20/README.md) | Kevin Balseca | CA-1 «Estado del sistema» en verde | [CA-1](HU-20/CA-1_estado-del-sistema-en-verde.png) | Completa |
| | | CA-2 Un servicio caído no detiene a los demás | [CA-2](HU-20/CA-2_servicio-caido-no-detiene-a-los-demas.png) | |
| | | CA-3 La conexión de Moodle sigue guardada tras reiniciar | [CA-3](HU-20/CA-3_conexion-de-moodle-conservada-tras-reinicio.png) | |
| | | CA-4 Un docente no ve «Estado del sistema» | [CA-4](HU-20/CA-4_docente-sin-acceso-a-estado.png) | |
| HU-12 | Sebastián Freire | — | — | Falta: la HU no está en `develop` |
| HU-19 | Santiago Escobar | — | — | Falta: la HU no está en `develop` |

Evidencias del Sprint 1: [docs/evidencias/sprint-1/](../sprint-1/README.md). Guía para repetir cada prueba en el navegador: [docs/PRUEBAS.md](../../PRUEBAS.md).
