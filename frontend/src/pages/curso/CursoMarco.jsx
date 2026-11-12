import { useEffect, useState } from 'react'
import { Outlet, useLocation, useParams } from 'react-router-dom'
import Marco from '../../components/Marco'
import PaginaError from '../../components/PaginaError'
import TiraPeriodo from '../../components/TiraPeriodo'
import { obtenerCurso } from '../../services/cursos'
import { describirError } from '../../services/errores'

function CabeceraCurso({ curso }) {
  return (
    <header className="cabecera-curso">
      <div className="entra" style={{ display: 'flex', flexDirection: 'column', gap: 'var(--e-6)' }}>
        <span className="mono">
          {curso.nombreCorto}
          {curso.periodo ? ` · ${curso.periodo}` : ''}
        </span>
        <h1>{curso.nombre}</h1>
      </div>
      <TiraPeriodo cursoId={curso.id} ultimaSincronizacion={curso.ultimaSincronizacion} />
    </header>
  )
}

/**
 * HU-19 · TA-19.2 · Estructura de las pantallas de un curso (diseño D): cabecera oscura con el código, el nombre y la tira del
 * período; debajo, la barra lateral con Panel, Alertas y Asistente. El Asistente es un chat de página completa con su propia
 * barra (mockup MK-14). 401, 403 y 404 se ven en pantalla completa, sin barra lateral.
 */
export default function CursoMarco() {
  const { id } = useParams()
  const { pathname } = useLocation()
  const [cargado, setCargado] = useState({ id: null, curso: null, error: null })

  useEffect(() => {
    let vigente = true
    obtenerCurso(id)
      .then((datos) => vigente && setCargado({ id, curso: datos, error: null }))
      .catch((error) => vigente && setCargado({ id, curso: null, error: describirError(error) }))
    return () => {
      vigente = false
    }
  }, [id])

  // Lo que llegó de otro curso no se muestra mientras carga el nuevo.
  const { curso, error } = cargado.id === id ? cargado : { curso: null, error: null }

  if (error && ['sesion', 'acceso', 'no-existe'].includes(error.tipo)) {
    return <PaginaError error={error} />
  }

  if (curso && pathname.endsWith('/asistente')) {
    return <Outlet context={{ curso }} />
  }

  return (
    <Marco curso={curso ?? { id }} cabecera={curso ? <CabeceraCurso curso={curso} /> : null}>
      {error && (
        <div className="aviso aviso--error" role="alert">
          <strong>{error.titulo}</strong>
          {error.detalle}
        </div>
      )}
      {!curso && !error && <p className="ayuda">Cargando el curso…</p>}
      {curso && <Outlet context={{ curso }} />}
    </Marco>
  )
}
