import api from './api'

/**
 * HU-14 · Pregunta al asistente de un curso. La API valida el curso y llama al agente;
 * el navegador nunca habla con el agente. Respuestas: 200, 400, 403, 404 y 503 (docs/contratos-api.md).
 */
export async function preguntarAlAsistente(cursoId, mensaje) {
  const { data } = await api.post('/api/agente/chat', { cursoId: Number(cursoId), mensaje })
  return data
}

/** HU-19 · Historial del docente en el curso, de la más reciente a la más antigua: id, fecha, pregunta, modelo, duración. */
export async function listarConversaciones(cursoId) {
  const { data } = await api.get('/api/agente/conversaciones', { params: { cursoId: Number(cursoId) } })
  return Array.isArray(data) ? data : []
}

/** HU-19 · Una conversación guardada, con su respuesta y las herramientas que usó. 403 si es de otro curso o de otro docente. */
export async function obtenerConversacion(conversacionId) {
  const { data } = await api.get(`/api/agente/conversaciones/${conversacionId}`)
  return data
}
