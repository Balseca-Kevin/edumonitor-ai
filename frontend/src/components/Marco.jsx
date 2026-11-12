import { Link, NavLink, useNavigate } from 'react-router-dom'
import { cerrarSesion } from '../services/auth'

function usuarioActual() {
  try {
    return JSON.parse(sessionStorage.getItem('edumonitor_usuario') ?? 'null')
  } catch {
    return null
  }
}

const claseEnlace = ({ isActive }) => (isActive ? 'activo' : undefined)

/** Nombre del sistema: «Edu» en 300 y «Monitor» en 800 (diseño D). */
export function Marca() {
  return (
    <Link to="/cursos" className="marca" aria-label="EduMonitor, Mis cursos">
      <span>Edu</span>
      <b>Monitor</b>
    </Link>
  )
}

/**
 * HU-19 · Estructura del diseño D (docs/sprint-2/diseno.md): barra lateral clara de 240 px con el nombre del sistema, las
 * secciones del curso abierto (Panel, Alertas, Asistente), las de ADMIN y cerrar sesión abajo. Dentro de un curso, la
 * cabecera oscura con la tira del período va arriba, a todo el ancho.
 */
export default function Marco({ curso, cabecera, children }) {
  const navigate = useNavigate()
  const usuario = usuarioActual()

  function salir() {
    cerrarSesion()
    navigate('/login', { replace: true })
  }

  const aplicacion = (
    <div className="app">
      <aside className="lateral" aria-label="Navegación">
        <div className="lateral-cabecera">
          <Marca />
          {curso?.nombreCorto && <span className="mono">{curso.nombreCorto}</span>}
        </div>

        <nav aria-label="General">
          <NavLink to="/cursos" end className={({ isActive }) => (isActive ? 'activo' : curso ? 'suave' : undefined)}>
            {curso ? '← Mis cursos' : 'Mis cursos'}
          </NavLink>
        </nav>

        {curso && (
          <nav aria-label={`Curso ${curso.nombreCorto ?? ''}`}>
            <NavLink to={`/cursos/${curso.id}/panel`} className={claseEnlace}>
              Panel
            </NavLink>
            <NavLink to={`/cursos/${curso.id}/alertas`} className={claseEnlace}>
              Alertas
            </NavLink>
            <NavLink to={`/cursos/${curso.id}/asistente`} className={claseEnlace}>
              Asistente
            </NavLink>
          </nav>
        )}

        {usuario?.rol === 'ADMIN' && (
          <nav aria-label="Administración">
            <span className="lateral-titulo">Administración</span>
            <NavLink to="/admin/moodle" className={claseEnlace}>
              Configuración de Moodle
            </NavLink>
            <NavLink to="/admin/estado" className={claseEnlace}>
              Estado del sistema
            </NavLink>
          </nav>
        )}

        <div className="lateral-pie">
          {usuario && <span>{usuario.nombre}</span>}
          <button type="button" className="boton boton--secundario boton--chico" onClick={salir}>
            Cerrar sesión
          </button>
        </div>
      </aside>

      <main className="contenido">{children}</main>
    </div>
  )

  if (!cabecera) return aplicacion

  return (
    <div className="pagina-curso">
      {cabecera}
      {aplicacion}
    </div>
  )
}
