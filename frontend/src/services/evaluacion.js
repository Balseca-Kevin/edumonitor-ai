import api from './api'

/**
 * HU-18 · TA-18.A1 · Evaluación del asistente con 18 preguntas y los dos modelos (solo ADMIN). POST responde 202 y corre en segundo
 * plano; 409 si ya hay una en curso.
 */
export async function iniciarEvaluacion(cursoId) {
  const { data } = await api.post('/api/admin/evaluacion-agente', { cursoId: Number(cursoId) })
  return data
}

export async function listarEvaluaciones() {
  const { data } = await api.get('/api/admin/evaluacion-agente')
  return Array.isArray(data) ? data : []
}

export async function obtenerEvaluacion(id) {
  const { data } = await api.get(`/api/admin/evaluacion-agente/${id}`)
  return data
}
