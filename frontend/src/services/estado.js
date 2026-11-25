import api from './api'

/** HU-20 · Estado de api, sqlserver, moodle, agente y modelo (solo ADMIN). Cada servicio se consulta con un máximo de 3 s. */
export async function obtenerEstadoSistema() {
  const { data } = await api.get('/api/admin/estado')
  return data
}

/** HU-23 · Los dos modelos del asistente, cuál responde ahora y el interruptor «Usar solo el modelo local» (solo ADMIN). */
export async function obtenerModelo() {
  const { data } = await api.get('/api/admin/modelo')
  return data
}

/** HU-23 · Activa o desactiva «Usar solo el modelo local». Devuelve el estado nuevo. */
export async function cambiarModelo(soloLocal) {
  const { data } = await api.put('/api/admin/modelo', { soloLocal })
  return data
}
