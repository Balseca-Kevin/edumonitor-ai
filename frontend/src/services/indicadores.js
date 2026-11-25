import api from './api'
import { paramsTramo } from '../components/tramo'

/**
 * HU-09 · Indicadores del curso calculados en el backend con la última sincronización (docs/contratos-api.md).
 * 403 si el curso es ajeno, 404 si no existe y 409 «Curso sin sincronizar».
 */
export async function obtenerIndicadores(cursoId, tramo) {
  const { data } = await api.get(`/api/cursos/${cursoId}/indicadores`, { params: paramsTramo(tramo) })
  return data
}

/**
 * Conteo de alertas activas para la tarjeta «Requiere atención». Las alertas son de HU-12: si su endpoint todavía no está
 * en la API o falla, devuelve null y la tarjeta muestra «no disponible», nunca 0.
 */
export async function contarAlertasActivas(cursoId, tramo) {
  try {
    const { data } = await api.get(`/api/cursos/${cursoId}/alertas`, { params: { estado: 'ACTIVA', ...paramsTramo(tramo) } })
    return Array.isArray(data) ? data.length : null
  } catch {
    return null
  }
}

/** R.2 · Estudiantes con al menos una alerta activa, para el contador de la barra lateral. Null si no se pudo consultar. */
export async function contarEstudiantesConAlertas(cursoId) {
  try {
    const { data } = await api.get(`/api/cursos/${cursoId}/alertas`, { params: { estado: 'ACTIVA' } })
    return Array.isArray(data) ? new Set(data.map((a) => a.estudiante?.id)).size : null
  } catch {
    return null
  }
}
