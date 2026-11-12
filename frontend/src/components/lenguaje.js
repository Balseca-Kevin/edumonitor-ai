import { formatearFecha, formatearNumero } from './formato'

// Lenguaje docente del diseño D: ningún término estadístico en pantalla (z, tendencia, desviación).

const NO_DISPONIBLE = 'no disponible'

/** «1,5 bajo el promedio», «0,4 sobre el promedio» o «en el promedio»: la diferencia con el promedio del curso, sin z. */
export function frenteAlCurso(promedio, promedioCurso) {
  if (promedio === null || promedio === undefined || promedioCurso === null || promedioCurso === undefined) return NO_DISPONIBLE
  const diferencia = Math.round((promedio - promedioCurso) * 10) / 10
  if (diferencia === 0) return 'en el promedio'
  return `${formatearNumero(Math.abs(diferencia), 1)} ${diferencia < 0 ? 'bajo' : 'sobre'} el promedio`
}

/** «↓ bajando 1,8», «↑ subiendo 0,2» o «→ se mantiene»: últimas 3 notas frente a las anteriores. Null con menos de 4 notas. */
export function ultimasNotas(tendencia) {
  if (tendencia === null || tendencia === undefined) return 'aún sin datos'
  const valor = Math.round(tendencia * 10) / 10
  if (valor === 0) return '→ se mantiene'
  return valor < 0 ? `↓ bajando ${formatearNumero(-valor, 1)}` : `↑ subiendo ${formatearNumero(valor, 1)}`
}

export const valida = (u) =>
  Number.isInteger(u?.actividades) && Number.isInteger(u?.calificadas) && u.actividades >= 0 && u.calificadas >= 0 && u.calificadas <= u.actividades

/**
 * Unidad «actual» (regla documentada en docs/sprint-2/diseno.md): la primera unidad, en orden, con menos actividades
 * calificadas que actividades. Si todas están completas, ninguna es la actual. Se calcula aquí, con `unidades` del endpoint
 * de indicadores; el backend no la envía.
 */
export function unidadActual(unidades) {
  const ordenadas = [...(unidades ?? [])].filter(valida).sort((a, b) => a.numero - b.numero)
  return ordenadas.find((u) => u.calificadas < u.actividades)?.numero ?? null
}

/** Qué vigila cada regla, en palabras del docente. El código (R1, R2, R4) es interno y no se muestra. */
export const DESCRIPCION_REGLA = {
  R1: 'Avisa cuando las últimas 3 notas bajan al menos estos puntos frente a las anteriores.',
  R2: 'Avisa cuando hay al menos estas actividades vencidas sin entregar.',
  R4: 'Avisa cuando el promedio queda bajo este valor, con al menos 4 notas registradas.',
}

/** Evidencia de una alerta en una línea, con sus números (docs/contratos-api.md), según la regla que la activó. */
export function textoEvidencia(alerta) {
  const e = alerta.evidencia ?? {}
  const numero = (valor, decimales = 1) => formatearNumero(valor, decimales) ?? 'no disponible'

  switch (alerta.regla) {
    case 'R1':
      return `Sus últimas notas (${(e.ultimas ?? []).map((n) => numero(n)).join(' · ')}) están ${numero(
        Math.abs(e.diferencia ?? 0),
      )} puntos bajo las anteriores (${(e.anteriores ?? []).map((n) => numero(n)).join(' · ')}); el aviso salta desde ${numero(e.umbral)}`
    case 'R2': {
      const actividades = e.actividades ?? []
      return `${actividades.length} ${actividades.length === 1 ? 'actividad vencida' : 'actividades vencidas'} sin entregar: ${actividades
        .map((a) => `${a.nombre} (${formatearFecha(a.fechaLimite).slice(0, 5)})`)
        .join(' · ')}`
    }
    case 'R4':
      return `Promedio ${numero(e.promedio, 2)} con ${e.notas ?? 'no disponible'} notas registradas; el mínimo es ${numero(e.umbral)}`
    default:
      return '—'
  }
}

const normalizar = (texto) =>
  String(texto ?? '')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()

/** Estudiantes nombrados en la respuesta («Nombre Apellido»), buscados en los indicadores reales del curso, en el orden del texto. */
export function estudiantesMencionados(texto, estudiantes) {
  const plano = normalizar(texto)
  return (estudiantes ?? [])
    .map((e) => ({ e, posicion: plano.indexOf(normalizar(`${e.nombre} ${e.apellido}`)) }))
    .filter((m) => m.posicion >= 0)
    .sort((a, b) => a.posicion - b.posicion)
    .map((m) => m.e)
}

/** Motivo de la tarjeta en una línea, con los conteos reales del panel. */
export function motivo(e) {
  const partes = []
  const n = e.notas ?? {}
  if (n.ceros) partes.push(`${n.ceros} ${n.ceros === 1 ? 'nota' : 'notas'} en cero`)
  if (n.sinEntrega) partes.push(`${n.sinEntrega} sin entregar`)
  if (n.pendientes) partes.push(`${n.pendientes} por calificar`)
  if (e.tendencia !== null && e.tendencia !== undefined && e.tendencia <= -0.5) partes.push(`últimas notas ${ultimasNotas(e.tendencia)}`)
  return partes.length ? partes.join(' · ') : 'Sin ceros ni entregas pendientes'
}

/** «Datos usados» en palabras del docente: qué consultó el asistente, sin nombres de funciones. */
export function datoUsado(herramienta, estudiantes) {
  const p = herramienta.parametros ?? {}
  let texto = 'Datos del curso'
  if (herramienta.nombre === 'listar_cursos_autorizados') texto = 'Tus cursos asignados'
  else if (herramienta.nombre === 'identificar_alertas') texto = 'Alertas del curso'
  else if (herramienta.nombre === 'obtener_calificaciones') {
    const porId = p.estudianteId ? (estudiantes ?? []).find((e) => e.id === Number(p.estudianteId)) : null
    if (p.nombre) texto = `Notas de ${p.nombre}`
    else if (porId) texto = `Notas de ${porId.nombre} ${porId.apellido}`
    else if (p.estudianteId) texto = 'Notas de un estudiante'
    else texto = 'Calificaciones del curso'
  }
  return `${texto} · ${herramienta.duracionMs} ms${herramienta.estado === 'OK' ? '' : ' · no se pudo consultar'}`
}

/** Grupo del historial: «Hoy», «Esta semana» (últimos 7 días) o «Antes». */
export function grupoDeFecha(fecha, ahora = new Date()) {
  const f = new Date(fecha)
  if (f.toDateString() === ahora.toDateString()) return 'Hoy'
  return ahora - f < 7 * 24 * 60 * 60 * 1000 ? 'Esta semana' : 'Antes'
}
