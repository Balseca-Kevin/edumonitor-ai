import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  obtenerCursos,
  sincronizarCurso,
  obtenerHistorialSincronizaciones,
} from '../services/cursos'
import Marco from '../components/Marco'
import Button from '../components/ui/Button'
import Input from '../components/ui/Input'
import Table from '../components/ui/Table'
import StatusBadge from '../components/ui/StatusBadge'
import Alert from '../components/ui/Alert'

function formatearFecha(fecha) {
  if (!fecha) return 'No disponible'
  const valor = new Date(fecha)
  return Number.isNaN(valor.getTime())
    ? fecha
    : valor.toLocaleString('es-EC', { dateStyle: 'short', timeStyle: 'short' })
}

function etiquetaEstado(estado) {
  return String(estado || 'PENDIENTE').toUpperCase()
}

function mensajeError(error) {
  if (error?.response?.status === 403) return 'Este curso no está asignado a tu cuenta'
  if (error?.response?.status === 502) return 'Moodle no responde; intenta más tarde'
  return (
    error?.response?.data?.detail ||
    error?.response?.data?.title ||
    (error?.response
      ? 'No se pudo completar la operación.'
      : 'No se pudo conectar con el servidor. Intenta de nuevo.')
  )
}

function usuarioActual() {
  try {
    return JSON.parse(sessionStorage.getItem('edumonitor_usuario') || 'null')
  } catch {
    return null
  }
}

function ErroresSincronizacion({ errores }) {
  if (!errores?.length) return null
  return (
    <ul className="hu19-cursos__errores">
      {errores.map((item, indice) => (
        <li key={`${item.funcion || 'sistema'}-${indice}`}>
          <code>{item.funcion || 'Sistema'}:</code> {item.mensaje || 'Error sin descripción'}
        </li>
      ))}
    </ul>
  )
}

function DetalleCurso({ curso, resultado, historial, historialAbierto, cargandoHistorial }) {
  if (!resultado && !historialAbierto) return null

  const columnasHistorial = [
    {
      key: 'fecha',
      label: 'Fecha',
      render: (_valor, item) => formatearFecha(item.fin || item.inicio || item.fecha),
    },
    {
      key: 'estado',
      label: 'Estado',
      render: (estado) => <StatusBadge status={estado} />,
    },
    {
      key: 'registros',
      label: 'Registros',
      render: (registros) => Object.values(registros || {}).reduce(
        (total, cantidad) => total + (Number(cantidad) || 0), 0,
      ),
    },
    {
      key: 'errores',
      label: 'Errores',
      render: (errores) => errores?.length
        ? <ErroresSincronizacion errores={errores} />
        : 'Sin errores',
    },
  ]

  const historialOrdenado = [...(historial || [])].sort(
    (a, b) => new Date(b.fin || b.inicio || b.fecha || 0) -
      new Date(a.fin || a.inicio || a.fecha || 0),
  )

  return (
    <section className="hu19-cursos__detalle" aria-label={`Detalle de ${curso.nombreCorto}`}>
      {resultado && (
        <section className="hu19-cursos__resultado" aria-label="Resultado de sincronización">
          {['PARCIAL', 'FALLIDA'].includes(etiquetaEstado(resultado.estado)) && (
            <Alert variant="warning" title="Sincronización incompleta">
              <p>La sincronización no se completó correctamente. Revisa los errores reportados.</p>
              <ErroresSincronizacion errores={resultado.errores} />
            </Alert>
          )}
          <div className="hu19-cursos__titulo-detalle">
            <h3>Resultado de sincronización</h3>
            <StatusBadge status={resultado.estado} />
          </div>
          <div className="hu19-cursos__fechas">
            {resultado.inicio && <span>Inicio: {formatearFecha(resultado.inicio)}</span>}
            {resultado.fin && <span>Fin: {formatearFecha(resultado.fin)}</span>}
          </div>
          {resultado.registros && (
            <dl className="hu19-cursos__registros">
              {Object.entries(resultado.registros).map(([nombre, cantidad]) => (
                <div key={nombre}>
                  <dt>{nombre}</dt>
                  <dd>{cantidad}</dd>
                </div>
              ))}
            </dl>
          )}
          {resultado.errores?.length > 0 && (
            <div>
              <h4>Errores reportados</h4>
              <ErroresSincronizacion errores={resultado.errores} />
            </div>
          )}
        </section>
      )}
      {historialAbierto && (
        <section className="hu19-cursos__historial">
          <h3>Historial de sincronizaciones</h3>
          {cargandoHistorial
            ? <p role="status">Cargando historial...</p>
            : historialOrdenado.length === 0
              ? <p>No hay sincronizaciones registradas.</p>
              : <Table caption={`Historial de sincronizaciones de ${curso.nombreCorto}`} columns={columnasHistorial} rows={historialOrdenado} />}
        </section>
      )}
    </section>
  )
}

export default function Cursos() {
  const navigate = useNavigate()
  const usuario = usuarioActual()
  const [cursos, setCursos] = useState([])
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState('')
  const [busqueda, setBusqueda] = useState('')
  const [periodoSeleccionado, setPeriodoSeleccionado] = useState('')
  const [estadoSeleccionado, setEstadoSeleccionado] = useState('')
  const [sincronizandoId, setSincronizandoId] = useState(null)
  const [historialAbiertoId, setHistorialAbiertoId] = useState(null)
  const [historialPorCurso, setHistorialPorCurso] = useState({})
  const [cargandoHistorialId, setCargandoHistorialId] = useState(null)
  const [errorOperacion, setErrorOperacion] = useState('')
  const [resultadoPorCurso, setResultadoPorCurso] = useState({})

  useEffect(() => {
    let activo = true
    obtenerCursos()
      .then((datos) => {
        if (activo) setCursos(Array.isArray(datos) ? datos : [])
      })
      .catch(() => {
        if (activo) setError('No fue posible cargar los cursos')
      })
      .finally(() => {
        if (activo) setCargando(false)
      })
    return () => { activo = false }
  }, [])

  const periodos = useMemo(
    () => [...new Set(cursos.map((curso) => curso.periodo).filter(Boolean))],
    [cursos],
  )

  const cursosFiltrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase()
    return cursos.filter((curso) => {
      const coincideBusqueda = !texto || [curso.nombre, curso.nombreCorto]
        .filter(Boolean)
        .some((valor) => String(valor).toLowerCase().includes(texto))
      const coincidePeriodo = !periodoSeleccionado || curso.periodo === periodoSeleccionado
      const completa = etiquetaEstado(curso.ultimaSincronizacion?.estado) === 'COMPLETADA'
      const coincideEstado = !estadoSeleccionado ||
        (estadoSeleccionado === 'sincronizado' && completa) ||
        (estadoSeleccionado === 'pendiente' && !completa)
      return coincideBusqueda && coincidePeriodo && coincideEstado
    })
  }, [cursos, busqueda, periodoSeleccionado, estadoSeleccionado])

  function limpiarFiltros() {
    setBusqueda('')
    setPeriodoSeleccionado('')
    setEstadoSeleccionado('')
  }

  async function manejarSincronizacion(curso) {
    if (sincronizandoId !== null) return
    setSincronizandoId(curso.id)
    setErrorOperacion('')
    try {
      const resultado = await sincronizarCurso(curso.id)
      setResultadoPorCurso((anterior) => ({ ...anterior, [curso.id]: resultado }))
      setCursos((anteriores) => anteriores.map((item) => item.id === curso.id
        ? {
            ...item,
            ultimaSincronizacion: {
              id: resultado?.id,
              fecha: resultado?.fin || resultado?.fecha || new Date().toISOString(),
              estado: resultado?.estado || 'PENDIENTE',
            },
          }
        : item))
      try {
        const historial = await obtenerHistorialSincronizaciones(curso.id)
        setHistorialPorCurso((anterior) => ({
          ...anterior,
          [curso.id]: Array.isArray(historial) ? historial : [],
        }))
      } catch {
        // El resultado de sincronización queda visible aunque falle la consulta del historial.
      }
    } catch (err) {
      setErrorOperacion(`No se pudo sincronizar "${curso.nombre || curso.nombreCorto || 'el curso'}": ${mensajeError(err)}`)
    } finally {
      setSincronizandoId(null)
    }
  }

  async function manejarHistorial(curso) {
    if (historialAbiertoId === curso.id) {
      setHistorialAbiertoId(null)
      return
    }
    setHistorialAbiertoId(curso.id)
    setErrorOperacion('')
    if (historialPorCurso[curso.id]) return
    setCargandoHistorialId(curso.id)
    try {
      const historial = await obtenerHistorialSincronizaciones(curso.id)
      setHistorialPorCurso((anterior) => ({
        ...anterior,
        [curso.id]: Array.isArray(historial) ? historial : [],
      }))
    } catch (err) {
      setErrorOperacion(`No se pudo cargar el historial: ${mensajeError(err)}`)
      setHistorialPorCurso((anterior) => ({ ...anterior, [curso.id]: [] }))
    } finally {
      setCargandoHistorialId(null)
    }
  }

  const columnas = [
    { key: 'nombreCorto', label: 'Código' },
    { key: 'nombre', label: 'Curso', render: (valor, curso) => valor || curso.nombreCorto || 'Curso sin nombre' },
    { key: 'periodo', label: 'Período', render: (valor) => valor || '—' },
    {
      key: 'ultimaSincronizacion',
      label: 'Última sincronización',
      render: (ultima) => ultima ? formatearFecha(ultima.fecha) : 'Sin registro',
    },
    {
      key: 'estado',
      label: 'Estado',
      render: (_valor, curso) => <StatusBadge status={curso.ultimaSincronizacion?.estado} />,
    },
    {
      key: 'acciones',
      label: 'Acciones',
      render: (_valor, curso) => (
        <div className="hu19-cursos__acciones">
          <Button variant="secondary" size="small" onClick={() => navigate(`/cursos/${curso.id}/panel`)}>
            Abrir
          </Button>
          <Button
            size="small"
            disabled={sincronizandoId !== null}
            aria-busy={sincronizandoId === curso.id}
            onClick={() => manejarSincronizacion(curso)}
          >
            {sincronizandoId === curso.id ? 'Sincronizando...' : 'Sincronizar curso'}
          </Button>
          <Button
            variant="secondary"
            size="small"
            disabled={cargandoHistorialId === curso.id}
            aria-expanded={historialAbiertoId === curso.id}
            onClick={() => manejarHistorial(curso)}
          >
            {cargandoHistorialId === curso.id
              ? 'Cargando historial...'
              : historialAbiertoId === curso.id
                ? 'Ocultar historial'
                : 'Ver historial'}
          </Button>
        </div>
      ),
    },
  ]

  return (
    <Marco>
      <div className="hu19-cursos">
        <header className="hu19-cursos__cabecera">
          <div>
            <p className="hu19-cursos__sobretitulo">
              {usuario?.nombre ? `Hola, ${usuario.nombre}` : 'Bienvenido'}
              {periodos.length > 0 && ` · Período ${periodos.join(', ')}`}
            </p>
            <h1>Mis cursos</h1>
            <p className="hu19-cursos__subtitulo">
              <strong>{cursosFiltrados.length === 1 ? '1 curso' : `${cursosFiltrados.length} cursos`}</strong> en el registro
            </p>
          </div>
          <div className="hu19-cursos__cabecera-acciones">
            {usuario?.rol === 'ADMIN' && (
              <>
                <Button variant="secondary" onClick={() => navigate('/admin/moodle')}>Configuración de Moodle</Button>
                <Button variant="secondary" onClick={() => navigate('/admin/estado')}>Estado del sistema</Button>
              </>
            )}
          </div>
        </header>

        <section className="hu19-cursos__filtros" aria-label="Búsqueda y filtros">
          <Input
            aria-label="Buscar cursos"
            type="search"
            placeholder="Buscar por nombre o código"
            value={busqueda}
            onChange={(event) => setBusqueda(event.target.value)}
          />
          <select
            aria-label="Filtrar por período"
            value={periodoSeleccionado}
            onChange={(event) => setPeriodoSeleccionado(event.target.value)}
          >
            <option value="">Todos los períodos</option>
            {periodos.map((periodo) => <option key={periodo} value={periodo}>{periodo}</option>)}
          </select>
          <select
            aria-label="Filtrar por sincronización"
            value={estadoSeleccionado}
            onChange={(event) => setEstadoSeleccionado(event.target.value)}
          >
            <option value="">Todos los estados</option>
            <option value="sincronizado">Sincronización completa</option>
            <option value="pendiente">Incompleta o sin sincronizar</option>
          </select>
          <Button variant="secondary" onClick={limpiarFiltros}>Limpiar filtros</Button>
        </section>

        {error && (
          <Alert variant="error">
            <p>{error}</p>
            <Button variant="secondary" onClick={() => window.location.reload()}>Reintentar</Button>
          </Alert>
        )}
        {errorOperacion && (
          <Alert variant="error">
            <p>{errorOperacion}</p>
            <Button variant="secondary" onClick={() => setErrorOperacion('')}>Cerrar</Button>
          </Alert>
        )}

        {cargando ? (
          <p role="status">Cargando cursos...</p>
        ) : cursosFiltrados.length === 0 ? (
          <div className="hu19-cursos__vacio">
            <h2>No hay cursos disponibles</h2>
            <p>Aún no tienes cursos</p>
          </div>
        ) : (
          <>
            <Table caption="Catálogo de cursos" columns={columnas} rows={cursosFiltrados} />
            {cursosFiltrados.map((curso) => (
              <DetalleCurso
                key={curso.id}
                curso={curso}
                resultado={resultadoPorCurso[curso.id]}
                historial={historialPorCurso[curso.id]}
                historialAbierto={historialAbiertoId === curso.id}
                cargandoHistorial={cargandoHistorialId === curso.id}
              />
            ))}
          </>
        )}
      </div>
    </Marco>
  )
}
