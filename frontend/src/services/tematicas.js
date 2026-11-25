import api from './api'

/**
 * HU-08 · Mapeo temático y desempeño por tema (docs/contratos-api.md, «Mapeo temático» y «Desempeño por tema»).
 * 403 curso ajeno, 404 no existe, 409 sin sincronizar.
 */
export async function obtenerMapeo(cursoId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/mapeo`)
  return data
}

export async function crearTema(cursoId, unidadId, nombre) {
  const { data } = await api.post(`/api/cursos/${cursoId}/unidades/${unidadId}/temas`, { nombre })
  return data
}

/** Reemplaza los temas de la actividad y el tema de cada criterio de su rúbrica. */
export async function asociarTemas(cursoId, actividadId, temas, criterios) {
  const { data } = await api.put(`/api/cursos/${cursoId}/actividades/${actividadId}/temas`, { temas, criterios })
  return data
}

/** Desempeño por tema del curso o, con `actividadId`, de una actividad. */
export async function obtenerTematicas(cursoId, actividadId) {
  const { data } = await api.get(`/api/cursos/${cursoId}/tematicas`, { params: actividadId ? { actividadId } : {} })
  return data
}
