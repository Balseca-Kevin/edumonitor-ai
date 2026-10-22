
import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { obtenerCursos } from '../services/cursos'
import { cerrarSesion } from '../services/auth'

export default function Cursos() {
  const [cursos, setCursos] = useState([])
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState('')
  const [busqueda, setBusqueda] = useState('')
  const [periodoSeleccionado, setPeriodoSeleccionado] = useState('todos')
  const [estadoSeleccionado, setEstadoSeleccionado] = useState('todos')

  const navigate = useNavigate()

  useEffect(() => {
    let activo = true

    async function cargarCursos() {
      try {
        const datos = await obtenerCursos()

        if (activo) {
          setCursos(Array.isArray(datos) ? datos : [])
        }
      } catch (err) {
        if (activo) {
          setError(
            err.response?.status === 401
              ? 'Tu sesión ha expirado. Inicia sesión nuevamente.'
              : 'No fue posible cargar los cursos. Comprueba la conexión con el servidor.',
          )
        }
      } finally {
        if (activo) setCargando(false)
      }
    }

    cargarCursos()

    return () => {
      activo = false
    }
  }, [])

  function handleLogout() {
    cerrarSesion()
    navigate('/login', { replace: true })
  }

  const nombreUsuario = (() => {
    try {
      const usuario = JSON.parse(
        sessionStorage.getItem('edumonitor_usuario') || 'null',
      )
      return usuario?.nombre || 'Docente'
    } catch {
      return 'Docente'
    }
  })()

  const periodos = useMemo(
    () => [...new Set(cursos.map((curso) => curso.periodo).filter(Boolean))],
    [cursos],
  )

  const cursosFiltrados = useMemo(() => {
    const termino = busqueda.trim().toLocaleLowerCase('es')

    return cursos.filter((curso) => {
      const coincideBusqueda = [
        curso.nombre,
        curso.nombreCorto,
      ].some((valor) =>
        String(valor || '').toLocaleLowerCase('es').includes(termino),
      )

      const coincidePeriodo =
        periodoSeleccionado === 'todos' ||
        curso.periodo === periodoSeleccionado

      const estado = curso.ultimaSincronizacion?.estado
      const coincideEstado =
        estadoSeleccionado === 'todos' ||
        (estadoSeleccionado === 'completada' && estado === 'COMPLETADA') ||
        (estadoSeleccionado === 'pendiente' && estado !== 'COMPLETADA')

      return coincideBusqueda && coincidePeriodo && coincideEstado
    })
  }, [cursos, busqueda, periodoSeleccionado, estadoSeleccionado])

  function limpiarFiltros() {
    setBusqueda('')
    setPeriodoSeleccionado('todos')
    setEstadoSeleccionado('todos')
  }

  return (
    <div className="dashboard">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <span className="brand-mark">E</span>
          <span>EduMonitor AI</span>
        </div>

        <nav aria-label="Navegación principal">
          <a className="nav-link active" href="/cursos" aria-current="page">
            <span aria-hidden="true">▦</span>
            Mis cursos
          </a>
        </nav>

        <div className="sidebar-bottom">
          <span className="avatar">
            {nombreUsuario.charAt(0).toUpperCase()}
          </span>
          <div className="user-info">
            <strong>{nombreUsuario}</strong>
            <span>Docente</span>
          </div>
          <button
            className="logout-button"
            type="button"
            onClick={handleLogout}
            aria-label="Cerrar sesión"
            title="Cerrar sesión"
          >
            ↪
          </button>
        </div>
      </aside>

      <main className="dashboard-main">
        <header className="page-header">
          <div>
            <p className="eyebrow">ESPACIO ACADÉMICO</p>
            <h1>Mis cursos</h1>
            <p>Consulta tus cursos y el estado de sincronización.</p>
          </div>
          <span className="user-badge">{nombreUsuario}</span>
        </header>

        <section className="courses-section">
          <div className="section-heading">
            <div>
              <h2>Listado de cursos</h2>
              <p>Busca y filtra los cursos disponibles para tu cuenta.</p>
            </div>

            {!cargando && !error && (
              <span className="course-count" aria-live="polite">
                {cursosFiltrados.length}{' '}
                {cursosFiltrados.length === 1 ? 'curso' : 'cursos'}
              </span>
            )}
          </div>

          {!cargando && !error && cursos.length > 0 && (
            <div className="course-filters">
              <div className="course-search">
                <label htmlFor="buscar-curso">Buscar curso</label>
                <input
                  id="buscar-curso"
                  type="search"
                  placeholder="Nombre o código del curso..."
                  value={busqueda}
                  onChange={(event) => setBusqueda(event.target.value)}
                />
              </div>

              <div className="course-filter">
                <label htmlFor="filtrar-periodo">Período académico</label>
                <select
                  id="filtrar-periodo"
                  value={periodoSeleccionado}
                  onChange={(event) => setPeriodoSeleccionado(event.target.value)}
                >
                  <option value="todos">Todos los períodos</option>
                  {periodos.map((periodo) => (
                    <option key={periodo} value={periodo}>
                      {periodo}
                    </option>
                  ))}
                </select>
              </div>

              <div className="course-filter">
                <label htmlFor="filtrar-estado">Sincronización</label>
                <select
                  id="filtrar-estado"
                  value={estadoSeleccionado}
                  onChange={(event) => setEstadoSeleccionado(event.target.value)}
                >
                  <option value="todos">Todos los estados</option>
                  <option value="completada">Completada</option>
                  <option value="pendiente">Pendiente o sin registro</option>
                </select>
              </div>

              <button
                className="clear-filters-button"
                type="button"
                onClick={limpiarFiltros}
              >
                Limpiar filtros
              </button>
            </div>
          )}

          {cargando && (
            <p className="status-message" role="status">
              Cargando cursos...
            </p>
          )}

          {error && (
            <div className="error-panel" role="alert">
              <p>{error}</p>
              {error.startsWith('Tu sesión') && (
                <button type="button" onClick={handleLogout}>
                  Volver al inicio de sesión
                </button>
              )}
            </div>
          )}

          {!cargando && !error && cursos.length === 0 && (
            <div className="empty-panel">
              <span aria-hidden="true">◇</span>
              <h3>Aún no tienes cursos</h3>
              <p>Cuando haya cursos disponibles, aparecerán en esta sección.</p>
            </div>
          )}

          {!cargando && !error && cursos.length > 0 && cursosFiltrados.length === 0 && (
            <div className="empty-panel" role="status">
              <span aria-hidden="true">⌕</span>
              <h3>No se encontraron cursos</h3>
              <p>Prueba con otro nombre, código o filtro.</p>
              <button
                className="clear-filters-button"
                type="button"
                onClick={limpiarFiltros}
              >
                Limpiar búsqueda y filtros
              </button>
            </div>
          )}

          {!cargando && !error && cursosFiltrados.length > 0 && (
            <div className="courses-grid">
              {cursosFiltrados.map((curso) => (
                <article className="course-card" key={curso.id}>
                  <div className="course-card-top">
                    <span className="course-icon" aria-hidden="true">▦</span>
                    <span className="course-period">
                      {curso.periodo || 'Período no especificado'}
                    </span>
                  </div>

                  <p className="course-short-name">{curso.nombreCorto}</p>
                  <h3>{curso.nombre}</h3>

                  <div className="sync-info">
                    <span className="sync-label">Última sincronización</span>
                    {curso.ultimaSincronizacion ? (
                      <>
                        <strong>
                          {new Date(
                            curso.ultimaSincronizacion.fecha,
                          ).toLocaleString('es-EC')}
                        </strong>
                        <span
                          className={`sync-status ${
                            curso.ultimaSincronizacion.estado === 'COMPLETADA'
                              ? 'sync-success'
                              : 'sync-pending'
                          }`}
                        >
                          {curso.ultimaSincronizacion.estado}
                        </span>
                      </>
                    ) : (
                      <span className="sync-pending">
                        Sin sincronizaciones registradas
                      </span>
                    )}
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </main>
    </div>
  )
}
