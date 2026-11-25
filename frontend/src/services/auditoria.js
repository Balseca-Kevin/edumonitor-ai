import api from './api'

/**
 * HU-17 · Registro de auditoría (solo ADMIN), del más reciente al más antiguo. Filtros opcionales: acción y usuario.
 * Respuesta: { total, acciones, registros } (docs/contratos-api.md, «Auditoría»).
 */
export async function obtenerAuditoria({ accion, usuario, limite } = {}) {
  const params = {}
  if (accion) params.accion = accion
  if (usuario?.trim()) params.usuario = usuario.trim()
  if (limite) params.limite = limite
  const { data } = await api.get('/api/auditoria', { params })
  return data
}
