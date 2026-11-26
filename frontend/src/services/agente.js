import api from './api'

/**
 * HU-14 · Pregunta al asistente de un curso. La API valida el curso y llama al agente;
 * el navegador nunca habla con el agente. Respuestas: 200, 400, 403, 404 y 503 (docs/contratos-api.md).
 */
export async function preguntarAlAsistente(cursoId, mensaje, foco, conversacionId) {
  const cuerpo = { cursoId: Number(cursoId), mensaje }
  // TA-15.A4 · Con { refuerzoId } el plan de refuerzo es el foco: el asistente redacta su borrador.
  // HU-23 · { estudianteId, actividadId } es lo que la pantalla tiene abierto; conversacionId sigue el hilo con su memoria.
  const elegido = {}
  for (const clave of ['refuerzoId', 'estudianteId', 'actividadId']) {
    if (foco?.[clave]) elegido[clave] = Number(foco[clave])
  }
  // C7 · El tramo de la barra del curso: el texto del asistente y las tarjetas calculan con las mismas actividades.
  if (foco?.tramo && foco.tramo !== 'todo') elegido.tramo = foco.tramo
  if (Object.keys(elegido).length) cuerpo.foco = elegido
  if (conversacionId) cuerpo.conversacionId = Number(conversacionId)
  const { data } = await api.post('/api/agente/chat', cuerpo)
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
