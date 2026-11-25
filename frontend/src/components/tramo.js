import { useSearchParams } from 'react-router-dom'

/**
 * R.2 · Tramo que elige el control segmentado de la barra del curso. Va en la URL (`?tramo=u2`) para que el libro, el panel y las
 * alertas filtren lo mismo y el enlace se pueda compartir. Los cálculos del tramo los hace el backend (docs/contratos-api.md,
 * «Tramo del período»); aquí solo se elige.
 */
export const TODO = 'todo'

export function useTramo() {
  const [parametros] = useSearchParams()
  const tramo = parametros.get('tramo')
  return tramo && /^(p|u)\d{1,2}$/.test(tramo) ? tramo : TODO
}

/** Parámetros de la consulta: sin `tramo` cuando es «todo», igual que la ruta sin filtro. */
export function paramsTramo(tramo) {
  return tramo && tramo !== TODO ? { tramo } : {}
}

/** Sufijo de la URL que conserva el tramo al cambiar de pantalla. */
export function busquedaTramo(tramo) {
  return tramo && tramo !== TODO ? `?tramo=${tramo}` : ''
}

/**
 * Segmentos del control: «Todo el ciclo», cada parcial del curso y cada unidad (U1 … Un), con tantas como tenga el curso.
 * Salen de las unidades reales que devuelve la API; sin unidades queda solo «Todo el ciclo».
 */
export function segmentosDe(unidades = []) {
  const ordenadas = [...unidades].sort((a, b) => a.numero - b.numero)
  const parciales = [...new Set(ordenadas.map((u) => u.parcial).filter((p) => p > 0))].sort((a, b) => a - b)
  return [
    { codigo: TODO, texto: 'Todo el ciclo', titulo: 'Todas las actividades del curso' },
    ...parciales.map((p) => ({ codigo: `p${p}`, texto: `Parcial ${p}`, titulo: `Actividades del parcial ${p}` })),
    ...ordenadas.map((u) => ({ codigo: `u${u.numero}`, texto: `U${u.numero}`, titulo: `Unidad ${u.numero} · ${u.nombre}` })),
  ]
}

/** Texto del tramo para los títulos y la ayuda («Parcial 2», «Unidad 3 · Principales metodologías…»). */
export function nombreTramo(tramo, unidades = []) {
  if (!tramo || tramo === TODO) return 'Todo el ciclo'
  const numero = Number(tramo.slice(1))
  if (tramo.startsWith('p')) return `Parcial ${numero}`
  const unidad = unidades.find((u) => u.numero === numero)
  return unidad ? `Unidad ${numero} · ${unidad.nombre}` : `Unidad ${numero}`
}
