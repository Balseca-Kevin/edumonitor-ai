# HU-20 · Entorno único de ejecución con Docker Compose

Capturas de la aplicación en Docker (`http://localhost:5173`) del 11 nov 2026 a las 21:49, con EduMonitor levantado con `docker compose up -d --wait` y el aula virtual aparte (`moodle-lab/`). La hora de cada consulta se ve en la columna «Consulta» de «Estado del sistema».

Los criterios salen del plan del Sprint 2 (TA-20.1 a 20.6 y paso 2 del guion de la Review) y del contrato de `GET /api/admin/estado`. La redacción oficial en Jira queda por confirmar.

| Criterio | Captura | Dato observado |
|---|---|---|
| CA-1 · DADO EduMonitor levantado con un solo comando, CUANDO el ADMIN abre «Estado del sistema», ENTONCES ve cada servicio con su estado, detalle y hora de consulta | [CA-1_estado-del-sistema-en-verde.png](CA-1_estado-del-sistema-en-verde.png) | api «versión 0.2.0», sqlserver «EduMonitorDB», moodle «Moodle-Lab-UTA 4.5», agente «Responde en la red interna», modelo «qwen2.5:7b»: los cinco «Disponible» a las 09:49 p. m. |
| CA-2 · DADO un servicio caído, ENTONCES sale «No disponible» y los demás se siguen consultando (máximo 3 s cada uno) | [CA-2_servicio-caido-no-detiene-a-los-demas.png](CA-2_servicio-caido-no-detiene-a-los-demas.png) | Con el aula virtual detenida, Moodle «No disponible: No se pudo conectar con Moodle»; los otros cuatro siguen «Disponible» |
| CA-3 · DADO la conexión de Moodle guardada, CUANDO se reinicia la API, ENTONCES la clave sigue guardada y la conexión funciona (TA-20.4) | [CA-3_conexion-de-moodle-conservada-tras-reinicio.png](CA-3_conexion-de-moodle-conservada-tras-reinicio.png) | «Última actualización: 11/11/26, 8:43 p. m.» aunque la API arrancó de nuevo a las 21:27; clave actual enmascarada (solo los últimos 4 caracteres); «Conexión correcta», 8/8 funciones |
| CA-4 · DADO un docente, CUANDO abre `/admin/estado`, ENTONCES ve «Acceso no permitido» (403 para DOCENTE) | [CA-4_docente-sin-acceso-a-estado.png](CA-4_docente-sin-acceso-a-estado.png) | doc01 en `/admin/estado`: «Acceso no permitido» |

Pasos para repetir la prueba de reinicio: [README.md, «Prueba de reinicio»](../../../../README.md#prueba-de-reinicio).
