import { useNavigate } from 'react-router-dom'
import { cerrarSesion } from '../../services/auth'
import '../../styles/sprint2.css'

/**
 * Página completa sin barra lateral para «Acceso no permitido» (403), «Tu sesión venció» (401) y recursos que no existen.
 * Título en 40 px, el motivo en una línea y un botón para volver.
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
    <main className="s2 s2-pagina-error" role="alert">
      <h1>{error.titulo}</h1>
      {error.detalle && <p>{error.detalle}</p>}
      <div>
        <button type="button" className="s2-boton" onClick={volver}>
          {esSesion ? 'Iniciar sesión' : 'Volver a Mis cursos'}
        </button>
      </div>
    </main>
  )
}
