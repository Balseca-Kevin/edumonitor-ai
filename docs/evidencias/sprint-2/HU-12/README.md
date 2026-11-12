# HU-12 · Alertas académicas con reglas configurables

Responsable: Sebastián Freire (PR #38, TA-12.1 a TA-12.5).
- Kevin revisó y corrigió el PR antes de fusionarlo (commit 4afe640):
  - un curso sincronizado antes de HU-12 no tenía reglas y mostraba 0 alertas;
  - una alerta revisada volvía a activas al recalcular con la misma evidencia.
- La pantalla usa el diseño D desde #35: sin códigos de regla y con la evidencia en una frase.

Capturas de la aplicación en Docker (`http://localhost:5173`) del 12 nov 2026, 08:23:
- doc01 en AGIL-4B, con datos reales del aula virtual sincronizados a las 07:07.
- Úrsula Vaca se marcó como revisada a las 07:12, en la prueba de la revisión.

| Criterio | Captura | Qué se ve |
|---|---|---|
| CA-1 · DADO un curso sincronizado, CUANDO el docente abre el panel, ENTONCES «Requiere atención» cuenta las alertas activas y lleva a ellas | [CA-1](CA-1_requiere-atencion-en-el-panel.png) | «Requiere atención 16 · alertas activas · ver alertas →» |
| CA-2 · Cada alerta muestra «Requiere atención», la regla y su evidencia con números, agrupadas por estudiante | [CA-2](CA-2_alertas-con-evidencia.png) | 16 alertas activas para 14 estudiantes: 8 «Promedio bajo», 6 «Atrasos» y 2 «Descenso reciente».<br>Ejemplo: Olga Paredes, «Promedio 6,97 con 5 notas registradas; el mínimo es 7,0» y «8 actividades vencidas sin entregar: Tarea 3 (29/09) · …». |
| CA-3 · DADO un umbral nuevo, CUANDO el docente guarda, ENTONCES las alertas se recalculan | [CA-3](CA-3_umbral-promedio-bajo-en-5.png) | Con «Promedio bajo» en 5 quedan 9 activas.<br>Al volver a 7 vuelven a ser 16. |
| CA-4 · Una alerta marcada como revisada sale de las activas y queda en «Revisadas» | [CA-4](CA-4_alertas-revisadas.png) | Úrsula Vaca, «Revisada», «Promedio bajo», «Promedio 6,54 con 12 notas registradas», revisada el 12/11/2026 a las 07:12.<br>No vuelve a activas al recalcular. |

Pruebas:
- Backend: pruebas de Sebastián de cada regla y de la API, más 2 de la corrección de Kevin:
  - `Un_curso_sin_reglas_las_recibe_por_defecto_al_sincronizar_y_al_consultar_alertas`;
  - `Una_alerta_revisada_no_vuelve_a_activas_al_recalcular_si_la_evidencia_no_cambia`.
- Frontend: 7 pruebas de la pantalla en `Alertas.test.jsx`.
