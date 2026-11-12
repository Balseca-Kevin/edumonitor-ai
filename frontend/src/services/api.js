
import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Adjuntar el token JWT a las solicitudes autenticadas.
api.interceptors.request.use(
  (config) => {
    const token = sessionStorage.getItem('edumonitor_token')

    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  },
  (error) => Promise.reject(error),
)

// HU-19 / TA-19.5
// Detectar una sesión vencida sin modificar los errores 403.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const estado = error.response?.status
    const url = error.config?.url || ''
    const token = sessionStorage.getItem('edumonitor_token')

    // El 401 del formulario de login indica credenciales
    // incorrectas, no una sesión que haya vencido.
    const esLogin = /\/api\/auth\/login(?:\?|$)/.test(url)

    if (estado === 401 && token && !esLogin) {
      window.dispatchEvent(
        new Event('edumonitor:sesion-vencida'),
      )
    }

    // Mantener el error original para los componentes.
    return Promise.reject(error)
  },
)

export default api
