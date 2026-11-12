import { useNavigate } from 'react-router-dom'
import { cerrarSesion } from '../services/auth'

const ENCABEZADOS = {
  sesion: 'EduMonitor · sesión',
  acceso: 'EduMonitor · acceso',
  'no-existe': 'EduMonitor · sin resultados',
}

/**
 * HU-19 · TA-19.5 · Página completa sin barra lateral para «Acceso no permitido» (403), «Tu sesión venció» (401) y recursos que
 * no existen (diseño D): el código en letra técnica, el título en 44 px, el motivo en una línea y un botón para volver.
 */
export default function PaginaError({ error }) {
  const navigate = useNavigate()
  const esSesion = error.tipo === 'sesion'

  function volver() {
    if (esSesion) {
      cerrarSesion()
      navigate('/login', { replace: true })
      return
    }
    navigate('/cursos')
  }

  return (
    <main className="pagina-error" role="alert">
      <div className="regla" aria-hidden="true" />
      <span className="mono">{ENCABEZADOS[error.tipo] ?? 'EduMonitor'}</span>
      <h1>{error.titulo}</h1>
      {error.detalle && <p>{error.detalle}</p>}
      <button type="button" className="boton" onClick={volver}>
        {esSesion ? 'Iniciar sesión' : 'Volver a Mis cursos'}
      </button>
    </main>
  )
}
