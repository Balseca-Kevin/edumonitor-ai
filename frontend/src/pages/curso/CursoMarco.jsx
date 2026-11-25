import { useEffect, useState } from 'react'
import { Outlet, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import Marco from '../../components/Marco'
import PaginaError from '../../components/PaginaError'
import TiraPeriodo from '../../components/TiraPeriodo'
import { formatearFecha } from '../../components/formato'
import { TODO, busquedaTramo, segmentosDe, useTramo } from '../../components/tramo'
import { obtenerCurso, sincronizarCurso } from '../../services/cursos'
import { contarEstudiantesConAlertas, obtenerIndicadores } from '../../services/indicadores'
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
 * R.2 · Barra blanca bajo la cabecera: el control segmentado (Todo el ciclo, cada parcial y cada unidad del curso) que filtra de
 * verdad el panel y las alertas, porque el backend calcula con las actividades del tramo; la fecha de la última sincronización,
 * «Sincronizar» y «Preguntar al asistente».
 */
export function BarraCurso({ curso, unidades, sincronizando, resultado, onSincronizar }) {
  const tramo = useTramo()
  const [parametros, setParametros] = useSearchParams()
  const navigate = useNavigate()
  const segmentos = segmentosDe(unidades)
  const ultima = curso.ultimaSincronizacion

  function elegir(codigo) {
    const siguientes = new URLSearchParams(parametros)
    if (codigo === TODO) siguientes.delete('tramo')
    else siguientes.set('tramo', codigo)
    setParametros(siguientes)
  }

  return (
    <div className="barra-curso">
      <div className="segmentado" role="group" aria-label="Filtrar por parcial o unidad">
        {segmentos.map((s) => (
          <button
            key={s.codigo}
            type="button"
            className="segmento"
            title={s.titulo}
            aria-pressed={tramo === s.codigo}
            onClick={() => elegir(s.codigo)}
          >
            {s.texto}
          </button>
        ))}
      </div>
      <span className="barra-curso-sync num" role="status">
        {sincronizando
          ? 'Sincronizando con el aula virtual…'
          : resultado
            ? resultado
            : ultima
              ? `Datos de la última sincronización · ${formatearFecha(ultima.fecha)}`
              : 'El curso todavía no se sincronizó'}
      </span>
      <button type="button" className="boton boton--secundario" onClick={onSincronizar} disabled={sincronizando}>
        {sincronizando ? 'Sincronizando…' : 'Sincronizar'}
      </button>
      <button type="button" className="boton boton--ia" onClick={() => navigate(`/cursos/${curso.id}/asistente${busquedaTramo(tramo)}`)}>
        Preguntar al asistente
      </button>
    </div>
  )
}

/**
 * HU-19 y R.2 · Estructura de las pantallas de un curso: barra lateral oscura con la navegación del curso y el contador de alertas,
 * cabecera oscura con el código, el nombre y la tira del período, y la barra con el control segmentado; el Asistente va dentro de la
 * misma estructura (R.6). 401, 403 y 404 se ven en pantalla completa, sin barra lateral.
 */
export default function CursoMarco() {
  const { id } = useParams()
  const tramo = useTramo()
  const [cargado, setCargado] = useState({ id: null, curso: null, error: null })
  const [contexto, setContexto] = useState({ id: null, unidades: [], alertas: null })
  const [version, setVersion] = useState(0)
  const [sincronizando, setSincronizando] = useState(false)
  const [resultado, setResultado] = useState(null)

  useEffect(() => {
    let vigente = true
    obtenerCurso(id)
      .then((datos) => vigente && setCargado({ id, curso: datos, error: null }))
      .catch((error) => vigente && setCargado({ id, curso: null, error: describirError(error) }))
    return () => {
      vigente = false
    }
  }, [id, version])

  // Unidades reales del curso para el control segmentado y estudiantes con alertas para el contador. Sin sincronizar (409) no hay
  // unidades y queda solo «Todo el ciclo»; el contador sin dato no se muestra, nunca como 0.
  useEffect(() => {
    let vigente = true
    Promise.all([obtenerIndicadores(id).catch(() => null), contarEstudiantesConAlertas(id)]).then(
      ([indicadores, alertas]) => vigente && setContexto({ id, unidades: indicadores?.unidades ?? [], alertas }),
    )
    return () => {
      vigente = false
    }
  }, [id, version])

  async function sincronizar() {
    setSincronizando(true)
    setResultado(null)
    try {
      const sync = await sincronizarCurso(id)
      setResultado(
        sync?.estado === 'COMPLETADA'
          ? 'Sincronización completada'
          : sync?.estado === 'PARCIAL'
            ? `Sincronización parcial: falló ${sync.errores?.map((e) => e.funcion).join(', ') || 'una función del aula virtual'}`
            : 'La sincronización falló; los datos guardados no cambiaron',
      )
      setVersion((v) => v + 1)
    } catch (error) {
      const descrito = describirError(error)
      setResultado(`${descrito.titulo}. ${descrito.detalle ?? ''}`.trim())
    } finally {
      setSincronizando(false)
    }
  }

  // Lo que llegó de otro curso no se muestra mientras carga el nuevo.
  const { curso, error } = cargado.id === id ? cargado : { curso: null, error: null }
  const { unidades, alertas } = contexto.id === id ? contexto : { unidades: [], alertas: null }

  if (error && ['sesion', 'acceso', 'no-existe'].includes(error.tipo)) {
    return <PaginaError error={error} />
  }

  return (
    <Marco
      curso={curso ?? { id }}
      alertas={alertas}
      busqueda={busquedaTramo(tramo)}
      cabecera={curso ? <CabeceraCurso curso={curso} /> : null}
      barra={
        curso ? (
          <BarraCurso curso={curso} unidades={unidades} sincronizando={sincronizando} resultado={resultado} onSincronizar={sincronizar} />
        ) : null
      }
    >
      {error && (
        <div className="aviso aviso--error" role="alert">
          <strong>{error.titulo}</strong>
          {error.detalle}
        </div>
      )}
      {!curso && !error && <p className="ayuda">Cargando el curso…</p>}
      {curso && <Outlet context={{ curso, version }} />}
    </Marco>
  )
}
