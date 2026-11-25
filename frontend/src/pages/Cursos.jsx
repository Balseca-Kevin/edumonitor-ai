import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { obtenerCursos, obtenerHistorialSincronizaciones, sincronizarCurso } from '../services/cursos'
import { contarEstudiantesConAlertas, obtenerIndicadores } from '../services/indicadores'
import { obtenerPeriodo } from '../services/periodo'
import Marco from '../components/Marco'
import Table from '../components/ui/Table'
import StatusBadge from '../components/ui/StatusBadge'
import Alert from '../components/ui/Alert'
import { formatearFecha, formatearNumero, tonoDeEstado } from '../components/formato'

const NO_DISPONIBLE = 'no disponible'

function mensajeError(error) {
  if (error?.response?.status === 403) return 'Este curso no está asignado a tu cuenta'
  if (error?.response?.status === 502) return 'Moodle no responde; intenta más tarde'
  return (
    error?.response?.data?.detail ||
    error?.response?.data?.title ||
    (error?.response ? 'No se pudo completar la operación.' : 'No se pudo conectar con el servidor. Intenta de nuevo.')
  )
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

/** HU-05 · Resultado de la última sincronización hecha desde la tarjeta y el historial del curso. */
function Historial({ curso, resultado, historial, cargando }) {
  const filas = [...(historial ?? [])].sort((a, b) => new Date(b.fin || b.inicio || 0) - new Date(a.fin || a.inicio || 0))
  const columnas = [
    { key: 'fecha', label: 'Fecha', render: (_v, item) => formatearFecha(item.fin || item.inicio) || NO_DISPONIBLE },
    { key: 'estado', label: 'Estado', render: (estado) => <StatusBadge status={estado} /> },
    {
      key: 'registros',
      label: 'Registros',
      render: (registros) => Object.values(registros || {}).reduce((total, cantidad) => total + (Number(cantidad) || 0), 0),
    },
    { key: 'errores', label: 'Errores', render: (errores) => (errores?.length ? <ErroresSincronizacion errores={errores} /> : 'Sin errores') },
  ]
  return (
    <section className="tarjeta-curso-detalle" aria-label={`Historial de ${curso.nombreCorto}`}>
      {resultado && ['PARCIAL', 'FALLIDA'].includes(String(resultado.estado).toUpperCase()) && (
        <Alert variant="warning" title="Sincronización incompleta">
          <p>La sincronización no se completó correctamente. Revisa los errores reportados.</p>
          <ErroresSincronizacion errores={resultado.errores} />
        </Alert>
      )}
      <h3>Historial de sincronizaciones</h3>
      {cargando ? (
        <p role="status">Cargando historial…</p>
      ) : filas.length === 0 ? (
        <p>No hay sincronizaciones registradas.</p>
      ) : (
        <Table caption={`Historial de sincronizaciones de ${curso.nombreCorto}`} columns={columnas} rows={filas} />
      )}
    </section>
  )
}

/**
 * R.3 · Tarjeta de un curso (docs/sprint-3/diseno.md): código y nombre, cuántos estudiantes requieren atención, promedio del curso,
 * entregas calificadas y estado de la última sincronización, con «Abrir curso» y «Sincronizar y abrir». Las cifras salen de la API
 * (indicadores y alertas del curso); un curso sin sincronizar lo dice, nunca muestra 0.
 */
/** Semana del período, unidad actual y lo que vence esta semana, tal como lo calcula GET /api/cursos/{id}/periodo (TA-22.A4). */
function LineaPeriodo({ periodo }) {
  const semana = periodo.semana
  const vence = periodo.venceEstaSemana ?? []
  return (
    <div className="tarjeta-curso-periodo">
      <div className="tarjeta-curso-periodo-fila">
        <span className="mono num">{semana ? `Semana ${semana.actual} de ${semana.total}` : 'Semana no disponible'}</span>
        {periodo.unidadActual && (
          <span className="tarjeta-curso-unidad">
            U{periodo.unidadActual.numero} · {periodo.unidadActual.nombre}
          </span>
        )}
      </div>
      {semana && (
        <div className="tarjeta-curso-avance" role="progressbar" aria-label="Avance del período" aria-valuemin={1} aria-valuemax={semana.total} aria-valuenow={semana.actual}>
          <div style={{ width: `${(semana.actual / semana.total) * 100}%` }} />
        </div>
      )}
      <span className="ayuda">{vence.length === 0 ? 'Nada vence esta semana' : `Vence esta semana: ${vence.map((a) => a.nombre).join(', ')}`}</span>
    </div>
  )
}

function TarjetaCurso({ curso, resumen, sincronizando, ocupado, onAbrir, onSincronizar, onHistorial, historialAbierto, children }) {
  const ultima = curso.ultimaSincronizacion
  const sinDatos = resumen?.sinDatos
  const promedio = formatearNumero(resumen?.promedio)
  return (
    <article className="tarjeta-curso hoja levanta" aria-label={`${curso.nombreCorto} · ${curso.nombre}`}>
      <div className="tarjeta-curso-cabeza">
        <div>
          <div className="mono">{curso.nombreCorto}</div>
          <h2>{curso.nombre}</h2>
        </div>
        {resumen?.atencion > 0 && <span className="chip-bajo num">{resumen.atencion} requieren atención</span>}
      </div>

      {sinDatos ? (
        <p className="ayuda">El curso todavía no tiene datos del aula virtual. Sincronízalo para ver sus notas.</p>
      ) : (
        <div className="tarjeta-curso-cifras">
          <div>
            <div className={`cifra-media${promedio ? '' : ' cifra--no-disponible'}`}>{resumen ? promedio ?? NO_DISPONIBLE : '…'}</div>
            <div className="ayuda">promedio del curso</div>
          </div>
          <div>
            <div className="cifra-media">{resumen ? resumen.calificadas ?? NO_DISPONIBLE : '…'}</div>
            <div className="ayuda">entregas calificadas</div>
          </div>
        </div>
      )}

      {resumen?.periodo && <LineaPeriodo periodo={resumen.periodo} />}

      <div className="tarjeta-curso-sync">
        <span className={`punto-estado punto-estado--${tonoDeEstado(ultima?.estado) ?? 'neutro'}`} aria-hidden="true" />
        <span>{ultima ? `Última sincronización: ${formatearFecha(ultima.fecha)} · ${ultima.estado}` : 'Sin sincronizar'}</span>
      </div>

      <div className="tarjeta-curso-acciones">
        <button type="button" className="boton tarjeta-curso-abrir" onClick={onAbrir}>
          Abrir curso
        </button>
        <button type="button" className="boton boton--secundario" onClick={onSincronizar} disabled={ocupado} aria-busy={sincronizando}>
          {sincronizando ? 'Sincronizando…' : 'Sincronizar y abrir'}
        </button>
        <button type="button" className="enlace" aria-expanded={historialAbierto} onClick={onHistorial}>
          {historialAbierto ? 'Ocultar historial' : 'Ver historial'}
        </button>
      </div>
      {children}
    </article>
  )
}

/** Resumen de la tarjeta: promedio y entregas calificadas del curso (indicadores), estudiantes con alertas activas y el estado del período. */
async function resumirCurso(curso) {
  if (!curso.ultimaSincronizacion) return { sinDatos: true }
  try {
    const [indicadores, atencion, periodo] = await Promise.all([
      obtenerIndicadores(curso.id),
      contarEstudiantesConAlertas(curso.id),
      obtenerPeriodo(curso.id).catch(() => null),
    ])
    const calificadas = indicadores.estudiantes.reduce((total, e) => total + (e.notas?.calificadas ?? 0), 0)
    return { promedio: indicadores.curso?.promedio ?? null, calificadas, atencion, periodo }
  } catch (error) {
    return error?.response?.status === 409 ? { sinDatos: true } : { promedio: null, calificadas: null, atencion: null }
  }
}

export default function Cursos() {
  const navigate = useNavigate()
  const [cursos, setCursos] = useState([])
  const [resumenes, setResumenes] = useState({})
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState('')
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
        if (!activo) return
        const lista = Array.isArray(datos) ? datos : []
        setCursos(lista)
        lista.forEach((curso) =>
          resumirCurso(curso).then((resumen) => activo && setResumenes((anterior) => ({ ...anterior, [curso.id]: resumen }))),
        )
      })
      .catch(() => activo && setError('No fue posible cargar los cursos'))
      .finally(() => activo && setCargando(false))
    return () => {
      activo = false
    }
  }, [])

  async function cargarHistorial(curso) {
    setCargandoHistorialId(curso.id)
    try {
      const historial = await obtenerHistorialSincronizaciones(curso.id)
      setHistorialPorCurso((anterior) => ({ ...anterior, [curso.id]: Array.isArray(historial) ? historial : [] }))
    } catch (err) {
      setErrorOperacion(`No se pudo cargar el historial: ${mensajeError(err)}`)
      setHistorialPorCurso((anterior) => ({ ...anterior, [curso.id]: [] }))
    } finally {
      setCargandoHistorialId(null)
    }
  }

  async function sincronizarYAbrir(curso) {
    if (sincronizandoId !== null) return
    setSincronizandoId(curso.id)
    setErrorOperacion('')
    try {
      const resultado = await sincronizarCurso(curso.id)
      setResultadoPorCurso((anterior) => ({ ...anterior, [curso.id]: resultado }))
      const estado = String(resultado?.estado ?? '').toUpperCase()
      const actualizado = { ...curso, ultimaSincronizacion: { id: resultado?.id, fecha: resultado?.fin || resultado?.inicio, estado } }
      setCursos((anteriores) => anteriores.map((item) => (item.id === curso.id ? actualizado : item)))
      if (estado === 'FALLIDA') {
        // No se importó nada: el docente se queda aquí y ve por qué en el historial.
        setHistorialAbiertoId(curso.id)
        await cargarHistorial(curso)
        return
      }
      navigate(`/cursos/${curso.id}/panel`)
    } catch (err) {
      setErrorOperacion(`No se pudo sincronizar "${curso.nombre || curso.nombreCorto}": ${mensajeError(err)}`)
    } finally {
      setSincronizandoId(null)
    }
  }

  async function alternarHistorial(curso) {
    if (historialAbiertoId === curso.id) {
      setHistorialAbiertoId(null)
      return
    }
    setHistorialAbiertoId(curso.id)
    setErrorOperacion('')
    if (!historialPorCurso[curso.id]) await cargarHistorial(curso)
  }

  return (
    <Marco>
      <div className="mis-cursos">
        <header className="mis-cursos-cabecera">
          <h1>Mis cursos</h1>
          <p>Solo aparecen los cursos asignados a tu usuario.</p>
        </header>

        {error && (
          <Alert variant="error">
            <p>{error}</p>
            <button type="button" className="boton boton--secundario" onClick={() => window.location.reload()}>
              Reintentar
            </button>
          </Alert>
        )}
        {errorOperacion && (
          <Alert variant="error">
            <p>{errorOperacion}</p>
            <button type="button" className="boton boton--secundario" onClick={() => setErrorOperacion('')}>
              Cerrar
            </button>
          </Alert>
        )}

        {cargando ? (
          <p role="status">Cargando cursos…</p>
        ) : cursos.length === 0 && !error ? (
          <div className="hu19-cursos__vacio">
            <h2>No hay cursos disponibles</h2>
            <p>Aún no tienes cursos</p>
          </div>
        ) : (
          <div className="mis-cursos-rejilla">
            {cursos.map((curso) => (
              <TarjetaCurso
                key={curso.id}
                curso={curso}
                resumen={resumenes[curso.id]}
                sincronizando={sincronizandoId === curso.id}
                ocupado={sincronizandoId !== null}
                historialAbierto={historialAbiertoId === curso.id}
                onAbrir={() => navigate(`/cursos/${curso.id}/panel`)}
                onSincronizar={() => sincronizarYAbrir(curso)}
                onHistorial={() => alternarHistorial(curso)}
              >
                {historialAbiertoId === curso.id && (
                  <Historial
                    curso={curso}
                    resultado={resultadoPorCurso[curso.id]}
                    historial={historialPorCurso[curso.id]}
                    cargando={cargandoHistorialId === curso.id}
                  />
                )}
              </TarjetaCurso>
            ))}
          </div>
        )}
      </div>
    </Marco>
  )
}
