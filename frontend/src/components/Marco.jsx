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
 * Pantallas del curso en la barra lateral, en el orden de docs/sprint-3/diseno.md. Solo van las que ya existen en la aplicación;
 * Libro de calificaciones, Mapa del curso y Plan de refuerzo se suman cuando su pantalla esté lista.
 */
const PANTALLAS_CURSO = [
  { ruta: 'libro', texto: 'Libro de calificaciones' },
  { ruta: 'panel', texto: 'Panel del curso' },
  { ruta: 'mapa', texto: 'Mapa del curso' },
  { ruta: 'alertas', texto: 'Alertas', contador: true },
  { ruta: 'refuerzos', texto: 'Plan de refuerzo' },
  { ruta: 'asistente', texto: 'Asistente' },
]

/**
 * R.2 · Estructura del Sprint 3 (docs/sprint-3/diseno.md): barra lateral oscura de 236 px con «EduMonitor», el código y el período
 * del curso, su navegación (Alertas con el contador de estudiantes que requieren atención), las pantallas de ADMIN, el usuario y
 * «Cerrar sesión». Dentro de un curso, la cabecera oscura y la barra blanca con el control segmentado van arriba del contenido.
 * La pantalla activa lleva una raya de 3 px en arena y la navegación conserva el tramo elegido (`?tramo=`).
 */
export default function Marco({ curso, cabecera, barra, alertas = null, busqueda = '', children }) {
  const navigate = useNavigate()
  const usuario = usuarioActual()

  function salir() {
    cerrarSesion()
    navigate('/login', { replace: true })
  }

  return (
    <div className="app">
      <aside className="lateral" aria-label="Navegación">
        <div className="lateral-cabecera">
          <Marca />
          {curso?.nombreCorto && (
            <span className="mono">
              {curso.nombreCorto}
              {curso.periodo ? ` · ${curso.periodo}` : ''}
            </span>
          )}
        </div>

        <nav aria-label="General">
          <NavLink to="/cursos" end className={claseEnlace}>
            Mis cursos
          </NavLink>
        </nav>

        {curso && (
          <nav aria-label={`Curso ${curso.nombreCorto ?? ''}`}>
            {PANTALLAS_CURSO.map((p) => (
              <NavLink key={p.ruta} to={`/cursos/${curso.id}/${p.ruta}${busqueda}`} className={claseEnlace}>
                <span>{p.texto}</span>
                {p.contador && alertas > 0 && (
                  <span className="lateral-contador num" aria-label={`${alertas} estudiantes requieren atención`}>
                    {alertas}
                  </span>
                )}
              </NavLink>
            ))}
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
            <NavLink to="/admin/auditoria" className={claseEnlace}>
              Auditoría
            </NavLink>
          </nav>
        )}

        <div className="lateral-pie">
          {usuario && (
            <span>
              {usuario.usuario ?? usuario.nombre} · {usuario.rol === 'ADMIN' ? 'Administrador' : 'Docente'}
            </span>
          )}
          <button type="button" className="lateral-salir" onClick={salir}>
            Cerrar sesión
          </button>
        </div>
      </aside>

      <div className="principal">
        {cabecera}
        {barra}
        <main className="contenido">{children}</main>
      </div>
    </div>
  )
}
