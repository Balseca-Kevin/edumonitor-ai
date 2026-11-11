import { useState } from 'react'
import { useOutletContext, useParams } from 'react-router-dom'
import Estado from '../../components/s2/Estado'
import { preguntarAlAsistente } from '../../services/agente'
import { describirError } from '../../services/errores'

const LARGO_MAXIMO = 500

const SUGERENCIAS = [
  '¿Qué estudiantes requieren atención?',
  '¿Quiénes tienen notas en cero o actividades sin entregar?',
  '¿Qué cursos tengo asignados?',
]

function textoParametros(parametros) {
  if (!parametros || typeof parametros !== 'object') return '—'
  const pares = Object.entries(parametros).map(([clave, valor]) => `${clave}=${valor}`)
  return pares.length ? pares.join(', ') : '—'
}

function HerramientasUsadas({ herramientas }) {
  if (!herramientas?.length) {
    return <p className="s2-ayuda">El asistente no usó herramientas en esta respuesta.</p>
  }

  return (
    <div className="s2-tabla-envoltura">
      <table className="s2-tabla" aria-label="Herramientas usadas">
        <thead>
          <tr>
            <th>Herramienta</th>
            <th>Parámetros</th>
            <th>Estado</th>
            <th className="s2-num">Duración (ms)</th>
          </tr>
        </thead>
        <tbody>
          {herramientas.map((h, i) => (
            <tr key={`${h.nombre}-${i}`}>
              <td>{h.nombre}</td>
              <td>
                <code>{textoParametros(h.parametros)}</code>
              </td>
              <td>
                <Estado estado={h.estado} />
              </td>
              <td className="s2-num">{h.duracionMs}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function AvisoError({ error }) {
  const tono = error.tipo === 'no-disponible' ? 'neutro' : 'error'
  return (
    <div className={`s2-aviso s2-aviso--${tono}`} role="alert">
      <strong>{error.titulo}</strong>
      {error.detalle ?? 'Intenta de nuevo en unos minutos.'}
    </div>
  )
}

/**
 * HU-14 · Panel «Asistente» (TA-14.3): pregunta, respuesta y la lista de herramientas usadas con su estado y duración.
 * Mientras espera muestra «Consultando…» y deshabilita el botón; un 503 se ve como «El asistente no está disponible».
 */
export default function Asistente() {
  const { id } = useParams()
  const { curso } = useOutletContext() ?? {}
  const [mensaje, setMensaje] = useState('')
  const [conversacion, setConversacion] = useState([])
  const [consultando, setConsultando] = useState(false)
  const [pendiente, setPendiente] = useState('')

  async function enviar(evento) {
    evento.preventDefault()
    const pregunta = mensaje.trim()
    if (!pregunta || consultando) return

    setConsultando(true)
    setPendiente(pregunta)
    setMensaje('')
    try {
      const respuesta = await preguntarAlAsistente(id, pregunta)
      setConversacion((anterior) => [...anterior, { pregunta, respuesta }])
    } catch (error) {
      setConversacion((anterior) => [...anterior, { pregunta, error: describirError(error) }])
    } finally {
      setConsultando(false)
      setPendiente('')
    }
  }

  return (
    <section className="s2-seccion" aria-labelledby="titulo-asistente">
      <h2 id="titulo-asistente" className="s2-seccion-titulo">
        Asistente
      </h2>
      <p className="s2-ayuda s2-intro">
        Pregunta sobre {curso?.nombreCorto ?? 'este curso'}. El asistente consulta los datos de EduMonitor con herramientas y
        separa hechos, inferencias y recomendaciones; no modifica notas.
      </p>

      <div className="s2-conversacion" aria-live="polite">
        {conversacion.map((turno, indice) => (
          <article key={indice} className="s2-conversacion">
            <div className="s2-mensaje s2-mensaje--docente">
              <span className="s2-mensaje-autor">Docente</span>
              <p className="s2-mensaje-texto">{turno.pregunta}</p>
            </div>
            {turno.respuesta && (
              <div className="s2-mensaje s2-mensaje--asistente">
                <span className="s2-mensaje-autor">Asistente</span>
                <p className="s2-mensaje-texto">{turno.respuesta.respuesta}</p>
                <HerramientasUsadas herramientas={turno.respuesta.herramientas} />
                <p className="s2-ayuda s2-num">
                  Modelo {turno.respuesta.modelo || 'no informado'} · {turno.respuesta.duracionMs} ms · conversación{' '}
                  {turno.respuesta.conversacionId}
                </p>
              </div>
            )}
            {turno.error && <AvisoError error={turno.error} />}
          </article>
        ))}

        {consultando && (
          <div className="s2-conversacion">
            <div className="s2-mensaje s2-mensaje--docente">
              <span className="s2-mensaje-autor">Docente</span>
              <p className="s2-mensaje-texto">{pendiente}</p>
            </div>
            <p className="s2-ayuda" role="status">
              Consultando…
            </p>
          </div>
        )}
      </div>

      <form className="s2-formulario" onSubmit={enviar}>
        <label htmlFor="pregunta" className="s2-mensaje-autor">
          Tu pregunta
        </label>
        <textarea
          id="pregunta"
          className="s2-campo"
          rows={3}
          maxLength={LARGO_MAXIMO}
          value={mensaje}
          onChange={(e) => setMensaje(e.target.value)}
          placeholder="Por ejemplo: ¿Qué estudiantes requieren atención?"
        />
        <div className="s2-sugerencias">
          {SUGERENCIAS.map((sugerencia) => (
            <button
              key={sugerencia}
              type="button"
              className="s2-boton s2-boton--secundario s2-boton--chico"
              onClick={() => setMensaje(sugerencia)}
              disabled={consultando}
            >
              {sugerencia}
            </button>
          ))}
        </div>
        <div className="s2-formulario-pie">
          <span className="s2-ayuda s2-num">
            {mensaje.length}/{LARGO_MAXIMO}
          </span>
          <button type="submit" className="s2-boton" disabled={consultando || !mensaje.trim()}>
            {consultando ? 'Consultando…' : 'Preguntar'}
          </button>
        </div>
      </form>
    </section>
  )
}
