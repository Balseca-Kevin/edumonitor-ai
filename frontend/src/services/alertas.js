import api from './api'

/**
 * HU-12 · Reglas configurables y alertas del curso (docs/contratos-api.md).
 * Las alertas las calcula el backend con código determinista; aquí solo se leen y se marcan como revisadas.
 */

/** Reglas del curso (R1, R2, R4); la primera consulta crea las tres con sus umbrales por defecto. */
export async function obtenerReglas(cursoId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/reglas`)
  return data
}

/** Guarda los umbrales cambiados (204) y el backend recalcula las alertas. 400 si un umbral sale de rango. */
export async function actualizarReglas(cursoId, cambios) {
  await api.put(`/api/cursos/${cursoId}/reglas`, cambios)
}

/** Alertas del curso en un estado: ACTIVA (por defecto) o REVISADA. Lista vacía si todavía no hay datos. */
export async function obtenerAlertas(cursoId, estado = 'ACTIVA') {
  const { data } = await api.get(`/api/cursos/${cursoId}/alertas`, { params: { estado } })
  return data
}

/** Marca una alerta como REVISADA con su fecha; sale de las activas. */
export async function marcarAlerta(cursoId, alertaId, estado = 'REVISADA') {
  const { data } = await api.patch(`/api/cursos/${cursoId}/alertas/${alertaId}`, { estado })
  return data
}
