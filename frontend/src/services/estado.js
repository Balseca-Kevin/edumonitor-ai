import api from './api'

/** HU-20 · Estado de api, sqlserver, moodle, agente y modelo (solo ADMIN). Cada servicio se consulta con un máximo de 3 s. */
export async function obtenerEstadoSistema() {
  const { data } = await api.get('/api/admin/estado')
  return data
}
