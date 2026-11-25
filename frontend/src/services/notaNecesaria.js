import api from './api'

/**
 * TA-10.A1 · Nota necesaria para llegar a 7,0 con las notas actuales (docs/contratos-api.md, «Nota necesaria»). La calcula el
 * backend; la pantalla solo la muestra. 404 si el estudiante no está en el curso, 409 sin sincronizar.
 */
export async function obtenerNotaNecesaria(cursoId, estudianteId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/estudiantes/${estudianteId}/nota-necesaria`)
  return data
}
