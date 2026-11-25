import { useEffect, useState } from 'react'
import Marco from '../../components/Marco'
import PaginaError from '../../components/PaginaError'
import Estado from '../../components/Estado'
import { formatearFecha, formatearNumero } from '../../components/formato'
import { obtenerCursos } from '../../services/cursos'
import { iniciarEvaluacion, listarEvaluaciones, obtenerEvaluacion } from '../../services/evaluacion'
import { describirError } from '../../services/errores'

const MODOS = [
  { clave: 'EXTERNO', nombre: 'Modelo externo' },
  { clave: 'LOCAL', nombre: 'Modelo local' },
]
const ESTADO = { EN_CURSO: 'En curso', TERMINADA: 'Terminada', FALLIDA: 'Falló' }
const CADA_MS = 3000

function segundos(ms) {
  return ms === null || ms === undefined ? '—' : `${formatearNumero(ms / 1000, 1)} s`
}

/** Celda de un modelo en una pregunta: aprobó, no aprobó, no respondió o todavía no llega. */
function Resultado({ r }) {
  if (!r) return <span className="ayuda">pendiente</span>
  if (!r.respondio) return <Estado estado="ERROR" texto="No respondió" />
  return (
    <div className="calidad-celda">
      <Estado estado={r.aprobada ? 'OK' : 'ERROR'} texto={r.aprobada ? 'Aprobó' : 'No aprobó'} />
      <span className="ayuda num">
        {segundos(r.duracionMs)}
        {r.cifrasTotal > 0 ? ` · cifras ${r.cifrasVerificadas}/${r.cifrasTotal}` : ''}
      </span>
      <details>
        <summary>Ver respuesta</summary>
        <p className="ayuda">{r.motivo}</p>
        <p className="ayuda mono">{r.herramientas || 'sin herramientas'}</p>
        <p className="calidad-respuesta">{r.respuesta}</p>
      </details>
    </div>
  )
}

/**
 * HU-18 · TA-18.A2 · «Calidad del asistente» (solo ADMIN): corre las 18 preguntas (10 de TA-13.7, 5 de seguimiento y 3 fuera de alcance)
 * con el modelo externo y con el local, y muestra por pregunta si aprobó, con qué herramienta, cuántas cifras quedaron verificadas y en
 * cuánto tiempo. Mientras corre, la tabla se actualiza sola.
 */
export default function CalidadAsistente() {
  const [cursos, setCursos] = useState([])
  const [cursoId, setCursoId] = useState('')
  const [lista, setLista] = useState(null)
  const [elegida, setElegida] = useState(null)
  const [detalle, setDetalle] = useState(null)
  const [error, setError] = useState(null)
  const [iniciando, setIniciando] = useState(false)

  useEffect(() => {
    obtenerCursos()
      .then((datos) => {
        setCursos(datos)
        const agil = datos.find((c) => c.nombreCorto === 'AGIL-4B') ?? datos[0]
        if (agil) setCursoId(String(agil.id))
      })
      .catch((e) => setError(describirError(e)))
    listarEvaluaciones()
      .then((datos) => {
        setLista(datos)
        if (datos[0]) setElegida(datos[0].id)
      })
      .catch((e) => setError(describirError(e)))
  }, [])

  // El detalle de la evaluación elegida; si está en curso, se vuelve a pedir cada 3 s.
  useEffect(() => {
    if (!elegida) return
    let vigente = true
    let reloj = null
    const pedir = () =>
      obtenerEvaluacion(elegida)
        .then((datos) => {
          if (!vigente) return
          setDetalle(datos)
          setLista((actual) => (actual ?? []).map((e) => (e.id === datos.id ? { ...e, estado: datos.estado, resumen: datos.resumen } : e)))
          if (datos.estado === 'EN_CURSO') reloj = setTimeout(pedir, CADA_MS)
        })
        .catch((e) => vigente && setError(describirError(e)))
    pedir()
    return () => {
      vigente = false
      if (reloj) clearTimeout(reloj)
    }
  }, [elegida])

  async function correr() {
    setIniciando(true)
    setError(null)
    try {
      const nueva = await iniciarEvaluacion(cursoId)
      setLista((actual) => [{ ...nueva }, ...(actual ?? [])])
      setElegida(nueva.id)
    } catch (e) {
      setError(describirError(e))
    } finally {
      setIniciando(false)
    }
  }

  if (error && ['sesion', 'acceso'].includes(error.tipo)) return <PaginaError error={error} />

  const enCurso = (lista ?? []).some((e) => e.estado === 'EN_CURSO')
  const resultadoDe = (numero, modo) => detalle?.resultados.find((r) => r.numero === numero && r.modo === modo)

  return (
    <Marco>
      <header className="cabecera">
        <div>
          <p className="sobretitulo">Administración</p>
          <h1 className="titulo">Calidad del asistente</h1>
        </div>
      </header>

      <section className="seccion calidad-acciones" aria-label="Correr evaluación">
        <p className="ayuda">
          18 preguntas: las 10 de la evaluación del Sprint 2, 5 de seguimiento que siguen el hilo de una anterior y 3 fuera de alcance. Cada
          una se hace con el modelo externo y con el local. Aprueba si usa la herramienta esperada y todas sus cifras están en los datos; las
          fuera de alcance aprueban si se responden sin llamar al modelo.
        </p>
        <label>
          Curso
          <select value={cursoId} onChange={(e) => setCursoId(e.target.value)}>
            {cursos.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nombreCorto} · {c.nombre}
              </option>
            ))}
          </select>
        </label>
        <button type="button" className="boton boton--ia" onClick={correr} disabled={iniciando || enCurso || !cursoId}>
          {enCurso ? 'Evaluación en curso…' : iniciando ? 'Iniciando…' : 'Correr evaluación'}
        </button>
      </section>

      {error && (
        <div className="aviso aviso--error" role="alert">
          <strong>{error.titulo}</strong>
          {error.detalle}
        </div>
      )}

      {lista && lista.length > 1 && (
        <label className="calidad-historial">
          Evaluación
          <select value={elegida ?? ''} onChange={(e) => setElegida(Number(e.target.value))}>
            {lista.map((e) => (
              <option key={e.id} value={e.id}>
                {formatearFecha(e.inicio)} · {e.curso} · {ESTADO[e.estado] ?? e.estado}
              </option>
            ))}
          </select>
        </label>
      )}

      {lista && lista.length === 0 && <p className="ayuda">Todavía no se corrió ninguna evaluación.</p>}

      {detalle && (
        <section className="seccion" aria-labelledby="titulo-resultados">
          <div className="regla" />
          <h2 id="titulo-resultados" className="seccion-titulo">
            {detalle.curso} · {formatearFecha(detalle.inicio)} · {ESTADO[detalle.estado] ?? detalle.estado}
          </h2>
          {detalle.detalle && <p className="ayuda">{detalle.detalle}</p>}
          <div className="calidad-resumen">
            {detalle.resumen.map((r) => (
              <div key={r.modo} className="hoja calidad-modo" aria-label={`Resumen ${r.modo === 'EXTERNO' ? 'modelo externo' : 'modelo local'}`}>
                <b>{r.modo === 'EXTERNO' ? 'Modelo externo' : 'Modelo local'}</b>
                <span className="num">
                  {r.aprobadas} de {r.preguntas} aprobadas · {r.respondidas} respondidas · promedio {segundos(r.duracionPromedioMs)}
                </span>
                {r.conResultado < r.preguntas && detalle.estado === 'EN_CURSO' && (
                  <span className="ayuda">
                    {r.conResultado} de {r.preguntas} preguntas hechas
                  </span>
                )}
              </div>
            ))}
          </div>
          <div className="tabla-envoltura">
            <table className="tabla" aria-label="Resultados por pregunta">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Tipo</th>
                  <th>Pregunta</th>
                  {MODOS.map((m) => (
                    <th key={m.clave}>{m.nombre}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {detalle.preguntas.map((p) => (
                  <tr key={p.numero}>
                    <td className="num">{p.numero}</td>
                    <td>
                      {p.tipo}
                      {p.seguimientoDe ? <span className="ayuda"> · sigue a la {p.seguimientoDe}</span> : null}
                    </td>
                    <td>{p.texto}</td>
                    {MODOS.map((m) => (
                      <td key={m.clave}>
                        <Resultado r={resultadoDe(p.numero, m.clave)} />
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </Marco>
  )
}
