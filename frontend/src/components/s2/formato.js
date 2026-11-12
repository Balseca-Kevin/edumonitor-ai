// Formato de cifras y fechas de las pantallas del Sprint 2: coma decimal y fechas de Ecuador.

const formatoFecha = new Intl.DateTimeFormat('es-EC', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

export function formatearFecha(valor) {
  if (!valor) return ''
  const fecha = new Date(valor)
  return Number.isNaN(fecha.getTime()) ? '' : formatoFecha.format(fecha)
}

/** Número con coma decimal. Null o undefined devuelve null para que la pantalla muestre «no disponible», nunca 0. */
export function formatearNumero(valor, decimales = 2) {
  if (valor === null || valor === undefined || Number.isNaN(Number(valor))) return null
  return Number(valor).toLocaleString('es-EC', {
    minimumFractionDigits: decimales,
    maximumFractionDigits: decimales,
  })
}

export function formatearPorcentaje(valor) {
  if (valor === null || valor === undefined) return null
  return `${formatearNumero(Number(valor) * 100, 0)} %`
}

/** Tono del punto de color de cada estado (diseno.md): OK y COMPLETADA en verde, PARCIAL en ámbar, FALLIDA y ERROR en rojo. */
export function tonoDeEstado(estado) {
  switch (String(estado ?? '').toUpperCase()) {
    case 'OK':
    case 'COMPLETADA':
      return 'ok'
    case 'PARCIAL':
      return 'atencion'
    case 'FALLIDA':
    case 'ERROR':
      return 'error'
    default:
      return 'neutro'
  }
}
