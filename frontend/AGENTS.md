# Frontend EduMonitor · reglas de la interfaz

React 18 + Vite, React Router, Axios, Recharts, Vitest + Testing Library. La interfaz solo habla con la API. En desarrollo sin Docker usa la URL de `VITE_API_URL`. En Docker se construye con `VITE_API_URL` vacío, llama a `/api` en la misma dirección (`http://localhost:5173`) y nginx lo pasa a la API. Nunca llama a Moodle ni al agente.

## Diseño

- `docs/sprint-2/diseno.md` manda. Todo color, tamaño de letra, espacio y radio sale de `src/styles/tokens.css`.
- Componentes base en `src/components/ui/` (Boton, Campo, Tarjeta, Tabla, InsigniaEstado, Aviso). Las páginas los usan; no crean estilos propios para lo mismo. Al cierre del Sprint 2 esa carpeta no está en `develop` (HU-19): el panel, el asistente y «Estado del sistema» usan `src/components/s2/` y el bloque `.s2` de `tokens.css`.
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
| `/acceso-no-permitido` | `pages/AccesoNoPermitido.jsx` | HU-19 |
| `/sesion-vencida` | `pages/SesionVencida.jsx` | HU-19 |

Al cierre del Sprint 2 el router (`src/App.jsx`) tiene `/login`, `/cursos`, `/cursos/:id/panel`, `/cursos/:id/asistente`, `/admin/moodle` y `/admin/estado`. `/cursos/:id/alertas` (HU-12), `/acceso-no-permitido` y `/sesion-vencida` (HU-19) no están en `develop`.

Un servicio por recurso en `src/services/` (`indicadores.js`, `alertas.js`, `agente.js`, `estado.js`), con las rutas y JSON de `docs/contratos-api.md`.

## Reglas

- Cada pantalla resuelve cuatro estados: cargando, vacío, error y «no disponible». Un dato en `noDisponibles` nunca se muestra como 0.
- El interceptor de Axios manda 401 a `/sesion-vencida` y 403 de un curso a `/acceso-no-permitido` (HU-19, no está en `develop`). Mientras tanto, cada pantalla del Sprint 2 muestra «Acceso no permitido» o «Tu sesión venció» en su misma ruta con `components/s2/PaginaError.jsx`.
- Textos en español. Alertas siempre como «Requiere atención».
- Cada página nueva trae su prueba en Vitest con datos simulados del contrato.
- Comandos: `npm run dev`, `npm test -- --run`, `npm run build`, `npm run lint`.
