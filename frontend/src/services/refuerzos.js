import api from './api'

/**
 * HU-15 · Planes de refuerzo (docs/contratos-api.md, «Plan de refuerzo»): el docente arma el plan, el asistente redacta el
 * borrador, el docente lo edita, lo aprueba y lo descarga. 400 datos inválidos, 404 plan ajeno, 409 aprobado o sin aprobar.
 */
export async function crearPlan(cursoId, { actividadId, temas, estudiantes, tipo }) {
  const cuerpo = { actividadId, temas, estudiantes }
  // HU-15 · RETROALIMENTACION: borrador por estudiante de toda la actividad, sin temas. Sin tipo es un plan de refuerzo.
  if (tipo) cuerpo.tipo = tipo
  const { data } = await api.post(`/api/cursos/${cursoId}/refuerzos`, cuerpo)
  return data
}

/** TA-15.A2 · Refuerzos aplicados: nota del tema antes y después de cada refuerzo aprobado, con los conteos. */
export async function obtenerAplicados(cursoId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/refuerzos/aplicados`)
  return data
}

/** HU-15 · Nombre de un plan en pantalla según su tipo. */
export function nombrePlan(p) {
  return p.tipo === 'RETROALIMENTACION'
    ? `Retroalimentación de ${p.actividad.nombre}`
    : `Refuerzo de ${p.actividad.nombre}: ${p.temas.map((t) => t.nombre).join(', ')}`
}

export async function listarPlanes(cursoId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/refuerzos`)
  return Array.isArray(data) ? data : []
}

export async function obtenerPlan(cursoId, planId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/refuerzos/${planId}`)
  return data
}

/** Edita el texto del borrador y, con `aprobar`, lo aprueba. */
export async function cambiarPlan(cursoId, planId, { texto, aprobar = false }) {
  const cuerpo = {}
  if (texto !== undefined) cuerpo.texto = texto
  if (aprobar) cuerpo.estado = 'APROBADO'
  const { data } = await api.patch(`/api/cursos/${cursoId}/refuerzos/${planId}`, cuerpo)
  return data
}

/** Documento del plan aprobado (pdf o docx) como archivo para guardar. */
export async function descargarDocumento(cursoId, planId, formato) {
  const respuesta = await api.get(`/api/cursos/${cursoId}/refuerzos/${planId}/documento`, { params: { formato }, responseType: 'blob' })
  const nombre = /filename="?([^";]+)"?/.exec(respuesta.headers?.['content-disposition'] ?? '')?.[1] ?? `plan-refuerzo-${planId}.${formato}`
  return { blob: respuesta.data, nombre }
}

/** Texto del estado de un plan en pantalla. */
export const ESTADO_PLAN = { ENVIADO: 'Enviado al asistente', BORRADOR: 'Borrador por aprobar', APROBADO: 'Aprobado' }
