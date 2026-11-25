import api from './api'

/**
 * HU-10 · Ficha del estudiante: notas y estados por actividad (los mismos del libro), criterios y comentario del docente, evolución
 * frente a 7,0, temas débiles y alertas activas. 404 si no está matriculado, 409 sin sincronizar.
 */
export async function obtenerFicha(cursoId, estudianteId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/estudiantes/${estudianteId}`)
  return data
}
