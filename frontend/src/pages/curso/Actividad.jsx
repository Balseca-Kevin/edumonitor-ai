import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Bar, BarChart, Cell, LabelList, ReferenceLine, ResponsiveContainer, XAxis, YAxis } from 'recharts'
import { formatearFecha, formatearNumero, formatearPorcentaje } from '../../components/formato'
import { sinMovimiento } from '../../components/movimiento'
import { describirError } from '../../services/errores'
import { obtenerActividad } from '../../services/actividades'
import { crearPlan } from '../../services/refuerzos'
import { celda } from './Libro'

const APROBACION = 7
const ESTADO_PERIODO = { VENCIDA: 'Vencida', VENCE_ESTA_SEMANA: 'Vence esta semana', PROXIMA: 'Próxima', SIN_FECHA: 'Sin fecha límite' }

function tamano(bytes) {
  if (!bytes) return ''
  return bytes < 1024 * 1024 ? `${formatearNumero(bytes / 1024, 0)} KB` : `${formatearNumero(bytes / (1024 * 1024), 1)} MB`
}

/** Distribución de las notas de la actividad en 10 tramos con la línea de 7,0; los tramos bajo 7,0 en arena. */
function Distribucion({ distribucion }) {
  const datos = distribucion.map((t) => ({ tramo: String(t.desde), desde: t.desde, cantidad: t.cantidad }))
  const quieto = sinMovimiento()
  return (
    <div className="histograma" role="img" aria-label="Estudiantes por tramo de nota en esta actividad, con la línea de 7,0">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={datos} margin={{ top: 22, right: 4, bottom: 0, left: 0 }} barCategoryGap={4}>
          <XAxis dataKey="tramo" tickLine={false} axisLine={{ stroke: 'var(--tinta)' }} />
          <YAxis hide allowDecimals={false} />
          <ReferenceLine x={String(APROBACION)} className="linea-aprobacion" position="start" label={{ value: '7,0 aprueba', position: 'insideTopLeft' }} />
          <Bar dataKey="cantidad" isAnimationActive={!quieto} animationDuration={800}>
            <LabelList dataKey="cantidad" position="top" className="histograma-cantidad" />
            {datos.map((t) => (
              <Cell key={t.tramo} fill={t.desde < APROBACION ? 'var(--arena)' : 'var(--grafico)'} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

/**
 * HU-22 · Panel de actividad: el enunciado y los adjuntos como están en Moodle, las entregas, la distribución de notas, cuánto domina
 * el curso cada criterio de la rúbrica, los temas que evalúa y quién requiere atención. Desde aquí se pregunta al asistente con la
 * actividad cargada, se arma el plan de refuerzo o se redacta la retroalimentación de los estudiantes elegidos.
 */
export default function Actividad() {
  const { id, aid } = useParams()
  const navigate = useNavigate()
  const clave = `${id}|${aid}`
  const [cargado, setCargado] = useState({ clave: null, datos: null, error: null })
  const [elegidos, setElegidos] = useState({ clave: null, ids: null })
  const [envio, setEnvio] = useState({ enviando: false, error: null })

  useEffect(() => {
    let vigente = true
    obtenerActividad(id, aid)
      .then((datos) => vigente && setCargado({ clave, datos, error: null }))
      .catch((e) => vigente && setCargado({ clave, datos: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id, aid, clave])

  const { datos, error } = cargado.clave === clave ? cargado : { datos: null, error: null }
  if (error) {
    return (
      <div className={`aviso aviso--${error.tipo === 'sin-sincronizar' ? 'atencion' : 'error'}`} role="alert">
        <strong>{error.tipo === 'sin-sincronizar' ? 'Curso sin sincronizar' : error.titulo}</strong>
        {error.detalle}
      </div>
    )
  }
  if (!datos) return <p className="ayuda">Cargando la actividad…</p>

  const a = datos.actividad
  const r = datos.resumen
  const conNota = datos.estudiantes.filter((e) => e.estado === 'CALIFICADO')
  // Por defecto se eligen para la retroalimentación quienes quedaron bajo 7,0.
  const seleccion = elegidos.clave === clave && elegidos.ids ? elegidos.ids : conNota.filter((e) => e.nota < APROBACION).map((e) => e.id)

  function alternar(estudianteId) {
    const nuevos = seleccion.includes(estudianteId) ? seleccion.filter((x) => x !== estudianteId) : [...seleccion, estudianteId]
    setElegidos({ clave, ids: nuevos })
  }

  async function redactarRetroalimentacion() {
    setEnvio({ enviando: true, error: null })
    try {
      const plan = await crearPlan(id, { actividadId: a.id, temas: [], estudiantes: seleccion, tipo: 'RETROALIMENTACION' })
      navigate(`/cursos/${id}/asistente?refuerzo=${plan.id}`)
    } catch (e) {
      setEnvio({ enviando: false, error: describirError(e).detalle ?? 'No se pudo guardar la retroalimentación.' })
    }
  }

  return (
    <div className="actividad-pagina">
      <header className="hoja actividad-cabecera">
        <div>
          <span className="mono">
            {a.sigla ?? 'ACTIVIDAD'} · {a.categoria ?? 'sin categoría'}
            {a.unidad ? ` · U${a.unidad.numero} · PARCIAL ${a.unidad.parcial}` : ' · sin unidad'}
          </span>
          <h1>{a.nombre}</h1>
          <span className="ayuda num">
            {a.fechaLimite ? `Vence ${formatearFecha(a.fechaLimite)}` : 'Sin fecha límite'}
            {a.semana ? ` · semana ${a.semana}` : ''}
            {a.estadoPeriodo ? ` · ${ESTADO_PERIODO[a.estadoPeriodo] ?? a.estadoPeriodo}` : ''}
          </span>
        </div>
        <div className="ficha-acciones ficha-acciones--fila">
          <button type="button" className="boton boton--ia" onClick={() => navigate(`/cursos/${id}/asistente?actividad=${a.id}`)}>
            Analizar con el asistente
          </button>
          {datos.temas.length > 0 && (
            <button type="button" className="boton boton--secundario" onClick={() => navigate(`/cursos/${id}/refuerzos?actividad=${a.id}`)}>
              Armar plan de refuerzo
            </button>
          )}
          <Link className="boton boton--secundario" to={`/cursos/${id}/libro`}>
            Volver al libro
          </Link>
        </div>
      </header>

      <div className="ficha-pagina-columnas">
        <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-enunciado">
          <h2 id="titulo-enunciado">Enunciado</h2>
          {a.enunciado ? <p className="actividad-enunciado">{a.enunciado}</p> : <p className="ayuda">El aula virtual no tiene enunciado para esta actividad.</p>}
          {a.archivos.length > 0 && (
            <ul className="actividad-archivos" aria-label="Archivos del enunciado">
              {a.archivos.map((f) => (
                <li key={f.nombre}>
                  {f.nombre} <span className="ayuda">{tamano(f.tamano)}</span>
                </li>
              ))}
            </ul>
          )}
          {a.archivos.length > 0 && <p className="ayuda">Los archivos se abren en el aula virtual.</p>}
        </section>
        <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-entregas">
          <h2 id="titulo-entregas">Entregas</h2>
          <dl className="actividad-entregas num">
            <div>
              <dt>Matriculados</dt>
              <dd>{r.matriculados}</dd>
            </div>
            <div>
              <dt>Calificadas</dt>
              <dd>{r.calificadas}</dd>
            </div>
            <div>
              <dt>Con cero</dt>
              <dd>{r.ceros}</dd>
            </div>
            <div>
              <dt>Por calificar</dt>
              <dd>{r.porCalificar}</dd>
            </div>
            <div>
              <dt>Sin entregar</dt>
              <dd>{r.sinEntrega}</dd>
            </div>
            <div>
              <dt>Aún no vence</dt>
              <dd>{r.aunNoVence}</dd>
            </div>
          </dl>
          <p className="num">
            Promedio de la actividad: <b>{formatearNumero(r.promedio) ?? 'no disponible'}</b> · {r.aprobados} llegan a 7,0 · {r.bajo6} bajo 6,0
          </p>
          {r.sinDato > 0 && <p className="ayuda">{r.sinDato} sin dato del aula virtual (no disponible).</p>}
        </section>
      </div>

      <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-distribucion">
        <h2 id="titulo-distribucion">Cómo se reparten las notas</h2>
        {r.calificadas === 0 ? <p className="ayuda">Todavía no hay notas calificadas.</p> : <Distribucion distribucion={datos.distribucion} />}
      </section>

      <div className="ficha-pagina-columnas">
        <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-criterios">
          <h2 id="titulo-criterios">Dominio por criterio de la rúbrica</h2>
          {datos.criterios.length === 0 ? (
            <p className="ayuda">Esta actividad no tiene rúbrica en el aula virtual.</p>
          ) : (
            <ul className="actividad-criterios">
              {datos.criterios.map((c) => (
                <li key={c.id}>
                  <div className="num">
                    <b>{c.descripcion}</b>
                    {c.tema && <span className="ayuda"> · {c.tema}</span>}
                  </div>
                  <div className="pista-fina" aria-hidden="true">
                    {c.domina !== null && (
                      <div className={`pista-fina-valor${c.domina < 0.7 ? ' pista-fina-valor--bajo' : ''}`} style={{ width: `${c.domina * 100}%` }} />
                    )}
                  </div>
                  <span className="ayuda num">
                    {c.domina === null
                      ? 'sin niveles calificados'
                      : `${formatearPorcentaje(c.domina)} del curso logra el 70 % del puntaje · promedio ${formatearNumero(c.puntajePromedio)} de ${formatearNumero(c.puntajeMaximo)}`}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-temas-actividad">
          <h2 id="titulo-temas-actividad">Temas que evalúa</h2>
          {datos.temas.length === 0 ? (
            <p className="ayuda">
              Sin temas asociados. <Link to={`/cursos/${id}/mapa`}>Asócialos en el Mapa del curso</Link>.
            </p>
          ) : (
            <ul className="ficha-temas">
              {datos.temas.map((t) => (
                <li key={t.id} className="num">
                  <b>{t.nombre}</b> · {t.domina === null ? 'sin notas' : `domina ${formatearPorcentaje(t.domina)} del curso · promedio ${formatearNumero(t.promedio)}`}
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>

      <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-atencion">
        <h2 id="titulo-atencion">Requieren atención en esta actividad</h2>
        {datos.requierenAtencion.length === 0 ? (
          <p className="ayuda">Nadie quedó bajo 6,0 ni dejó de entregar.</p>
        ) : (
          <ul className="chips">
            {datos.requierenAtencion.map((e) => (
              <li key={e.id}>
                <Link className={e.estado === 'SIN_ENTREGA' ? 'chip-sin-entrega' : 'chip-bajo'} to={`/cursos/${id}/estudiantes/${e.id}`}>
                  {e.nombre} {e.apellido} · {e.estado === 'SIN_ENTREGA' ? 'sin entrega' : formatearNumero(e.nota)}
                  {e.alertaActiva ? ' · requiere atención' : ''}
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-estudiantes">
        <h2 id="titulo-estudiantes">Notas y retroalimentación</h2>
        <p className="ayuda">Marca a quiénes redactar una retroalimentación con su rúbrica y tu comentario; el asistente prepara el borrador.</p>
        <div className="tabla-envoltura">
          <table className="tabla" aria-label="Notas de la actividad">
            <thead>
              <tr>
                <th>Retroalimentación</th>
                <th>Estudiante</th>
                <th>Nota</th>
                <th>Comentario del docente</th>
              </tr>
            </thead>
            <tbody>
              {datos.estudiantes.map((e) => {
                const { texto, clase } = celda(e)
                return (
                  <tr key={e.id}>
                    <td>
                      {e.estado === 'CALIFICADO' && (
                        <input
                          type="checkbox"
                          aria-label={`Retroalimentación para ${e.nombre} ${e.apellido}`}
                          checked={seleccion.includes(e.id)}
                          onChange={() => alternar(e.id)}
                        />
                      )}
                    </td>
                    <td>
                      <Link to={`/cursos/${id}/estudiantes/${e.id}`}>
                        {e.apellido}, {e.nombre}
                      </Link>
                    </td>
                    <td>
                      <Link to={`/cursos/${id}/libro?estudiante=${e.id}&actividad=${a.id}`} className={`nota ${clase}`}>
                        {texto}
                      </Link>
                    </td>
                    <td className="ficha-comentario">{e.comentario ?? '—'}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        <div>
          <button type="button" className="boton boton--ia" onClick={redactarRetroalimentacion} disabled={envio.enviando || seleccion.length === 0}>
            {envio.enviando ? 'Enviando…' : `Redactar retroalimentación (${seleccion.length})`}
          </button>
          {envio.error && (
            <p className="ayuda mapa-error" role="alert">
              {envio.error}
            </p>
          )}
        </div>
      </section>
    </div>
  )
}
