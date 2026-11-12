# HU-09 · Panel de indicadores del curso

Capturas de la aplicación en Docker (`http://localhost:5173`) del 11 nov 2026, entre las 21:45 y las 21:50, con doc01 en AGIL-4B. La fecha de la última sincronización (30/10/2026, 02:25 p. m.) se ve en la cabecera del curso.

Los criterios salen de la planificación del Sprint 1 (RF-07, RNF-03, RNF-08 y Tabla 9) y del plan del Sprint 2. La redacción oficial en Jira queda por confirmar.

| Criterio | Captura | Dato observado |
|---|---|---|
| CA-1 · DADO un curso sincronizado, CUANDO el docente abre el panel, ENTONCES ve el promedio con escala explícita, la mediana, la desviación estándar y la fecha de la última sincronización (RF-07, RNF-08) | [CA-1_promedio-mediana-desviacion-y-fecha.png](CA-1_promedio-mediana-desviacion-y-fecha.png) | Promedio 7,60 / 10, aprobación 7,0, 32 estudiantes, 12 actividades evaluadas; mediana 7,73; desviación 1,24; sincronización COMPLETADA del 30/10/2026 |
| CA-2 · ENTONCES ve la distribución en 10 tramos con la línea de aprobación 7,0 y los grupos alto, medio y bajo (RF-07) | [CA-2_distribucion-y-grupos.png](CA-2_distribucion-y-grupos.png) | Histograma con la línea en 7,0; grupos 8 alto, 22 medio y 2 bajo; finalización 73 % |
| CA-3 · ENTONCES la tabla distingue nota cero, sin entrega y pendiente de calificar (Tabla 9, criterio 3) | [CA-3_cero-sin-entrega-y-pendiente.png](CA-3_cero-sin-entrega-y-pendiente.png) | Elena Espinoza (est05): 8,61, 1 cero; Gabriela Guerrero (est07): 6,68, 7 sin entrega; Isabel Iza (est09): 7,35, 2 pendientes |
| CA-4 · DADO un dato que no está disponible, ENTONCES se muestra «no disponible», nunca 0 (RNF-03) | [CA-4_dato-no-disponible-nunca-cero.png](CA-4_dato-no-disponible-nunca-cero.png) | «Requiere atención: no disponible», porque las alertas de HU-12 no están en `develop` |
| CA-5 · DADO un curso de otro docente, CUANDO doc01 abre su panel, ENTONCES ve «Acceso no permitido» (RF-01, Tabla 9 criterio 7) | [CA-5_curso-ajeno-acceso-no-permitido.png](CA-5_curso-ajeno-acceso-no-permitido.png) | `/cursos/2/panel` (BDD-4B): «El curso 2 no está asignado a este docente.» |

Sin captura real: el aviso «Curso sin sincronizar» (409). Los dos cursos del laboratorio ya están sincronizados; lo cubren `frontend/src/pages/curso/Panel.test.jsx` y `backend/EduMonitor.Tests/Api/IndicadoresApiTests.cs`.
