/**
 * Traduce un error de Axios a lo que muestra la pantalla. Todo error se ve en la aplicación:
 * 401 «Tu sesión venció», 403 «Acceso no permitido», 409 «Curso sin sincronizar», 503 «El asistente no está disponible».
 */
export function describirError(error) {
  const status = error?.response?.status
  const problema = error?.response?.data ?? {}
  const detalle = typeof problema.detail === 'string' ? problema.detail : undefined

  if (!error?.response) {
    return {
      tipo: 'red',
      titulo: 'No se pudo conectar con EduMonitor',
      detalle: 'La API no respondió. Revisa que el sistema esté encendido.',
    }
  }

  switch (status) {
    case 401:
      return { tipo: 'sesion', titulo: 'Tu sesión venció', detalle: 'Vuelve a iniciar sesión para continuar.' }
    case 403:
      return { tipo: 'acceso', titulo: 'Acceso no permitido', detalle: detalle ?? 'No tienes permiso para ver este curso.' }
    case 404:
      return { tipo: 'no-existe', titulo: 'No encontrado', detalle: detalle ?? 'Lo que buscas no existe.' }
    case 409:
      return { tipo: 'sin-sincronizar', titulo: 'Curso sin sincronizar', detalle }
    case 503:
      return { tipo: 'no-disponible', titulo: 'El asistente no está disponible', detalle }
    default:
      return { tipo: 'otro', titulo: problema.title ?? 'Ocurrió un error', detalle }
  }
}
