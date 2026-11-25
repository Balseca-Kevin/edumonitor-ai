import api from './api'

/** Pregunta con la que el Panel abre el asistente para armar el informe del curso (HU-16). */
export const PREGUNTA_INFORME = 'Resume el estado del curso: promedio, quiénes requieren atención, temas donde falla y qué viene.'

/**
 * HU-16 · Informe del curso en PDF: los indicadores y el resumen de una respuesta del asistente. 400 sin conversacionId, 404 si la
 * conversación no es del docente en el curso, 409 sin sincronizar.
 */
export async function descargarInforme(cursoId, conversacionId) {
  const respuesta = await api.get(`/api/cursos/${cursoId}/informe`, { params: { formato: 'pdf', conversacionId }, responseType: 'blob' })
  const nombre = /filename="?([^";]+)"?/.exec(respuesta.headers?.['content-disposition'] ?? '')?.[1] ?? `informe-${cursoId}-${conversacionId}.pdf`
  return { blob: respuesta.data, nombre }
}
