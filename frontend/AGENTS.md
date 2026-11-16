# Frontend EduMonitor · reglas de la interfaz

React 18 + Vite, React Router, Axios, Recharts, Vitest + Testing Library. La interfaz solo habla con la API. En desarrollo sin Docker usa la URL de `VITE_API_URL`. En Docker se construye con `VITE_API_URL` vacío, llama a `/api` en la misma dirección (`http://localhost:5173`) y nginx lo pasa a la API. Nunca llama a Moodle ni al agente.

## Diseño

- `docs/sprint-2/diseno.md` (diseño D, 12 nov 2026) manda. Todo color, tamaño de letra, espacio, radio y tiempo sale de `src/styles/tokens.css`; las clases de las pantallas están en `src/styles/app.css`.
- Componentes base de Santiago en `src/components/ui/` (Button, Input, Card, Table, StatusBadge, Alert). Estructura en `src/components/Marco.jsx`, estados en `Estado.jsx`, páginas de 401 y 403 en `PaginaError.jsx`, tira del período en `TiraPeriodo.jsx`.
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
| `/cursos/:id/refuerzos` | `pages/curso/Refuerzos.jsx` | HU-15 |
| `/cursos/:id/informe` | `pages/curso/Informe.jsx` | HU-16 |
| `/admin/auditoria` | `pages/admin/Auditoria.jsx` | HU-17 |
| `/admin/calidad-asistente` | `pages/admin/CalidadAsistente.jsx` | HU-18 (TA-18.A2) |

Reglas nuevas del Sprint 3:

- Barra lateral del curso en este orden: Libro, Panel, Mapa, Alertas, Asistente. El libro es la primera pantalla al abrir un curso.
- Toda cifra (promedio, alerta, desempeño por tema, nota necesaria) se puede abrir para ver de qué notas sale.
- Cada panel tiene el botón «Analizar con el asistente», que abre el chat con el curso, la actividad o el estudiante cargados (por id).
- Cero, sin entrega y pendiente se ven igual en todas las pantallas.
- Cada respuesta del asistente muestra «Respondió: modelo externo» o «Respondió: modelo local (respaldo)», y sus cifras verificadas son enlaces.
- La nota necesaria lleva la leyenda «cálculo con las notas actuales». El diagnóstico por criterio dice «revisar», sin la palabra correlación.

El router (`src/App.jsx`) tiene `/login`, `/cursos`, `/cursos/:id/panel`, `/cursos/:id/alertas`, `/cursos/:id/asistente`, `/admin/moodle` y `/admin/estado`. «Acceso no permitido» y «Tu sesión venció» se muestran en la misma ruta con `components/PaginaError.jsx`, sin rutas propias.

Un servicio por recurso en `src/services/` (`indicadores.js`, `alertas.js`, `agente.js`, `estado.js`), con las rutas y JSON de `docs/contratos-api.md`.

## Reglas

- Cada pantalla resuelve cuatro estados: cargando, vacío, error y «no disponible». Un dato en `noDisponibles` nunca se muestra como 0.
- El interceptor de Axios avisa un 401 con el evento `edumonitor:sesion-vencida` y `Layout.jsx` muestra «Tu sesión venció». Un 403 de un curso se ve como «Acceso no permitido» en pantalla completa (`CursoMarco.jsx`).
- Textos en español. Alertas siempre como «Requiere atención».
- Cada página nueva trae su prueba en Vitest con datos simulados del contrato.
- Comandos: `npm run dev`, `npm test -- --run`, `npm run build`, `npm run lint`.
