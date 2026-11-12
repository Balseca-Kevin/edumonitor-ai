import { Link, NavLink, useNavigate } from 'react-router-dom'
import { cerrarSesion } from '../../services/auth'
import '../../styles/sprint2.css'

function usuarioActual() {
  try {
    return JSON.parse(sessionStorage.getItem('edumonitor_usuario') ?? 'null')
  } catch {
    return null
  }
}

/**
 * Estructura del Sprint 2 (diseno.md): barra lateral de 240 px con el nombre del sistema, las secciones del curso abierto,
 * las de ADMIN y cerrar sesión abajo.
 */
export default function MarcoS2({ curso, children }) {
  const navigate = useNavigate()
  const usuario = usuarioActual()

  function salir() {
    cerrarSesion()
    navigate('/login', { replace: true })
  }

  return (
    <div className="s2 s2-app">
      <aside className="s2-lateral" aria-label="Navegación">
        <Link to="/cursos" className="s2-marca">
          EduMonitor
        </Link>

        <nav className="s2-nav" aria-label="General">
          <NavLink to="/cursos" end>
            Mis cursos
          </NavLink>
        </nav>

        {curso && (
          <nav className="s2-nav" aria-label={`Curso ${curso.nombreCorto ?? ''}`}>
            <span className="s2-nav-titulo">{curso.nombreCorto ?? 'Curso'}</span>
            <NavLink to={`/cursos/${curso.id}/panel`}>Panel</NavLink>
            <NavLink to={`/cursos/${curso.id}/alertas`}>Alertas</NavLink>
            <NavLink to={`/cursos/${curso.id}/asistente`}>Asistente</NavLink>
          </nav>
        )}

        {usuario?.rol === 'ADMIN' && (
          <nav className="s2-nav" aria-label="Administración">
            <span className="s2-nav-titulo">Administración</span>
            <NavLink to="/admin/moodle">Configuración de Moodle</NavLink>
            <NavLink to="/admin/estado">Estado del sistema</NavLink>
          </nav>
        )}

        <div className="s2-lateral-pie s2-nav">
          {usuario && (
            <span className="s2-ayuda">
              {usuario.nombre} · {usuario.rol}
            </span>
          )}
          <button type="button" className="s2-boton s2-boton--secundario s2-boton--chico" onClick={salir}>
            Cerrar sesión
          </button>
        </div>
      </aside>

      <main className="s2-contenido">{children}</main>
    </div>
  )
}
