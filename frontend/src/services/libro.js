import api from './api'
import { paramsTramo } from '../components/tramo'

/**
 * HU-21 · Libro de calificaciones del curso (docs/contratos-api.md): la nota de cada estudiante en cada actividad, por parcial y
 * unidad, con su estado (CALIFICADO, SIN_ENTREGA, PENDIENTE, AUN_NO_VENCE o null). Con `tramo`, solo las columnas de ese tramo.
 * 403 curso ajeno, 404 no existe, 409 sin sincronizar.
 */
export async function obtenerLibro(cursoId, { tramo, busqueda, orden } = {}) {
  const params = { ...paramsTramo(tramo) }
  if (busqueda) params.busqueda = busqueda
  if (orden) params.orden = orden
  const { data } = await api.get(`/api/cursos/${cursoId}/libro`, { params })
  return data
}

/** Actividades del libro en el orden de las columnas, con su parcial y su unidad. */
export function actividadesDelLibro(libro) {
  return (libro?.parciales ?? []).flatMap((p) =>
    p.unidades.flatMap((u) => u.actividades.map((a) => ({ ...a, parcial: p.numero, unidad: u.numero }))),
  )
}
