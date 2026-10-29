# EduMonitor AI — Frontend

Interfaz web de EduMonitor AI desarrollada con React 18 y Vite.

Permite a los usuarios autenticarse, consultar sus cursos y ejecutar
sincronizaciones con Moodle a través del backend de EduMonitor AI.

## Tecnologías

- React 18
- Vite 6
- React Router 6
- Axios
- Vitest
- React Testing Library

## Requisitos

- Node.js compatible con Vite 6
- npm
- Backend de EduMonitor AI para las pruebas de integración

## Instalación

Desde la raíz del repositorio:

```powershell
cd frontend
npm install
```

## Variables de entorno

La aplicación utiliza las siguientes variables:

| Variable | Descripción |
|---|---|
| VITE_API_URL | Dirección base del backend |
| VITE_USE_MOCK | Activa los datos simulados cuando vale `true` |

Para trabajar con datos simulados, crear un archivo `.env.local`
dentro de `frontend`:

```env
VITE_USE_MOCK=true
VITE_API_URL=http://localhost:5000
```

Para utilizar el backend real:

```env
VITE_USE_MOCK=false
VITE_API_URL=http://localhost:5000
```

El archivo `.env.local` es de uso local y no debe contener secretos.

## Ejecutar la aplicación

```powershell
npm run dev
```

Abrir la dirección local indicada por Vite.

## Funcionalidades

### Inicio de sesión

- Formulario de usuario y contraseña.
- Validación de campos.
- Manejo de errores de autenticación.
- Almacenamiento del token JWT en `sessionStorage`.
- Redirección a Mis cursos después del inicio de sesión.

### Mis cursos

- Consulta de cursos asignados al usuario.
- Búsqueda por nombre.
- Filtrado por período.
- Visualización del estado de sincronización.
- Sincronización manual de cursos.
- Consulta del historial de sincronizaciones.
- Visualización de errores de sincronización.

## Servicios

El frontend consume los siguientes endpoints del backend:

| Método | Endpoint | Función |
|---|---|---|
| GET | `/api/cursos` | Obtener cursos |
| POST | `/api/cursos/{id}/sync` | Sincronizar curso |
| GET | `/api/cursos/{id}/sync` | Consultar historial |

La autenticación utiliza el contrato definido en
`docs/contratos-api.md` del repositorio.

El frontend no se conecta directamente a Moodle.

## Pruebas

Ejecutar todas las pruebas:

```powershell
npm test -- --run
```

Las pruebas incluyen:

- Validaciones del login.
- Manejo de errores de autenticación.
- Visualización de cursos.
- Sincronización de cursos.
- Manejo de errores HTTP.
- Historial de sincronizaciones.
- Navegación y protección de rutas.

## Compilación de producción

```powershell
npm run build
```

La compilación genera el directorio `dist`.

## Estructura principal

```text
frontend/
├── src/
│   ├── components/
│   │   └── Layout.jsx
│   ├── pages/
│   │   ├── Login.jsx
│   │   ├── Login.test.jsx
│   │   ├── Cursos.jsx
│   │   └── Cursos.test.jsx
│   ├── services/
│   │   ├── api.js
│   │   ├── auth.js
│   │   └── cursos.js
│   ├── App.jsx
│   └── App.test.jsx
├── package.json
└── README.md
```

## Consideraciones

- El token se adjunta a las solicitudes mediante Axios.
- Las rutas privadas requieren una sesión.
- El backend es responsable de validar la autenticación y autorización.
- Los datos simulados se utilizan únicamente para desarrollo y pruebas.
- No incluir credenciales reales ni tokens en el repositorio.