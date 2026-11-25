import api from './api'

/**
 * HU-22 · Panel de actividad: enunciado y adjuntos de Moodle, entregas, distribución, dominio por criterio, temas evaluados y quién
 * requiere atención. 404 si la actividad no es del curso, 409 sin sincronizar.
 */
export async function obtenerActividad(cursoId, actividadId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/actividades/${actividadId}`)
  return data
}
