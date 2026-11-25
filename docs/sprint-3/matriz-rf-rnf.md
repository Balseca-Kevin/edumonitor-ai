# Matriz de requisitos · cierre del Sprint 3

Cada requisito de la planificación (tablas 7 y 8 de `docs/planificacion/Planificacion_Proyecto_Scrum_Sprint1.pdf`) con la historia que lo cumple, la pantalla donde se ve y las pruebas automatizadas que lo comprueban. Las rutas de pantalla son de `http://localhost:5173`.

Los archivos de prueba se nombran con su ubicación corta:
- `backend/…`: `backend/EduMonitor.Tests/…`;
- `frontend/…`: `frontend/src/…`;
- `agent/…`: `agent/tests/…`.

## Requisitos funcionales

| ID | Requisito | HU | Pantalla | Pruebas automatizadas |
|---|---|---|---|---|
| RF-01 | Autenticación y autorización | HU-04, HU-06 | Inicio de sesión; «Acceso no permitido» y «Tu sesión venció» | `backend/Api/PermisosTests.cs`, `backend/Auth/AuthServiceTests.cs`, `backend/Seguridad/JwtServiceTests.cs`, `frontend/pages/Login.test.jsx` |
| RF-02 | Conexión con Moodle | HU-02, HU-20 | Configuración de Moodle; Estado del sistema | `backend/Api/ConfiguracionMoodleApiTests.cs`, `backend/Moodle/MoodleClientTests.cs`, `frontend/pages/ConfiguracionMoodle.test.jsx` |
| RF-03 | Sincronización | HU-05, HU-21 (TA-21.A1) | Mis cursos → «Sincronizar y abrir»; aviso con los cambios | `backend/Api/SincronizacionApiTests.cs`, `backend/Api/CambiosSincronizacionApiTests.cs`, `backend/Sincronizaciones/*`, `frontend/components/AvisoSincronizacion.test.jsx` |
| RF-04 | Gestión de cursos | HU-06, HU-10 | Mis cursos; Ficha del estudiante | `backend/Cursos/CursoServiceTests.cs`, `frontend/pages/Cursos.test.jsx`, `frontend/pages/curso/FichaEstudiante.test.jsx` |
| RF-05 | Panel de indicadores | HU-09, HU-21 | Panel del curso; Libro de calificaciones | `backend/Indicadores/CalculadoraIndicadoresTests.cs`, `backend/Api/IndicadoresApiTests.cs`, `backend/Api/LibroApiTests.cs`, `frontend/pages/curso/Panel.test.jsx`, `frontend/pages/curso/Libro.test.jsx` |
| RF-06 | Seguimiento individual | HU-10, HU-21 | Ficha del estudiante (evolución frente a 7,0); ficha lateral | `backend/Api/AuditoriaFichaApiTests.cs` (la ficha coincide con el libro), `backend/NotaNecesaria/CalculadoraNotaNecesariaTests.cs`, `frontend/pages/curso/FichaEstudiante.test.jsx` |
| RF-07 | Alertas académicas | HU-12 | Alertas; modal «Umbrales de alerta» | `backend/Alertas/ReglasAlertaTests.cs`, `backend/Api/AlertasApiTests.cs`, `frontend/pages/curso/Alertas.test.jsx` |
| RF-08 | Análisis grupal | HU-22 (absorbe HU-11) | Mapa del curso con mapa de calor; Panel de actividad | `backend/Api/ActividadRetroalimentacionApiTests.cs`, `backend/Api/TematicasApiTests.cs`, `frontend/pages/curso/Mapa.test.jsx`, `frontend/pages/curso/Actividad.test.jsx` |
| RF-09 | Agente de IA | HU-13, HU-14, HU-23 | Asistente («Cómo llegó a esta respuesta», cifras enlazadas) | `backend/Api/AgenteApiTests.cs`, `backend/Api/AsistenteHu23ApiTests.cs`, `agent/test_hu23.py`, `agent/test_herramientas_hu13.py`, `frontend/pages/curso/Asistente.test.jsx` |
| RF-10 | Recomendaciones | HU-15 | Plan de refuerzo; retroalimentación desde el Panel de actividad | `backend/Api/RefuerzosApiTests.cs`, `backend/Api/ActividadRetroalimentacionApiTests.cs`, `frontend/pages/curso/PlanRefuerzo.test.jsx`, `frontend/pages/curso/AsistentePlan.test.jsx` |
| RF-11 | Informe | HU-16 | Panel → «Informe del curso» → asistente → «Descargar como informe del curso (PDF)» | `backend/Api/InformeApiTests.cs`, `frontend/pages/curso/Panel.test.jsx`, `frontend/pages/curso/Asistente.test.jsx` |
| RF-12 | Auditoría | HU-13, HU-17 | Auditoría (ADMIN) | `backend/Api/AuditoriaFichaApiTests.cs`, `backend/Api/TokenMoodleEnLogsTests.cs`, `frontend/pages/admin/Auditoria.test.jsx` |
| RF-13 | Mapeo temático | HU-08 | Mapa del curso | `backend/Tematicas/CalculadoraTematicaTests.cs`, `backend/Api/TematicasApiTests.cs`, `backend/Sincronizaciones/TemasSilaboTests.cs`, `frontend/pages/curso/Mapa.test.jsx` |
| RF-14 | Desempeño por temática y dispersión | HU-09, HU-22 | Panel; Mapa del curso; Panel de actividad (dominio por criterio) | `backend/Indicadores/CalculadoraIndicadoresTests.cs`, `backend/Tematicas/CalculadoraTematicaTests.cs`, `backend/Api/ActividadRetroalimentacionApiTests.cs` |
| RF-15 | Tareas de refuerzo | HU-15 | Plan de refuerzo → asistente → «Aprobar borrador» → PDF y DOCX; «Refuerzos aplicados» | `backend/Api/RefuerzosApiTests.cs`, `frontend/pages/curso/PlanRefuerzo.test.jsx` |
| RF-16 | Moodle de laboratorio | HU-01, TA-08.A1, TA-22.A3 | Aula virtual en `http://localhost:8080` | `backend/Moodle/MoodleClientIntegracionTests.cs`, `backend/Sincronizaciones/SincronizacionMoodleRealTests.cs` (necesitan el aula virtual encendida) |

## Requisitos no funcionales

| ID | Categoría | Cómo se cumple | Dónde se comprueba |
|---|---|---|---|
| RNF-01 | Seguridad | JWT con rol; política `CursoAutorizado` (401, 403); token de servicio del agente de 2 minutos; secretos solo en `.env`; token de Moodle enmascarado; auditoría sin secretos | `backend/Api/PermisosTests.cs`, `backend/Api/TokenAgenteApiTests.cs`, `backend/Seguridad/EnmascaradorSecretosTests.cs`, `backend/Api/AuditoriaFichaApiTests.cs` |
| RNF-02 | Privacidad | Datos mínimos de estudiantes; con el modelo externo, nombres cambiados por E-001… y sin ids de Moodle (seudonimización en la API); respaldo local con Ollama | `backend/Api/AsistenteHu23ApiTests.cs` (las herramientas dan códigos al agente y nombres al docente) |
| RNF-03 | Confiabilidad | Sincronización `PARCIAL` y «no disponible», nunca 0; respaldo local si el modelo externo falla (20 s, 429, formato); 3 fallos en 5 minutos → local por 5 minutos; «El asistente no está disponible» si fallan los dos | `backend/Api/IndicadoresApiTests.cs`, `agent/test_hu23.py` (respaldo, tiempo agotado, pausa), `backend/Api/AgenteApiTests.cs` (503) |
| RNF-04 | Usabilidad | Diseño del Sprint 3 (`docs/sprint-3/diseno.md`); lenguaje docente sin términos estadísticos; «Requiere atención» | Pruebas de interfaz de `frontend/` |
| RNF-05 | Mantenibilidad | Monolito modular por capas; README que se sigue desde la carpeta; contratos en `docs/contratos-api.md` | Revisión del README y de `docs/` |
| RNF-06 | Calidad | Pruebas unitarias de los cálculos (indicadores, tema, nota necesaria, período, actividad, ficha, evaluación), de API y de permisos; integración continua en GitHub | `dotnet test`, `npm test -- --run`, `python -m pytest` (resultados en `docs/PRUEBAS.md`) |
| RNF-07 | Rendimiento | Herramientas del agente con datos resumidos y filtros en el backend; evaluación en segundo plano; tiempos medidos en «Calidad del asistente» | Pantalla «Calidad del asistente» (tiempo promedio por modelo) |
| RNF-08 | Transparencia | Fuente y fecha de sincronización; evidencia de cada alerta; «Cómo llegó a esta respuesta» con modelo, herramientas, parámetros y duración; cifras verificadas enlazadas al libro; preguntas fuera de alcance respondidas sin modelo | `agent/test_hu23.py` (verificación de cifras y fuera de alcance), `frontend/pages/curso/Asistente.test.jsx`, `backend/Api/EvaluacionApiTests.cs` |

## Criterios de aceptación de la demostración (tabla 9)

| # | Criterio | Dónde se ve |
|---|---|---|
| 1 | El docente inicia sesión y solo ve sus cursos | Inicio de sesión → Mis cursos |
| 2 | La sincronización muestra origen y fecha; las calificaciones coinciden con Moodle | Aviso de sincronización; Libro de calificaciones con Moodle abierto al lado |
| 3 | Se distinguen nota cero, sin entrega y pendiente | Libro, Ficha del estudiante y Panel de actividad |
| 4 | Una regla de alerta se activa y se explica con evidencia | Alertas |
| 5 | El agente responde con herramientas reales y no inventa datos si falla | Asistente; «Calidad del asistente» |
| 6 | Recomendaciones relacionadas con los hallazgos | Plan de refuerzo y retroalimentación con criterios fallados y notas reales |
| 7 | Un usuario no autorizado no accede a otro curso | URL de un curso ajeno → «Acceso no permitido»; queda en Auditoría |
| 8 | Se instala siguiendo el README y tiene pruebas ejecutables | README; `docs/PRUEBAS.md` |
