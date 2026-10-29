import api from './api'

const USE_MOCK = import.meta.env.VITE_USE_MOCK === 'true'

const FUNCIONES_PERMITIDAS = [
  'core_webservice_get_site_info',
  'core_enrol_get_users_courses',
  'core_course_get_contents',
  'core_enrol_get_enrolled_users',
  'gradereport_user_get_grade_items',
  'mod_assign_get_assignments',
  'mod_assign_get_submissions',
  'core_completion_get_activities_completion_status',
]

let configuracionSimulada = {
  url: 'http://localhost:8080',
  servicio: 'edumonitor_ws',
  tokenEnmascarado: '••••3f9a',
  actualizado: '2026-10-21T22:10:00-05:00',
}

/** GET /api/admin/moodle-config (solo ADMIN). El token llega enmascarado. */
export async function obtenerConfiguracion() {
  if (USE_MOCK) {
    return configuracionSimulada
  }

  const response = await api.get('/api/admin/moodle-config')
  return response.data
}

/** PUT /api/admin/moodle-config. Sin token, el backend conserva el actual. */
export async function guardarConfiguracion(url, token) {
  if (USE_MOCK) {
    configuracionSimulada = {
      ...configuracionSimulada,
      url,
      tokenEnmascarado: token ? `••••${token.slice(-4)}` : configuracionSimulada.tokenEnmascarado,
      actualizado: new Date().toISOString(),
    }
    return
  }

  await api.put('/api/admin/moodle-config', { url, token: token || null })
}

/** POST /api/admin/moodle-config/test: conexión y estado de cada función permitida. */
export async function probarConexion() {
  if (USE_MOCK) {
    return {
      conectado: true,
      sitio: 'Moodle-Lab-UTA',
      version: '4.5',
      usuarioTecnico: 'edumonitor_ws_user',
      funciones: FUNCIONES_PERMITIDAS.map((nombre) => ({
        nombre,
        disponible: nombre !== 'core_completion_get_activities_completion_status',
      })),
    }
  }

  const response = await api.post('/api/admin/moodle-config/test')
  return response.data
}
