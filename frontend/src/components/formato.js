// Formato de cifras y fechas de las pantallas del Sprint 2: coma decimal y fechas de Ecuador.

const formatoFecha = new Intl.DateTimeFormat('es-EC', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  // Hora de Ecuador en cualquier equipo: la integración continua corre en UTC y una fecha límite de las 23:59 cambiaría de día.
  timeZone: 'America/Guayaquil',
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

/** Tono del punto de color de cada estado (diseno.md): OK y COMPLETADA en verde, PARCIAL y ACTIVA («Requiere atención») en ámbar, FALLIDA y ERROR en rojo. */
export function tonoDeEstado(estado) {
  switch (String(estado ?? '').toUpperCase()) {
    case 'OK':
    case 'COMPLETADA':
      return 'ok'
    case 'PARCIAL':
    case 'ACTIVA':
      return 'atencion'
    case 'FALLIDA':
    case 'ERROR':
      return 'error'
    default:
      return 'neutro'
  }
}

const formatoDiaMes = new Intl.DateTimeFormat('es-EC', { day: '2-digit', month: '2-digit', timeZone: 'America/Guayaquil' })

/** «29/09»: día y mes de una fecha límite, en hora de Ecuador. Sin fecha devuelve «sin fecha». */
export function formatearDiaMes(valor) {
  if (!valor) return 'sin fecha'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return 'sin fecha'
  // Algunos ICU devuelven «8/10» aunque se pida 2 dígitos: se arma con las partes para que siempre sea «08/10».
  const partes = Object.fromEntries(formatoDiaMes.formatToParts(fecha).map((p) => [p.type, p.value]))
  return `${partes.day.padStart(2, '0')}/${partes.month.padStart(2, '0')}`
}
