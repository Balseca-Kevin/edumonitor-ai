# EduMonitor AI — Frontend

Interfaz web de EduMonitor AI con React 18 y Vite. Incluye:

- inicio de sesión y Mis cursos, con su sincronización con Moodle;
- el panel de indicadores y el asistente de cada curso;
- para el administrador, la configuración de Moodle y el estado del sistema.

La interfaz solo habla con la API de EduMonitor. Nunca llama a Moodle ni al agente de IA.

## Tecnologías

- React 18 y React Router 6
- Vite 6
- Axios
- Recharts 2 (gráficos del panel)
- Vitest 5 y React Testing Library
- oxlint
- Tipografía Archivo (`@fontsource-variable/archivo`)

## Ejecutar la aplicación

### Con Docker (como en la Review)

Desde la raíz del repositorio, con el aula virtual encendida (ver el [README de la raíz](../README.md#puesta-en-marcha)):

```powershell
docker compose up -d --wait
```

La aplicación queda en <http://localhost:5173>. La imagen (`frontend/Dockerfile`) se construye con `VITE_API_URL` vacío y `VITE_USE_MOCK=false`. Así la app llama a `/api` en la misma dirección y nginx (`frontend/nginx.conf`) lo pasa a la API.

### Sin Docker (desarrollo)

Requiere Node.js 22 (la versión de la imagen) y la API en `http://localhost:5000`:

```powershell
cd frontend
npm ci
npm run dev
```

`vite.config.js` pasa `/api` a `http://localhost:5000`. Si el contenedor `web` ya ocupa el 5173, Vite usa el siguiente puerto libre.

| Variable | Uso |
|---|---|
| `VITE_API_URL` | Dirección base de la API. Vacía, la app llama a `/api` en su misma dirección |
| `VITE_USE_MOCK` | `true` usa los datos simulados de `auth.js`, `cursos.js` y `moodleConfig.js` |

Las variables locales van en `frontend/.env.local`, que no se versiona y no lleva secretos.

## Rutas

Rutas de `src/App.jsx` al cierre del Sprint 2:

| Ruta | Pantalla | Usuario | HU |
|---|---|---|---|
| `/login` | Inicio de sesión. El ADMIN entra a `/admin/moodle` y el docente a `/cursos` | todos | HU-06 |
| `/cursos` | Mis cursos: búsqueda, filtros, sincronización e historial; «Abrir» lleva al panel | DOCENTE y ADMIN | HU-06 |
| `/cursos/:id/panel` | Panel del curso: indicadores en bento grid (`/cursos/:id` redirige aquí) | DOCENTE y ADMIN | HU-09 |
| `/cursos/:id/asistente` | Asistente: pregunta, respuesta y herramientas usadas | DOCENTE y ADMIN | HU-14 |
| `/admin/moodle` | Configuración de Moodle: URL, token enmascarado y prueba de conexión | ADMIN | HU-02 |
| `/admin/estado` | Estado del sistema: api, sqlserver, moodle, agente y modelo | ADMIN | HU-20 |
| cualquier otra | Redirige a `/cursos` con sesión o a `/login` sin ella | — | — |

Un 403 muestra «Acceso no permitido» y un 401 «Tu sesión venció» en la misma ruta (`components/s2/PaginaError.jsx`). Las rutas propias `/acceso-no-permitido` y `/sesion-vencida` y la pantalla de Alertas son de HU-19 y HU-12 y no están en `develop`.

## Servicios

| Archivo | Endpoint de la API |
|---|---|
| `services/auth.js` | `POST /api/auth/login` |
| `services/cursos.js` | `GET /api/cursos`, `GET /api/cursos/{id}`, `POST` y `GET /api/cursos/{id}/sync` |
| `services/indicadores.js` | `GET /api/cursos/{id}/indicadores`; `GET /api/cursos/{id}/alertas?estado=ACTIVA` para la tarjeta «Requiere atención», que dice «no disponible» mientras ese endpoint no exista |
| `services/agente.js` | `POST /api/agente/chat` |
| `services/moodleConfig.js` | `GET` y `PUT /api/admin/moodle-config`, `POST /api/admin/moodle-config/test` |
| `services/estado.js` | `GET /api/admin/estado` |
| `services/errores.js` | Traduce los `ProblemDetails` (401, 403, 409, 503) a mensajes de pantalla |

Las rutas y los JSON están en [docs/contratos-api.md](../docs/contratos-api.md).

## Pruebas

```powershell
npm test -- --run
npm run lint
```

Archivos de prueba:

- `App.test.jsx`: rutas protegidas.
- `pages/Login.test.jsx`.
- `pages/Cursos.test.jsx`: búsqueda, sincronización e historial.
- `pages/ConfiguracionMoodle.test.jsx`.
- `pages/curso/Panel.test.jsx`: indicadores, «no disponible» y 409.
- `pages/curso/Asistente.test.jsx`: respuesta con herramientas, «Consultando…», 503, 403 y 401.
- `pages/admin/EstadoSistema.test.jsx`.

El total de pruebas al día está en [docs/PRUEBAS.md](../docs/PRUEBAS.md#pruebas-automatizadas).

## Compilación de producción

```powershell
npm run build
```

Genera `dist/`, que la imagen de Docker sirve con nginx.

## Estructura principal

```text
frontend/
├── Dockerfile, nginx.conf        imagen: build de Vite + nginx con /api → api:5000
├── vite.config.js                proxy /api → localhost:5000 en desarrollo
└── src/
    ├── App.jsx                   rutas
    ├── components/
    │   ├── Layout.jsx            exige sesión
    │   └── s2/                   marco del Sprint 2 (barra lateral), estado, error y formato
    ├── pages/
    │   ├── Login.jsx, Cursos.jsx, ConfiguracionMoodle.jsx
    │   ├── curso/                CursoMarco.jsx, Panel.jsx, Asistente.jsx
    │   └── admin/                EstadoSistema.jsx
    ├── services/                 api.js (Axios con el JWT) y un servicio por recurso
    └── styles/                   tokens.css (bloque .s2 del Sprint 2) y sprint2.css
```
