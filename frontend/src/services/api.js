
import axios from 'axios'

const api = axios.create({
  // Vacío (Docker y desarrollo): la app llama a /api en su misma dirección; nginx o el proxy de Vite la pasan a la API.
  baseURL: import.meta.env.VITE_API_URL ?? '',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Adjunta el JWT automáticamente a las peticiones autenticadas.
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

export default api
