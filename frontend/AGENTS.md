# Frontend EduMonitor · reglas de la interfaz

React 18 + Vite, React Router, Axios, Recharts, Vitest + Testing Library. La interfaz solo habla con la API. En desarrollo sin Docker usa la URL de `VITE_API_URL`. En Docker se construye con `VITE_API_URL` vacío, llama a `/api` en la misma dirección (`http://localhost:5173`) y nginx lo pasa a la API. Nunca llama a Moodle ni al agente.

## Diseño

- `docs/sprint-3/diseno.md` manda, con el prototipo `docs/sprint-3/prototipo/EduMonitor_Sprint3.dc.html` como referencia (sus datos son ficticios; no se copian). Lo que no cambia sigue en `docs/sprint-2/diseno.md` (diseño D). Todo color, tamaño de letra, espacio, radio y tiempo sale de `src/styles/tokens.css`; las clases de las pantallas están en `src/styles/app.css`.
- Componentes base de Santiago en `src/components/ui/` (Button, Input, Card, Table, StatusBadge, Alert). Estructura en `src/components/Marco.jsx`, estados en `Estado.jsx`, páginas de 401 y 403 en `PaginaError.jsx`, barra de contexto del curso, panel «Período» y línea de tiempo (`LineaTiempo`, también en el Panel) en `CabeceraCurso.jsx`.
- Textos en lenguaje docente en `src/components/lenguaje.js`: en pantalla no van términos estadísticos (z, desviación, tendencia), códigos de regla (R1, R2, R4) ni nombres de funciones del agente.
- Movimiento una sola vez y en menos de 3 s, quieto con `prefers-reduced-motion` (`src/components/movimiento.js`).
- Gráficos con Recharts, colores tomados de los tokens.

## Estructura

| Ruta | Página | HU |
|---|---|---|
| `/login` | `pages/Login.jsx` | HU-19 |
| `/cursos` | `pages/Cursos.jsx` | HU-19 |
| `/cursos/:id/panel` | `pages/curso/Panel.jsx` | HU-09 |
| `/cursos/:id/alertas` | `pages/curso/Alertas.jsx` | HU-12 |
| `/cursos/:id/asistente` | `pages/curso/Asistente.jsx` | HU-14 |
| `/admin/moodle` | `pages/ConfiguracionMoodle.jsx` | HU-02 |
| `/admin/estado` | `pages/admin/EstadoSistema.jsx` | HU-20 |

Pantallas del Sprint 3 (rutas propuestas; se confirman con el contrato de cada HU):

| Ruta | Página | HU |
|---|---|---|
| `/cursos/:id/libro` | `pages/curso/Libro.jsx` | HU-21 |
| `/cursos/:id/estudiantes/:eid` | `pages/curso/FichaEstudiante.jsx` | HU-10 |
| `/cursos/:id/mapa` | `pages/curso/Mapa.jsx` | HU-22 |
| `/cursos/:id/actividades/:aid` | `pages/curso/Actividad.jsx` | HU-22 |
| `/cursos/:id/refuerzos` | `pages/curso/PlanRefuerzo.jsx` (plan, envío al asistente e historial) | HU-15 (TA-15.A3, TA-15.A4) |
| `/cursos/:id/informe` | `pages/curso/Informe.jsx` | HU-16 |
| `/admin/auditoria` | `pages/admin/Auditoria.jsx` | HU-17 |
| `/admin/calidad-asistente` | `pages/admin/CalidadAsistente.jsx` | HU-18 (TA-18.A2) |

Reglas nuevas del Sprint 3:

- Barra lateral oscura del curso en este orden: Libro de calificaciones, Panel del curso, Mapa del curso, Alertas, Plan de refuerzo, Asistente. El libro es la primera pantalla al abrir un curso.
- Ninguna pantalla muestra datos fijos ni calcula fechas: semana, unidad actual, vencidas y siguiente vienen de `GET /api/cursos/{id}/periodo`; las celdas nuevas, de `GET /api/cursos/{id}/sync/{sid}/cambios`.
- Se mantienen las animaciones del diseño D y las del prototipo (pantalla de carga, aviso de sincronización, línea de tiempo, ficha lateral), quietas con `prefers-reduced-motion`.
- No se escriben en pantalla las reglas del sistema («Nada se envía…», códigos E-001). «Respondió: modelo …», herramientas, duración y cifras verificadas van en el desplegable cerrado «Cómo llegó a esta respuesta».
- Toda cifra (promedio, alerta, desempeño por tema, nota necesaria) se puede abrir para ver de qué notas sale.
- Cada panel tiene el botón «Analizar con el asistente», que abre el chat con el curso, la actividad o el estudiante cargados (por id).
- Cero, sin entrega y pendiente se ven igual en todas las pantallas.
- Cada respuesta del asistente tiene «Respondió: modelo externo» o «Respondió: modelo local (respaldo)» dentro de «Cómo llegó a esta respuesta», y sus cifras verificadas son enlaces.
- La nota necesaria lleva la leyenda «cálculo con las notas actuales». El diagnóstico por criterio dice «revisar», sin la palabra correlación.

El router (`src/App.jsx`) tiene `/login`, `/cursos`, `/cursos/:id/libro` (primera pantalla del curso), `/cursos/:id/panel`, `/cursos/:id/mapa` (mapeo temático, desempeño por tema y mapa de calor), `/cursos/:id/actividades/:aid` (panel de actividad), `/cursos/:id/estudiantes/:eid` (ficha del estudiante), `/cursos/:id/refuerzos` (plan de refuerzo, refuerzos aplicados e historial), `/cursos/:id/alertas`, `/cursos/:id/asistente`, `/admin/moodle`, `/admin/estado`, `/admin/auditoria` y `/admin/calidad-asistente`. El informe del curso (HU-16) se pide desde el Panel y se descarga desde el asistente; no tiene ruta propia. «Acceso no permitido» y «Tu sesión venció» se muestran en la misma ruta con `components/PaginaError.jsx`, sin rutas propias.

Un servicio por recurso en `src/services/` (`indicadores.js`, `alertas.js`, `agente.js`, `estado.js`), con las rutas y JSON de `docs/contratos-api.md`.

## Reglas

- Cada pantalla resuelve cuatro estados: cargando, vacío, error y «no disponible». Un dato en `noDisponibles` nunca se muestra como 0.
- El interceptor de Axios avisa un 401 con el evento `edumonitor:sesion-vencida` y `Layout.jsx` muestra «Tu sesión venció». Un 403 de un curso se ve como «Acceso no permitido» en pantalla completa (`CursoMarco.jsx`).
- Textos en español. Alertas siempre como «Requiere atención».
- Cada página nueva trae su prueba en Vitest con datos simulados del contrato.
- Comandos: `npm run dev`, `npm test -- --run`, `npm run build`, `npm run lint`.
