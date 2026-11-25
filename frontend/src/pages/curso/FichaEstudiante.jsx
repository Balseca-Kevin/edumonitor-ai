import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { CartesianGrid, Line, LineChart, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { Desglose, ParaLlegar } from '../../components/FichaLateral'
import { formatearFecha, formatearNumero } from '../../components/formato'
import { textoEvidencia } from '../../components/lenguaje'
import { describirError } from '../../services/errores'
import { obtenerFicha } from '../../services/fichas'
import { celda } from './Libro'

const NO_DISPONIBLE = 'no disponible'
const NOMBRE_REGLA = { R1: 'Descenso reciente', R2: 'Atrasos', R4: 'Promedio bajo' }

/** Evolución frente a 7,0: cada nota calificada en el orden del curso y el promedio ponderado con las notas hasta ese punto. */
function Evolucion({ puntos, aprobacion }) {
  if (!puntos.length) {
    return <p className="ayuda">Todavía no tiene notas calificadas.</p>
  }
  const datos = puntos.map((p) => ({ ...p, etiqueta: p.actividad }))
  return (
    <div className="ficha-grafico" role="img" aria-label={`Evolución de ${puntos.length} notas frente a 7,0`}>
      <ResponsiveContainer width="100%" height={240}>
        <LineChart data={datos} margin={{ top: 12, right: 16, bottom: 8, left: 0 }}>
          <CartesianGrid stroke="var(--linea)" vertical={false} />
          <XAxis dataKey="etiqueta" tick={{ fontSize: 11, fill: 'var(--tinta-2)' }} interval={0} angle={-20} textAnchor="end" height={56} />
          <YAxis domain={[0, 10]} ticks={[0, 2, 4, 6, 7, 8, 10]} tick={{ fontSize: 11, fill: 'var(--tinta-2)' }} width={28} />
          <ReferenceLine y={aprobacion} stroke="var(--acento-texto)" strokeDasharray="4 4" />
          <Tooltip formatter={(valor, nombre) => [formatearNumero(valor), nombre]} />
          <Line type="monotone" dataKey="nota" name="Nota" stroke="var(--grafico)" strokeWidth={0} dot={{ r: 4, fill: 'var(--grafico)' }} isAnimationActive={false} />
          <Line type="monotone" dataKey="promedio" name="Promedio hasta aquí" stroke="var(--ia)" strokeWidth={2} dot={false} isAnimationActive={false} />
        </LineChart>
      </ResponsiveContainer>
      <p className="ayuda">Puntos: cada nota. Línea: el promedio 40/30/30 con las notas hasta esa actividad. Línea punteada: 7,0.</p>
    </div>
  )
}

function Criterios({ criterios }) {
  if (!criterios.length) return null
  return (
    <details>
      <summary>Rúbrica</summary>
      <ul className="ficha-criterios">
        {criterios.map((c) => (
          <li key={c.descripcion}>
            {c.descripcion}: <b>{c.nivel}</b> ({formatearNumero(c.puntaje)} de {formatearNumero(c.puntajeMaximo)})
          </li>
        ))}
      </ul>
    </details>
  )
}

/**
 * HU-10 · Ficha del estudiante: las mismas notas y estados del libro, con el comentario del docente y la rúbrica de cada actividad,
 * la evolución frente a 7,0, «Para llegar a 7,0», los temas donde queda bajo 7,0 y sus alertas. Cada cifra se abre en el libro.
 */
export default function FichaEstudiante() {
  const { id, eid } = useParams()
  const navigate = useNavigate()
  const clave = `${id}|${eid}`
  const [cargado, setCargado] = useState({ clave: null, datos: null, error: null })

  useEffect(() => {
    let vigente = true
    obtenerFicha(id, eid)
      .then((datos) => vigente && setCargado({ clave, datos, error: null }))
      .catch((e) => vigente && setCargado({ clave, datos: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id, eid, clave])

  const { datos, error } = cargado.clave === clave ? cargado : { datos: null, error: null }

  if (error) {
    return (
      <div className={`aviso aviso--${error.tipo === 'sin-sincronizar' ? 'atencion' : 'error'}`} role="alert">
        <strong>{error.tipo === 'sin-sincronizar' ? 'Curso sin sincronizar' : error.titulo}</strong>
        {error.detalle}
      </div>
    )
  }
  if (!datos) return <p className="ayuda">Cargando la ficha…</p>

  const e = datos.estudiante
  const nombre = `${e.apellido}, ${e.nombre}`
  const enLibro = `/cursos/${id}/libro?estudiante=${e.id}`
  const comentarios = datos.actividades.filter((a) => a.comentario)

  return (
    <div className="ficha-pagina">
      <header className="ficha-pagina-cabecera hoja">
        <div>
          <span className="mono">FICHA DEL ESTUDIANTE</span>
          <h1>{nombre}</h1>
          <span className={datos.alertas.length ? 'chip-bajo' : 'chip-ok'}>{datos.alertas.length ? 'Requiere atención' : 'Al día'}</span>
        </div>
        <div className="ficha-promedio">
          <Link className={`cifra${datos.promedioPonderado === null ? ' cifra--no-disponible' : ''}`} to={enLibro} title="Ver sus notas en el libro">
            {formatearNumero(datos.promedioPonderado) ?? NO_DISPONIBLE}
          </Link>
          <span className="ayuda">promedio ponderado{datos.parcial ? ' (falta una categoría)' : ''}</span>
        </div>
        <div className="ficha-acciones ficha-acciones--fila">
          <button type="button" className="boton boton--ia" onClick={() => navigate(`/cursos/${id}/asistente?estudiante=${e.id}`)}>
            Analizar con el asistente
          </button>
          <button type="button" className="boton boton--secundario" onClick={() => navigate(`/cursos/${id}/refuerzos?estudiante=${e.id}`)}>
            Armar plan de refuerzo
          </button>
          <Link className="boton boton--secundario" to={enLibro}>
            Ver en el libro
          </Link>
        </div>
      </header>

      <div className="ficha-pagina-columnas">
        <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-evolucion">
          <h2 id="titulo-evolucion">Evolución frente a 7,0</h2>
          <Evolucion puntos={datos.evolucion} aprobacion={datos.aprobacion} />
        </section>
        <section className="hoja ficha-pagina-bloque">
          <Desglose categorias={datos.categorias} />
          <ParaLlegar cursoId={id} estudianteId={e.id} />
          <p className="ayuda num">
            {datos.notas.calificadas} notas registradas · {datos.notas.ceros} con cero · {datos.notas.sinEntrega} sin entregar ·{' '}
            {datos.notas.pendientes} por calificar · {datos.notas.aunNoVence} aún no vencen
          </p>
        </section>
      </div>

      <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-notas">
        <h2 id="titulo-notas">Notas por actividad</h2>
        <div className="tabla-envoltura">
          <table className="tabla" aria-label="Notas por actividad">
            <thead>
              <tr>
                <th>Actividad</th>
                <th>Unidad</th>
                <th>Categoría</th>
                <th>Fecha límite</th>
                <th>Nota</th>
                <th>Comentario del docente</th>
              </tr>
            </thead>
            <tbody>
              {datos.actividades.map((a) => {
                const { texto, clase } = celda(a)
                return (
                  <tr key={a.id}>
                    <td>
                      {a.nombre}
                      <Criterios criterios={a.criterios} />
                    </td>
                    <td>{a.unidad ? `U${a.unidad}` : 'Sin unidad'}</td>
                    <td>{a.categoria ?? '—'}</td>
                    <td className="num">{a.fechaLimite ? formatearFecha(a.fechaLimite) : 'sin fecha'}</td>
                    <td>
                      <Link to={`${enLibro}&actividad=${a.id}`} className={`nota ${clase}`} title="Ver la celda en el libro">
                        {texto}
                      </Link>
                    </td>
                    <td className="ficha-comentario">{a.comentario ?? '—'}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        {comentarios.length === 0 && <p className="ayuda">El docente todavía no dejó comentarios en sus entregas.</p>}
      </section>

      <div className="ficha-pagina-columnas">
        <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-temas">
          <h2 id="titulo-temas">Temas donde queda bajo 7,0</h2>
          {datos.temasDebiles.length === 0 ? (
            <p className="ayuda">Ningún tema bajo 7,0 con las actividades asociadas a temas en el Mapa del curso.</p>
          ) : (
            <ul className="ficha-temas">
              {datos.temasDebiles.map((t) => (
                <li key={t.id}>
                  <div className="num">
                    <b>{t.nombre}</b> · U{t.unidad} · su nota <b>{formatearNumero(t.nota)}</b>
                    {t.promedioCurso !== null && ` · curso ${formatearNumero(t.promedioCurso)}`}
                  </div>
                  <div className="ayuda">
                    Sale de:{' '}
                    {t.notas.map((n, i) => (
                      <span key={n.actividadId}>
                        {i > 0 && ', '}
                        <Link to={`${enLibro}&actividad=${n.actividadId}`}>
                          {n.actividad} ({formatearNumero(n.nota)})
                        </Link>
                      </span>
                    ))}
                    {t.proximas.length > 0 &&
                      ` · se vuelve a evaluar en ${t.proximas.map((p) => `${p.nombre} (${formatearFecha(p.fechaLimite).slice(0, 5)})`).join(', ')}`}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section className="hoja ficha-pagina-bloque" aria-labelledby="titulo-alertas">
          <h2 id="titulo-alertas">Alertas activas</h2>
          {datos.alertas.length === 0 ? (
            <p className="ayuda">No tiene alertas activas.</p>
          ) : (
            <ul className="ficha-temas">
              {datos.alertas.map((a) => (
                <li key={a.id}>
                  <b>Requiere atención · {NOMBRE_REGLA[a.regla] ?? a.regla}</b>
                  <div className="ayuda">{textoEvidencia(a)}</div>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  )
}
