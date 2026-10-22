
import api from './api'

const USE_MOCK = import.meta.env.VITE_USE_MOCK === 'true'

/**
 * Inicia sesión utilizando el contrato de EduMonitor AI.
 * En modo simulado, permite desarrollar sin el backend.
 */
export async function login(usuario, contrasena) {
  if (USE_MOCK) {
    // Credenciales ficticias únicamente para desarrollo local.
    if (usuario !== 'doc01' || contrasena !== 'valor-de-prueba') {
      throw new Error('Credenciales incorrectas')
    }

    return {
      token: 'token-simulado-desarrollo',
      expiraEn: '2099-12-31T23:59:59-05:00',
      usuario: {
        id: 2,
        usuario: 'doc01',
        nombre: 'Docente Uno',
        rol: 'DOCENTE',
      },
    }
  }

  const response = await api.post('/api/auth/login', {
    usuario,
    contrasena,
  })

  return response.data
}

export function guardarSesion(sesion) {
  sessionStorage.setItem('edumonitor_token', sesion.token)
  sessionStorage.setItem(
    'edumonitor_usuario',
    JSON.stringify(sesion.usuario),
  )
}

export function cerrarSesion() {
  sessionStorage.removeItem('edumonitor_token')
  sessionStorage.removeItem('edumonitor_usuario')
}
