import { useCallback, useEffect, useRef, useState } from 'react'
import { useOutletContext, useParams, useSearchParams } from 'react-router-dom'
import FichaLateral from '../../components/FichaLateral'
import { formatearFecha, formatearNumero } from '../../components/formato'
import { nombreTramo, useTramo } from '../../components/tramo'
import { datoUsado, estudiantesMencionados, motivo, separarRespuesta } from '../../components/lenguaje'
import { listarConversaciones, obtenerConversacion, preguntarAlAsistente } from '../../services/agente'
import { obtenerIndicadores } from '../../services/indicadores'
import { obtenerAlertas } from '../../services/alertas'
import { actividadesDelLibro, obtenerLibro } from '../../services/libro'
import { describirError } from '../../services/errores'

const LARGO_MAXIMO = 500
const APROBACION = 7

const SUGERENCIAS = [
  { texto: '¿Qué estudiantes requieren atención?', ayuda: 'Alertas activas con su motivo' },
  { texto: '¿Quiénes tienen promedio bajo 7?', ayuda: 'Promedio de exámenes 40 %, tareas 30 % y APE 30 %' },
  { texto: '¿Quién tiene más actividades sin entregar?', ayuda: 'Entregas pendientes por estudiante' },
  { texto: '¿Qué cursos tengo asignados?', ayuda: 'Tus cursos en EduMonitor' },
]

/** Nombres de los parámetros de las consultas en palabras del docente (dentro de «Cómo llegó a esta respuesta»). */
const PARAMETRO = { cursoId: 'curso', estudianteId: 'estudiante', nombre: 'nombre', promedioMenorA: 'promedio menor a', orden: 'orden' }

function usuarioActual() {
  try {
    return JSON.parse(sessionStorage.getItem('edumonitor_usuario') ?? 'null')
  } catch {
    return null
  }
}

function Bloques({ texto }) {
  const partes = separarRespuesta(texto)
  if (!partes) return <p className="msg-texto">{texto}</p>
  return (
    <>
      {partes.antes && <p className="msg-texto">{partes.antes}</p>}
      <div className="respuesta-bloques num">
        {partes.hechos && (
          <div className="respuesta-bloque">
            <span>Hechos</span>
            <p>{partes.hechos}</p>
          </div>
        )}
        {partes.inferencia && (
          <div className="respuesta-bloque">
            <span>Inferencia</span>
            <p>{partes.inferencia}</p>
          </div>
        )}
        {partes.recomendacion && (
          <div className="respuesta-bloque respuesta-bloque--ia">
            <span>Recomendación</span>
            <p>{partes.recomendacion}</p>
          </div>
        )}
      </div>
    </>
  )
}

function Estudiantes({ estudiantes, onAbrir }) {
  if (!estudiantes.length) return null
  return (
    <div className="respuesta-estudiantes hoja" aria-label="Estudiantes mencionados">
      <div className="respuesta-estudiantes-titulo">Estudiantes mencionados</div>
      {estudiantes.map((e) => {
        const bajo = e.promedioPonderado !== null && e.promedioPonderado < APROBACION
        return (
          <div className="respuesta-estudiante num" key={e.id}>
            <b>
              {e.apellido}, {e.nombre}
            </b>
            <span className={`fuerte${bajo ? ' arena-texto' : ''}`}>{formatearNumero(e.promedioPonderado) ?? 'no disponible'}</span>
            <span className="ayuda">{motivo(e)}</span>
            <button type="button" className="boton boton--secundario boton--chico" onClick={() => onAbrir(e.id)}>
              Abrir ficha
            </button>
          </div>
        )
      })}
    </div>
  )
}

/** HU-17 · Desplegable cerrado: quién respondió, qué se consultó con qué parámetros, en cuánto tiempo y de qué sincronización. */
function ComoLlego({ respuesta, estudiantes, sincronizacion }) {
  const modelo = respuesta.modelo
  const quien =
    !modelo || modelo === 'ninguno'
      ? 'Respondió: EduMonitor, sin llamar al modelo'
      : respuesta.respaldo === false
        ? `Respondió: modelo externo (${modelo})`
        : respuesta.respaldo === true
          ? `Respondió: modelo local (respaldo) · ${modelo}`
          : `Respondió: modelo local · ${modelo}`
  return (
    <details className="como-llego">
      <summary>Cómo llegó a esta respuesta</summary>
      <ul>
        <li>{quien}</li>
        {(respuesta.herramientas ?? []).map((h, i) => {
          const parametros = Object.entries(h.parametros ?? {})
            .map(([clave, valor]) => `${PARAMETRO[clave] ?? clave} ${valor}`)
            .join(' · ')
          return (
            <li key={`${h.nombre}-${i}`}>
              {datoUsado(h, estudiantes)}
              {parametros && ` · ${parametros}`}
            </li>
          )
        })}
        {!respuesta.herramientas?.length && <li>Sin consultar datos del curso.</li>}
        {respuesta.duracionMs != null && <li>Tiempo total: {formatearNumero(respuesta.duracionMs / 1000, 1)} s</li>}
        {sincronizacion?.fecha && <li>Datos de la sincronización del {formatearFecha(sincronizacion.fecha)}</li>}
      </ul>
    </details>
  )
}

function Turno({ turno, estudiantes, sincronizacion, onAbrir }) {
  return (
    <div className="msg">
      <div className="msg-docente">{turno.pregunta}</div>
      <div className="msg-asistente">
        <div className="msg-icono" aria-hidden="true">
          <span />
          <span />
          <span />
          <span />
        </div>
        <div className="msg-cuerpo">
          {turno.cargando && (
            <div className="pensando" role="status">
              <span className="pensando-texto">Consultando…</span>
              <span className="pensando-puntos" aria-hidden="true">
                <span />
                <span />
                <span />
              </span>
            </div>
          )}
          {turno.respuesta && (
            <>
              <Bloques texto={turno.respuesta.respuesta} />
              <Estudiantes estudiantes={estudiantesMencionados(turno.respuesta.respuesta, estudiantes)} onAbrir={onAbrir} />
              <ComoLlego respuesta={turno.respuesta} estudiantes={estudiantes} sincronizacion={sincronizacion} />
            </>
          )}
          {turno.error && (
            <div className={`aviso aviso--${turno.error.tipo === 'no-disponible' ? 'neutro' : 'error'}`} role="alert">
              <strong>{turno.error.titulo}</strong>
              {turno.error.detalle ?? 'Intenta de nuevo en unos minutos.'}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

/**
 * Historial del asistente: «Planes de refuerzo» (TA-15.A4, cuando existan) y «Conversaciones» guardadas en la base
 * (GET /api/agente/conversaciones), de la más reciente a la más antigua, con su fecha.
 */
function Historial({ conversaciones, activa, abrir, nueva, consultando }) {
  return (
    <aside className="asistente-historial" aria-label="Historial del asistente">
      <button type="button" className="boton boton--ia" onClick={nueva} disabled={consultando}>
        Nueva conversación
      </button>
      <nav aria-label="Conversaciones">
        <span className="asistente-grupo mono">Conversaciones</span>
        {conversaciones.length === 0 ? (
          <p className="ayuda">Todavía no hay preguntas en este curso.</p>
        ) : (
          conversaciones.map((c) => (
            <button
              key={c.id}
              type="button"
              className="asistente-item"
              aria-current={activa === c.id ? 'true' : undefined}
              title={c.pregunta}
              onClick={() => abrir(c.id)}
            >
              <span>{c.pregunta}</span>
              {c.fecha && <span className="ayuda">{formatearFecha(c.fecha)}</span>}
            </button>
          ))
        )}
      </nav>
    </aside>
  )
}

/**
 * HU-14, HU-19 y R.6 · Asistente dentro del curso (docs/sprint-3/diseno.md): a la izquierda «Nueva conversación» y el historial; a
 * la derecha el chat con el contexto visible («Sabe: …» con el tramo y el estudiante elegidos), respuestas con Hechos, Inferencia y
 * Recomendación, tarjetas de los estudiantes nombrados con «Abrir ficha», y el desplegable cerrado «Cómo llegó a esta respuesta».
 * Desde una ficha llega con `?estudiante=` y la pregunta ya escrita. Un 503 se ve como «El asistente no está disponible».
 */
export default function Asistente() {
  const { id } = useParams()
  const tramo = useTramo()
  const [parametros] = useSearchParams()
  const { curso, periodo } = useOutletContext() ?? {}
  const usuario = usuarioActual()
  const [mensaje, setMensaje] = useState('')
  const [turnos, setTurnos] = useState([])
  const [activa, setActiva] = useState(null)
  const [historial, setHistorial] = useState([])
  const [indicadores, setIndicadores] = useState(null)
  const [conAlertas, setConAlertas] = useState(null)
  const [ficha, setFicha] = useState({ id: null, libro: null })
  const cuerpo = useRef(null)
  const consultando = turnos.some((t) => t.cargando)
  const estudiantes = indicadores?.estudiantes ?? []
  const enfocado = estudiantes.find((e) => e.id === Number(parametros.get('estudiante'))) ?? null

  useEffect(() => {
    let vigente = true
    listarConversaciones(id)
      .then((lista) => vigente && setHistorial(lista))
      .catch(() => vigente && setHistorial([]))
    const pedido = Number(parametros.get('estudiante'))
    obtenerIndicadores(id, tramo)
      .then((datos) => {
        if (!vigente) return
        setIndicadores(datos)
        // Desde la ficha: la pregunta sobre ese estudiante queda escrita para que el docente la revise y la envíe.
        const elegido = datos?.estudiantes?.find((e) => e.id === pedido)
        if (elegido) setMensaje((actual) => actual || `¿Cómo va ${elegido.nombre} ${elegido.apellido}? ¿En qué actividades le fue mal?`)
      })
      .catch(() => vigente && setIndicadores(null))
    obtenerAlertas(id, 'ACTIVA', tramo)
      .then((alertas) => vigente && setConAlertas(new Set(alertas.map((a) => a.estudiante?.id))))
      .catch(() => vigente && setConAlertas(null))
    return () => {
      vigente = false
    }
  }, [id, tramo, parametros])

  useEffect(() => {
    // Solo la columna de mensajes se desplaza: baja sola al último mensaje sin mover la página.
    const caja = cuerpo.current
    if (caja && typeof caja.scrollTo === 'function') caja.scrollTo({ top: caja.scrollHeight, behavior: 'smooth' })
  }, [turnos])

  async function enviar(texto) {
    const pregunta = texto.trim()
    if (!pregunta || consultando) return
    setMensaje('')
    setTurnos((t) => [...t, { pregunta, cargando: true }])
    try {
      const respuesta = await preguntarAlAsistente(id, pregunta)
      setTurnos((t) => [...t.slice(0, -1), { pregunta, respuesta }])
      setActiva(respuesta.conversacionId)
      const guardada = { id: respuesta.conversacionId, fecha: respuesta.fecha, pregunta }
      setHistorial((h) => [guardada, ...h.filter((c) => c.id !== respuesta.conversacionId)])
    } catch (error) {
      setTurnos((t) => [...t.slice(0, -1), { pregunta, error: describirError(error) }])
    }
  }

  async function abrir(conversacionId) {
    if (consultando) return
    setActiva(conversacionId)
    try {
      const c = await obtenerConversacion(conversacionId)
      setTurnos([
        {
          pregunta: c.pregunta,
          respuesta: { conversacionId: c.id, respuesta: c.respuesta, herramientas: c.herramientas, modelo: c.modelo, duracionMs: c.duracionMs, respaldo: c.respaldo },
        },
      ])
    } catch (error) {
      setTurnos([{ pregunta: 'Conversación guardada', error: describirError(error) }])
    }
  }

  function nueva() {
    if (consultando) return
    setTurnos([])
    setActiva(null)
    setMensaje('')
  }

  const abrirFicha = useCallback(
    (estudianteId) => {
      setFicha((f) => ({ ...f, id: estudianteId }))
      if (!ficha.libro) obtenerLibro(id, { tramo }).then((libro) => setFicha((f) => ({ ...f, libro }))).catch(() => {})
    },
    [ficha.libro, id, tramo],
  )
  const cerrarFicha = useCallback(() => setFicha((f) => ({ ...f, id: null })), [])

  const preguntadas = new Set(turnos.map((t) => t.pregunta))
  const seguimiento = SUGERENCIAS.filter((s) => !preguntadas.has(s.texto)).slice(0, 3)
  const codigo = curso?.nombreCorto ?? 'este curso'
  const semanaPeriodo = periodo?.semana ? `semana ${periodo.semana.actual} de ${periodo.semana.total}` : null
  const sabe = [semanaPeriodo, nombreTramo(tramo, indicadores?.unidades), enfocado ? `${enfocado.nombre} ${enfocado.apellido}` : null].filter(Boolean).join(' · ')
  const estudianteFicha = estudiantes.find((e) => e.id === ficha.id) ?? null
  const columnas = actividadesDelLibro(ficha.libro)
  const filaLibro = ficha.libro?.estudiantes.find((e) => e.id === ficha.id)
  const celdas = filaLibro ? columnas.map((a, i) => ({ ...filaLibro.notas[i], nombre: a.nombre, fechaLimite: a.fechaLimite })) : []

  return (
    <div className="asistente">
      <Historial conversaciones={historial} activa={activa} abrir={abrir} nueva={nueva} consultando={consultando} />

      <section className="asistente-chat hoja" aria-label="Conversación con el asistente">
        <header className="asistente-cabecera">
          <span className="fuerte">{turnos[0]?.pregunta ?? 'Nueva conversación'}</span>
          <span className="asistente-contexto">Sabe: {sabe}</span>
        </header>

        <div className="chat-cuerpo" ref={cuerpo}>
          <div className="chat-hilo" aria-live="polite">
            {turnos.length === 0 && (
              <div className="chat-bienvenida">
                <h1>
                  Hola, <b>{usuario?.nombre ?? 'docente'}</b>. ¿Qué quieres saber de {codigo}?
                </h1>
                <div className="chat-sugerencias">
                  {SUGERENCIAS.map((s) => (
                    <button key={s.texto} type="button" className="chat-sugerencia" onClick={() => enviar(s.texto)}>
                      <strong>{s.texto}</strong>
                      <span>{s.ayuda}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}
            {turnos.map((turno, i) => (
              <Turno key={i} turno={turno} estudiantes={estudiantes} sincronizacion={curso?.ultimaSincronizacion} onAbrir={abrirFicha} />
            ))}
          </div>
        </div>

        <div className="chat-pie">
          <div>
            {turnos.length > 0 && (
              <div className="chat-seguimiento" aria-label="Preguntas de seguimiento">
                {seguimiento.map((s) => (
                  <button key={s.texto} type="button" className="chip" onClick={() => enviar(s.texto)} disabled={consultando}>
                    {s.texto}
                  </button>
                ))}
              </div>
            )}
            <form
              className="chat-form"
              onSubmit={(e) => {
                e.preventDefault()
                enviar(mensaje)
              }}
            >
              <span className="chat-curso">{curso?.nombreCorto}</span>
              <label htmlFor="pregunta" className="sr-only">
                Tu pregunta
              </label>
              <input
                id="pregunta"
                className="chat-input"
                maxLength={LARGO_MAXIMO}
                value={mensaje}
                onChange={(e) => setMensaje(e.target.value)}
                placeholder="Pregunta sobre notas, entregas o alertas del curso"
                autoComplete="off"
              />
              <button type="submit" className="chat-enviar" aria-label="Enviar pregunta" disabled={consultando || !mensaje.trim()}>
                ↑
              </button>
            </form>
          </div>
        </div>
      </section>

      {estudianteFicha && (
        <FichaLateral
          cursoId={id}
          estudiante={estudianteFicha}
          requiereAtencion={conAlertas ? conAlertas.has(estudianteFicha.id) : null}
          celdas={celdas}
          tramo={tramo}
          nombreTramo={nombreTramo(tramo, indicadores?.unidades)}
          onCerrar={cerrarFicha}
        />
      )}
    </div>
  )
}
