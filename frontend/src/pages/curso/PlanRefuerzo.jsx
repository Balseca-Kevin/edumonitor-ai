import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useOutletContext, useParams, useSearchParams } from 'react-router-dom'
import { formatearFecha, formatearNumero, formatearPorcentaje } from '../../components/formato'
import { textoResultadoRefuerzo } from '../../components/lenguaje'
import { obtenerMapeo, obtenerTematicas } from '../../services/tematicas'
import { crearPlan, ESTADO_PLAN, listarPlanes, nombrePlan, obtenerAplicados } from '../../services/refuerzos'
import { obtenerNotaNecesaria } from '../../services/notaNecesaria'
import { describirError } from '../../services/errores'

const DOMINA = 0.7

function textoNecesaria(n) {
  if (!n) return 'nota necesaria no disponible'
  switch (n.estado) {
    case 'NECESITA':
      return `necesita ${formatearNumero(n.notaNecesaria)} en lo pendiente para llegar a 7,0`
    case 'YA_ALCANZA':
      return 'ya llega a 7,0'
    case 'NO_ALCANZA':
      return `ya no alcanza 7,0 (máximo ${formatearNumero(n.maximoPosible)})`
    case 'SIN_PENDIENTES':
      return 'sin actividades pendientes'
    default:
      return 'sin notas para calcularlo'
  }
}

/** Historial de planes del curso con su estado y, en los aprobados, qué pasó con el tema (TA-15.A1). */
function HistorialPlanes({ planes, cursoId }) {
  if (planes.length === 0) return <p className="ayuda">Todavía no hay planes de refuerzo en este curso.</p>
  return (
    <ul className="plan-historial">
      {planes.map((p) => (
        <li key={p.id} className="hoja plan-historial-item">
          <div className="plan-historial-cabeza">
            <b>{nombrePlan(p)}</b>
            <span className={`chip-estado chip-estado--${p.estado.toLowerCase()}`}>{ESTADO_PLAN[p.estado] ?? p.estado}</span>
          </div>
          <span className="ayuda num">
            {formatearFecha(p.creado)} · {p.estudiantes.length} {p.estudiantes.length === 1 ? 'estudiante' : 'estudiantes'}
            {p.aprobadoEn ? ` · aprobado el ${formatearFecha(p.aprobadoEn)}` : ''}
          </span>
          {p.resultados.length > 0 && (
            <ul className="plan-resultados">
              {p.resultados.map((r) => (
                <li key={`${r.estudianteId}-${r.temaId}`} className={`plan-resultado plan-resultado--${r.estado.toLowerCase()}`}>
                  {textoResultadoRefuerzo(r)}
                </li>
              ))}
            </ul>
          )}
          <Link className="enlace" to={`/cursos/${cursoId}/asistente?refuerzo=${p.id}`}>
            {p.estado === 'APROBADO' ? 'Ver el plan y descargarlo' : 'Abrir en el asistente'}
          </Link>
        </li>
      ))}
    </ul>
  )
}

const CONTEO_APLICADOS = [
  ['subio', 'subieron'],
  ['igual', 'quedaron igual'],
  ['bajo', 'bajaron'],
  ['sinNotasNuevas', 'sin notas nuevas'],
]

/**
 * TA-15.A2 · Refuerzos aplicados: por estudiante y tema de cada refuerzo aprobado, la nota del tema al aprobar y la de hoy. Se dice el
 * cambio, no que el refuerzo lo causó. Cada fila abre la ficha del estudiante.
 */
function RefuerzosAplicados({ aplicados, cursoId }) {
  if (!aplicados) return <p className="ayuda">Refuerzos aplicados: no disponible.</p>
  if (aplicados.filas.length === 0) return <p className="ayuda">Todavía no hay refuerzos aprobados en este curso.</p>
  return (
    <>
      <p className="num" aria-label="Resultado de los refuerzos aprobados">
        {CONTEO_APLICADOS.map(([clave, texto]) => `${aplicados.conteos[clave]} ${texto}`).join(' · ')}
      </p>
      <ul className="plan-resultados">
        {aplicados.filas.map((r) => (
          <li key={`${r.planId}-${r.estudianteId}-${r.temaId}`} className={`plan-resultado plan-resultado--${r.estado.toLowerCase()}`}>
            <Link to={`/cursos/${cursoId}/estudiantes/${r.estudianteId}`}>{textoResultadoRefuerzo(r)}</Link>
            <span className="ayuda"> · {r.actividad}, aprobado el {formatearFecha(r.aprobadoEn)}</span>
          </li>
        ))}
      </ul>
    </>
  )
}

/**
 * HU-15 · TA-15.A3 · Plan de refuerzo (docs/sprint-3/diseno.md): la actividad evaluada, las temáticas donde falló el curso con el
 * porcentaje que domina cada tema y quién quedó bajo 6,0, los estudiantes, dónde se vuelven a evaluar esos temas y la nota necesaria
 * de cada uno. «Enviar el plan al asistente» lo guarda (POST /api/cursos/{id}/refuerzos) y abre el asistente con el plan como foco.
 * Todo sale de la API; desde la ficha llega con `?estudiante=`.
 */
export default function PlanRefuerzo() {
  const { id } = useParams()
  const navigate = useNavigate()
  const [parametros] = useSearchParams()
  const { version = 0 } = useOutletContext() ?? {}
  const pedidoEstudiante = Number(parametros.get('estudiante')) || null
  const [mapeo, setMapeo] = useState({ clave: null, datos: null, error: null })
  const [actividadElegida, setActividadElegida] = useState(Number(parametros.get('actividad')) || null)
  const [tematicas, setTematicas] = useState({ clave: null, datos: null })
  const [marcados, setMarcados] = useState({ clave: null, temas: null, fuera: [] })
  const [necesarias, setNecesarias] = useState({})
  const [planes, setPlanes] = useState([])
  const [aplicados, setAplicados] = useState(undefined)
  const pedidas = useRef(new Set())
  const [envio, setEnvio] = useState({ enviando: false, error: null })
  const claveMapeo = `${id}|${version}`

  useEffect(() => {
    let vigente = true
    obtenerMapeo(id)
      .then((datos) => vigente && setMapeo({ clave: claveMapeo, datos, error: null }))
      .catch((e) => vigente && setMapeo({ clave: claveMapeo, datos: null, error: describirError(e) }))
    listarPlanes(id)
      .then((lista) => vigente && setPlanes(lista))
      .catch(() => vigente && setPlanes([]))
    obtenerAplicados(id)
      .then((datos) => vigente && setAplicados(datos))
      .catch(() => vigente && setAplicados(null))
    return () => {
      vigente = false
    }
  }, [id, claveMapeo])

  const datosMapeo = mapeo.clave === claveMapeo ? mapeo.datos : null
  const actividades = useMemo(() => (datosMapeo?.actividades ?? []).filter((a) => a.temas.length > 0), [datosMapeo])
  const actividadId = actividades.some((a) => a.id === actividadElegida) ? actividadElegida : (actividades[0]?.id ?? null)
  const claveTemas = `${id}|${actividadId}|${version}`

  useEffect(() => {
    if (!actividadId) return
    let vigente = true
    obtenerTematicas(id, actividadId)
      .then((datos) => vigente && setTematicas({ clave: claveTemas, datos }))
      .catch(() => vigente && setTematicas({ clave: claveTemas, datos: null }))
    return () => {
      vigente = false
    }
  }, [id, actividadId, claveTemas])

  const temas = useMemo(() => (tematicas.clave === claveTemas ? (tematicas.datos?.temas ?? []) : []), [tematicas, claveTemas])
  // Por defecto se marcan los temas que menos del 70 % del curso domina.
  const temasMarcados =
    marcados.clave === claveTemas && marcados.temas ? marcados.temas : temas.filter((t) => t.domina !== null && t.domina < DOMINA).map((t) => t.id)
  const fuera = marcados.clave === claveTemas ? marcados.fuera : []

  const candidatos = useMemo(() => {
    const mapa = new Map()
    for (const t of temas.filter((x) => temasMarcados.includes(x.id))) {
      for (const e of t.estudiantes) {
        if (e.nota < 6 || e.id === pedidoEstudiante) {
          const actual = mapa.get(e.id) ?? { ...e, notas: [] }
          actual.notas.push({ tema: t.nombre, nota: e.nota })
          mapa.set(e.id, actual)
        }
      }
    }
    return [...mapa.values()].sort((a, b) => a.apellido.localeCompare(b.apellido))
  }, [temas, temasMarcados, pedidoEstudiante])
  const elegidos = candidatos.filter((e) => !fuera.includes(e.id))

  // Nota necesaria de cada candidato, una sola vez por estudiante.
  useEffect(() => {
    for (const e of candidatos) {
      if (pedidas.current.has(e.id)) continue
      pedidas.current.add(e.id)
      obtenerNotaNecesaria(id, e.id)
        .then((n) => setNecesarias((actual) => ({ ...actual, [e.id]: n })))
        .catch(() => setNecesarias((actual) => ({ ...actual, [e.id]: null })))
    }
  }, [candidatos, id])

  function alternarTema(temaId) {
    const nuevos = temasMarcados.includes(temaId) ? temasMarcados.filter((t) => t !== temaId) : [...temasMarcados, temaId]
    setMarcados({ clave: claveTemas, temas: nuevos, fuera })
  }

  function alternarEstudiante(estudianteId) {
    const nuevos = fuera.includes(estudianteId) ? fuera.filter((e) => e !== estudianteId) : [...fuera, estudianteId]
    setMarcados({ clave: claveTemas, temas: temasMarcados, fuera: nuevos })
  }

  async function enviar() {
    setEnvio({ enviando: true, error: null })
    try {
      const plan = await crearPlan(id, { actividadId, temas: temasMarcados, estudiantes: elegidos.map((e) => e.id) })
      navigate(`/cursos/${id}/asistente?refuerzo=${plan.id}`)
    } catch (e) {
      setEnvio({ enviando: false, error: describirError(e).detalle ?? 'No se pudo guardar el plan.' })
    }
  }

  if (mapeo.error) {
    return (
      <div className={`aviso ${mapeo.error.tipo === 'sin-sincronizar' ? 'aviso--atencion' : 'aviso--error'}`} role="alert">
        <strong>{mapeo.error.tipo === 'sin-sincronizar' ? 'Curso sin sincronizar' : mapeo.error.titulo}</strong>
        {mapeo.error.detalle}
      </div>
    )
  }
  if (!datosMapeo) return <p className="ayuda">Cargando el plan de refuerzo…</p>

  const proximas = [...new Map(temas.filter((t) => temasMarcados.includes(t.id)).flatMap((t) => t.proximas).map((p) => [p.actividadId, p])).values()]

  return (
    <section className="plan" aria-labelledby="titulo-plan">
      <div className="libro-cabecera">
        <div>
          <h2 id="titulo-plan">Plan de refuerzo</h2>
          <p className="ayuda">Elige la actividad evaluada, marca los temas donde falló el curso y envía el plan al asistente para que redacte el borrador.</p>
        </div>
      </div>

      {actividades.length === 0 ? (
        <div className="aviso aviso--atencion" role="status">
          <strong>Sin temas asociados</strong>
          <span>
            Ninguna actividad tiene temas todavía. <Link to={`/cursos/${id}/mapa`}>Asócialos en el Mapa del curso</Link>.
          </span>
        </div>
      ) : (
        <div className="plan-grilla">
          <div className="hoja plan-paso">
            <label className="plan-actividad">
              <span className="mono">ACTIVIDAD EVALUADA</span>
              <select value={actividadId ?? ''} onChange={(e) => setActividadElegida(Number(e.target.value))}>
                {actividades.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.nombre}
                  </option>
                ))}
              </select>
            </label>

            <fieldset className="plan-temas">
              <legend className="mono">TEMÁTICAS DONDE FALLÓ</legend>
              {temas.length === 0 && <p className="ayuda">Cargando los temas…</p>}
              {temas.map((t) => (
                <label key={t.id} className="plan-tema">
                  <input type="checkbox" checked={temasMarcados.includes(t.id)} onChange={() => alternarTema(t.id)} />
                  <span className="plan-tema-texto">
                    <b>{t.nombre}</b>
                    <span className="ayuda num">
                      {t.domina === null
                        ? 'sin notas en esta actividad'
                        : `domina ${formatearPorcentaje(t.domina)} del curso · ${t.bajo6.length} bajo 6,0${t.bajo6.length ? `: ${t.bajo6.map((b) => `${b.nombre} ${b.apellido} (${formatearNumero(b.nota)})`).join(', ')}` : ''}`}
                    </span>
                  </span>
                </label>
              ))}
            </fieldset>
          </div>

          <div className="hoja plan-paso">
            <fieldset className="plan-estudiantes">
              <legend className="mono">ESTUDIANTES</legend>
              {candidatos.length === 0 && <p className="ayuda">Nadie quedó bajo 6,0 en los temas marcados.</p>}
              {candidatos.map((e) => (
                <label key={e.id} className="plan-estudiante">
                  <input type="checkbox" checked={!fuera.includes(e.id)} onChange={() => alternarEstudiante(e.id)} />
                  <span>
                    <b>
                      {e.nombre} {e.apellido}
                    </b>
                    <span className="ayuda num">
                      {e.notas.map((n) => `${n.tema}: ${formatearNumero(n.nota)}`).join(' · ')} ·{' '}
                      {necesarias[e.id] === undefined ? 'calculando…' : textoNecesaria(necesarias[e.id])}
                    </span>
                  </span>
                </label>
              ))}
              <span className="ayuda">La nota necesaria es un cálculo con las notas actuales.</span>
            </fieldset>

            <div className="plan-donde">
              <span className="mono">DÓNDE SE VUELVEN A EVALUAR</span>
              {proximas.length === 0 ? (
                <p className="ayuda">Ninguna actividad futura evalúa estos temas.</p>
              ) : (
                <ul>
                  {proximas.map((p) => (
                    <li key={p.actividadId}>
                      {p.nombre} · vence {formatearFecha(p.fechaLimite)}
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <button
              type="button"
              className="boton boton--ia"
              onClick={enviar}
              disabled={envio.enviando || temasMarcados.length === 0 || elegidos.length === 0}
            >
              {envio.enviando ? 'Enviando…' : 'Enviar el plan al asistente'}
            </button>
            {envio.error && (
              <p className="ayuda mapa-error" role="alert">
                {envio.error}
              </p>
            )}
          </div>
        </div>
      )}

      <div className="plan-historial-seccion">
        <div>
          <h3>Refuerzos aplicados</h3>
          {aplicados === undefined ? <p className="ayuda">Cargando…</p> : <RefuerzosAplicados aplicados={aplicados} cursoId={id} />}
        </div>
        <div>
          <h3>Historial de refuerzos y retroalimentaciones</h3>
          <HistorialPlanes planes={planes} cursoId={id} />
        </div>
      </div>
    </section>
  )
}
