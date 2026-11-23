import api from './api'

/**
 * TA-22.A4 · Estado del período del curso, calculado en el backend con la fecha del servidor (docs/contratos-api.md, «Estado del
 * período»): semana, unidades, actividades con su estado y los bloques vencidas, vence esta semana y siguiente. El frontend no
 * calcula fechas. 403 curso ajeno, 404 no existe, 409 sin sincronizar.
 */
export async function obtenerPeriodo(cursoId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/periodo`)
  return data
}
