
import { useEffect, useState } from 'react'
import {
  Navigate,
  Outlet,
  useLocation,
} from 'react-router-dom'
import PaginaError from './PaginaError'

function obtenerUsuario() {
  try {
    return JSON.parse(
      sessionStorage.getItem('edumonitor_usuario') || 'null',
    )
  } catch {
    return null
  }
}

/**
 * HU-19 / TA-19.5
 *
 * Protege el acceso por sesión y rol.
 *
 * 401: sesión vencida.
 * 403: usuario sin permisos de administrador.
 */
export default function Layout() {
  const location = useLocation()
  const [sesionVencida, setSesionVencida] = useState(false)

  useEffect(() => {
    function manejarSesionVencida() {
      setSesionVencida(true)
    }

    window.addEventListener(
      'edumonitor:sesion-vencida',
      manejarSesionVencida,
    )

    return () => {
      window.removeEventListener(
        'edumonitor:sesion-vencida',
        manejarSesionVencida,
      )
    }
  }, [])

  const token = sessionStorage.getItem('edumonitor_token')
  const usuario = obtenerUsuario()

  // Una sesión vencida requiere volver a autenticarse.
  if (sesionVencida) {
    return (
      <PaginaError
        error={{
          tipo: 'sesion',
          titulo: 'Tu sesión venció',
          detalle:
            'Tu sesión ha expirado. Inicia sesión nuevamente para continuar.',
        }}
      />
    )
  }

  // Sin token no existe una sesión autenticada.
  if (!token) {
    return <Navigate to="/login" replace />
  }

  // Las rutas administrativas solo admiten ADMIN.
  const rutaAdministrativa =
    location.pathname === '/admin' ||
    location.pathname.startsWith('/admin/')

  if (rutaAdministrativa && usuario?.rol !== 'ADMIN') {
    return (
      <PaginaError
        error={{
          tipo: 'acceso',
          titulo: 'Acceso no permitido',
          detalle:
            'No tienes permisos para acceder a esta sección.',
        }}
      />
    )
  }

  return <Outlet />
}
