import { useEffect, useRef, useState } from 'react'
import { Link, NavLink, useOutletContext, useParams } from 'react-router-dom'
import { Marca } from '../../components/Marco'
import { formatearFecha, formatearNumero } from '../../components/formato'
import { listarConversaciones, obtenerConversacion, preguntarAlAsistente } from '../../services/agente'
import { obtenerIndicadores } from '../../services/indicadores'
import { describirError } from '../../services/errores'
import { datoUsado, estudiantesMencionados, grupoDeFecha, motivo } from '../../components/lenguaje'

const LARGO_MAXIMO = 500
const BAJO = 6

const SUGERENCIAS = [
  { texto: '¿Qué estudiantes requieren atención?', ayuda: 'Alertas activas con su motivo' },
  { texto: '¿Quiénes tienen promedio bajo 7?', ayuda: 'Promedio de exámenes 40 %, tareas 30 % y APE 30 %' },
  { texto: '¿Quién tiene más actividades sin entregar?', ayuda: 'Entregas pendientes por estudiante' },
  { texto: '¿Qué cursos tengo asignados?', ayuda: 'Tus cursos en EduMonitor' },
]

function usuarioActual() {
  try {
    return JSON.parse(sessionStorage.getItem('edumonitor_usuario') ?? 'null')
  } catch {
    return null
  }
}

function TarjetasEstudiantes({ estudiantes }) {
  if (!estudiantes.length) return null
  return (
    <div className="tarjetas-estudiantes" aria-label="Estudiantes mencionados">
      {estudiantes.map((e) => {
        const promedio = e.promedioPonderado
        return (
          <div className="tarjeta-estudiante" key={e.id}>
            <b>
              {e.nombre} {e.apellido}
            </b>
            <div className="barra-nota">
              <div className="barra-nota-pista">
                <div
                  className={`barra-nota-valor${promedio !== null && promedio < BAJO ? ' barra-nota-valor--arena' : ''}`}
                  style={{ width: `${Math.max(0, Math.min(100, (promedio ?? 0) * 10))}%` }}
                />
              </div>
              <span>{motivo(e)}</span>
            </div>
            <span className="nota">{formatearNumero(promedio) ?? 'no disponible'}</span>
          </div>
        )
      })}
    </div>
  )
}

function RespuestaAsistente({ turno, cursoId, estudiantes, sincronizacion }) {
  const [verDatos, setVerDatos] = useState(false)
  const [copiado, setCopiado] = useState(false)
  const r = turno.respuesta
  const mencionados = estudiantesMencionados(r.respuesta, estudiantes)

  async function copiar() {
    try {
      await navigator.clipboard?.writeText(r.respuesta)
      setCopiado(true)
      setTimeout(() => setCopiado(false), 2000)
    } catch {
      setCopiado(false)
    }
  }

  return (
    <>
      <p className="msg-texto">{r.respuesta}</p>
      <TarjetasEstudiantes estudiantes={mencionados} />
      <div className="chat-acciones">
        <Link to={`/cursos/${cursoId}/panel`} className="chip">
          Abrir en el panel
        </Link>
        <button type="button" className="chip" onClick={copiar}>
          {copiado ? 'Copiado' : 'Copiar'}
        </button>
        <button type="button" className="chip chip--suave" aria-expanded={verDatos} onClick={() => setVerDatos((v) => !v)}>
          {verDatos ? 'Ocultar datos usados' : 'Datos usados'}
        </button>
      </div>
      {verDatos && (
        <ul className="chat-datos" aria-label="Datos usados">
          {r.herramientas?.length ? (
            r.herramientas.map((h, i) => <li key={`${h.nombre}-${i}`}>{datoUsado(h, estudiantes)}</li>)
          ) : (
            <li>Respuesta de EduMonitor sin consultar datos del curso.</li>
          )}
          {sincronizacion?.fecha && <li>Sincronización del {formatearFecha(sincronizacion.fecha)}</li>}
          <li>
            {r.modelo && r.modelo !== 'ninguno'
              ? `Respondió el modelo local ${r.modelo} en ${formatearNumero(r.duracionMs / 1000, 1)} s.`
              : 'EduMonitor respondió sin llamar al modelo.'}
          </li>
        </ul>
      )}
    </>
  )
}

function Turno({ turno, cursoId, estudiantes, sincronizacion }) {
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
            <RespuestaAsistente turno={turno} cursoId={cursoId} estudiantes={estudiantes} sincronizacion={sincronizacion} />
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

function Historial({ conversaciones, activa, abrir }) {
  const grupos = ['Hoy', 'Esta semana', 'Antes']
    .map((nombre) => ({ nombre, items: conversaciones.filter((c) => grupoDeFecha(c.fecha) === nombre) }))
    .filter((g) => g.items.length)
  if (!grupos.length) return <p className="chat-grupo chat-historial">Todavía no hay preguntas en este curso.</p>
  return (
    <nav className="chat-historial" aria-label="Historial de preguntas">
      {grupos.map((g) => (
        <div key={g.nombre}>
          <span className="chat-grupo">{g.nombre}</span>
          {g.items.map((c) => (
            <button
              key={c.id}
              type="button"
              className={`chat-hist${activa === c.id ? ' chat-hist--activo' : ''}`}
              title={c.pregunta}
              onClick={() => abrir(c.id)}
            >
              {c.pregunta}
            </button>
          ))}
        </div>
      ))}
    </nav>
  )
}

/**
 * HU-14 y HU-19 · Asistente como chat (diseño D, mockup MK-14): barra izquierda con la navegación del curso, «+ Nueva conversación»
 * y el historial guardado en la base (GET /api/agente/conversaciones); bienvenida con sugerencias; burbuja del docente y texto del
 * asistente con tarjetas de los estudiantes nombrados (sus números salen de los indicadores reales), «Abrir en el panel», «Copiar»
 * y «Datos usados» en palabras del docente. Mientras espera, «Consultando…»; un 503 se ve como «El asistente no está disponible».
 */
export default function Asistente() {
  const { id } = useParams()
  const { curso } = useOutletContext() ?? {}
  const usuario = usuarioActual()
  const [mensaje, setMensaje] = useState('')
  const [turnos, setTurnos] = useState([])
  const [activa, setActiva] = useState(null)
  const [historial, setHistorial] = useState([])
  const [estudiantes, setEstudiantes] = useState([])
  const cuerpo = useRef(null)
  const consultando = turnos.some((t) => t.cargando)

  useEffect(() => {
    let vigente = true
    listarConversaciones(id)
      .then((lista) => vigente && setHistorial(lista))
      .catch(() => vigente && setHistorial([]))
    obtenerIndicadores(id)
      .then((datos) => vigente && setEstudiantes(datos?.estudiantes ?? []))
      .catch(() => vigente && setEstudiantes([]))
    return () => {
      vigente = false
    }
  }, [id])

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
      setHistorial((h) => [
        { id: respuesta.conversacionId, fecha: new Date().toISOString(), pregunta },
        ...h.filter((c) => c.id !== respuesta.conversacionId),
      ])
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
          respuesta: { conversacionId: c.id, respuesta: c.respuesta, herramientas: c.herramientas, modelo: c.modelo, duracionMs: c.duracionMs },
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

  const preguntadas = new Set(turnos.map((t) => t.pregunta))
  const seguimiento = SUGERENCIAS.filter((s) => !preguntadas.has(s.texto)).slice(0, 3)
  const codigo = curso?.nombreCorto ?? 'este curso'

  return (
    <div className="chat">
      <aside className="chat-lateral" aria-label="Navegación del asistente">
        <div className="lateral-cabecera">
          <Marca />
          <span className="mono">{curso?.nombreCorto}</span>
        </div>
        <Link to="/cursos" className="suave">
          ← Mis cursos
        </Link>
        <NavLink to={`/cursos/${id}/panel`}>Panel</NavLink>
        <NavLink to={`/cursos/${id}/alertas`}>Alertas</NavLink>
        <NavLink to={`/cursos/${id}/asistente`} className="activo">
          Asistente
        </NavLink>
        <button type="button" className="chat-nueva" onClick={nueva} disabled={consultando}>
          <span aria-hidden="true">+</span> Nueva conversación
        </button>
        <Historial conversaciones={historial} activa={activa} abrir={abrir} />
        <div className="chat-lateral-pie">{usuario?.nombre}</div>
      </aside>

      <main className="chat-main">
        <header className="chat-cabecera">
          <div>
            <span className="titulo-chat">{turnos[0]?.pregunta ?? 'Nueva conversación'}</span>
            <span className="ayuda">
              {curso?.nombre}
              {curso?.ultimaSincronizacion?.fecha ? ` · datos del ${formatearFecha(curso.ultimaSincronizacion.fecha)}` : ''}
            </span>
          </div>
          <span className="chat-pildora">Solo lectura · modelo local</span>
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
              <Turno
                key={i}
                turno={turno}
                cursoId={id}
                estudiantes={estudiantes}
                sincronizacion={curso?.ultimaSincronizacion}
              />
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
            <span className="chat-nota">El asistente lee los datos del aula virtual y no modifica notas. Revisa antes de decidir.</span>
          </div>
        </div>
      </main>
    </div>
  )
}
