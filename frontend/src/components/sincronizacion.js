/**
 * TA-21.A2 · Pasos de la pantalla «Sincronizando curso…» (mockup MK-05) y las funciones de Moodle de cada uno. POST
 * /api/cursos/{id}/sync responde al terminar: mientras espera, todos los pasos giran; con la respuesta, cada paso queda terminado
 * o con el error de su función, según `errores`. No se inventa un avance por tiempo.
 */
export const PASOS_SINCRONIZACION = [
  { texto: 'Cursos y unidades', funciones: ['core_course_get_contents', 'core_enrol_get_users_courses'] },
  { texto: 'Matriculados', funciones: ['core_enrol_get_enrolled_users'] },
  { texto: 'Ítems y notas por categoría', funciones: ['gradereport_user_get_grade_items'] },
  { texto: 'Entregas y fechas límite', funciones: ['mod_assign_get_assignments', 'mod_assign_get_submissions'] },
  { texto: 'Finalización de actividades', funciones: ['core_completion_get_activities_completion_status'] },
]

/** Estado de cada paso: `girando` sin respuesta; con ella, `error` si falló una de sus funciones (o toda la sincronización) o `listo`. */
export function pasosDeSincronizacion(resultado) {
  return PASOS_SINCRONIZACION.map((paso) => {
    if (!resultado) return { texto: paso.texto, estado: 'girando' }
    const errores = (resultado.errores ?? []).filter((e) => paso.funciones.includes(e.funcion))
    if (errores.length > 0) return { texto: paso.texto, estado: 'error', detalle: errores.map((e) => e.mensaje).join(' · ') }
    if (resultado.estado === 'FALLIDA') return { texto: paso.texto, estado: 'error', detalle: 'El aula virtual no respondió' }
    return { texto: paso.texto, estado: 'listo' }
  })
}

/** Pasos de «Abriendo curso…»: cada uno queda listo cuando responde su consulta real a la API (no llama a Moodle). */
export const PASOS_APERTURA = ['Libro de calificaciones', 'Indicadores y alertas', 'Estado del período']

/** El aviso de sincronización se cierra solo a los 8 s (docs/sprint-3/diseno.md). */
export const CIERRE_AVISO_MS = 8000

/** Pausa para que el docente vea los pasos terminados antes de cerrar la pantalla de carga. */
export const PAUSA_FINAL_MS = 600
