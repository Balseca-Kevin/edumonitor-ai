# Registro de contribuciones · Sprint 3

Quién hizo cada parte del Sprint 3, tomado de las columnas «Hizo» de [PLAN.md](PLAN.md). La regla del sprint es que cualquier integrante puede adelantar un bloque, y que el responsable de la HU tiene que poder explicarla en la Review aunque otro la haya hecho.

| HU | Responsable en el plan | Lo que se hizo | Hizo |
|---|---|---|---|
| HU-21 Libro de calificaciones | Santiago (pantalla); Kevin (endpoint) | Endpoint, pantalla, celdas nuevas de la sincronización, pantalla de carga y aviso | Kevin |
| HU-10 Ficha del estudiante | Santiago | Endpoint, evolución frente a 7,0, temas débiles, comentarios, nota necesaria y pantalla | Kevin |
| HU-08 Mapeo temático | Sebastián | Rúbricas en la sincronización, temas del sílabo, mapeo, desempeño por tema y «Mapa del curso» | Kevin |
| HU-22 Mapa del curso y panel de actividad | Kevin; Sebastián (cálculos) | Estado del período, barra del curso con el panel «Período», panel de actividad, cálculos y avisos por criterio y mapa de calor | Kevin |
| HU-15 Retroalimentación y refuerzo | Jeremy; Kevin (documentos) | Plan de refuerzo, retroalimentación por estudiante, borrador del asistente, aprobación, PDF y DOCX, antes y después, refuerzos aplicados, grupos por tema y estilo del docente | Kevin |
| HU-23 Asistente con contexto y respaldo | Kevin; Jeremy (nodos del grafo) | Filtros, memoria, foco, seudonimización, respaldo local, interruptor, verificación de cifras, fuera de alcance, tramo de la pantalla y preguntas generales | Kevin |
| HU-16 Informe del curso | Santiago | Informe en PDF con los indicadores, los refuerzos aplicados y el resumen del asistente | Kevin |
| HU-17 Trazas y auditoría | Kevin | «Cómo llegó a esta respuesta» y pantalla «Auditoría» | Kevin |
| HU-18 Pruebas, documentación y demostración | Todo el equipo | Evaluación del asistente y «Calidad del asistente», matriz RF/RNF, `docs/PRUEBAS.md` del Sprint 3 | Kevin. Quedan la corrida final de «Calidad del asistente» con el modelo local, el recorrido completo, las capturas, el informe, el video y el ensayo |

## Bloques del cierre

El cierre siguió `docs/sprint-3/GUIA_CIERRE.md`. Detalle en «Alcance final» de [PLAN.md](PLAN.md).

| Bloque | Qué se hizo | Hizo |
|---|---|---|
| B | Mapeo de AGIL-4B a los temas del sílabo, con el motivo de cada asociación ([mapeo-agil-4b.md](mapeo-agil-4b.md)) | Kevin, con Claude Code |
| C1 | Diagnóstico por criterio y sus avisos en el panel de actividad, con «Pedir enunciado corregido» (TA-22.A1, TA-22.A2) | Kevin, con Claude Code |
| C2 | Sección «Refuerzos aplicados» en el PDF del informe (TA-16.A1) | Kevin, con Claude Code |
| C3 y C4 | Grupos de refuerzo por tema y «Armar plan grupal» (TA-15.B1, TA-15.B2) | Kevin, con Claude Code |
| C5 | Estilo del docente en el borrador, «Con tu estilo» (TA-15.B3) | Kevin, con Claude Code |
| C6 | Barra de contexto compacta del curso con el panel «Período» | Kevin, con Claude Code |
| C7 | Tramo de la pantalla en el asistente y preguntas generales sin datos | Kevin, con Claude Code |
| C8 | Velocidad y lógica del asistente: no se aplicó, por decisión de Kevin | — |

Santiago, Sebastián y Jeremy no abrieron ramas ni Pull Request del Sprint 3 (Daily en [daily.md](daily.md)). Cada uno presenta en la Review la HU de la que es responsable, con esta tabla y los contratos de `docs/contratos-api.md`.
