import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Bar, BarChart, Cell, ReferenceLine, ResponsiveContainer, XAxis, YAxis } from 'recharts'
import Estado from '../../components/Estado'
import { formatearFecha, formatearNumero } from '../../components/formato'
import { sinMovimiento, useCuenta } from '../../components/movimiento'
import { frenteAlCurso, ultimasNotas } from '../../components/lenguaje'
import { contarAlertasActivas, obtenerIndicadores } from '../../services/indicadores'
import { describirError } from '../../services/errores'

const NO_DISPONIBLE = 'no disponible'
const BAJO = 6 // por debajo de 6,0 el valor va en arena (diseño D)

/** Cifra grande que cuenta una vez desde 0. Null se ve como «no disponible», nunca 0. */
function Cifra({ valor, decimales = 2, sufijo }) {
  const actual = useCuenta(valor)
  if (valor === null || valor === undefined) return <span className="cifra cifra--no-disponible">{NO_DISPONIBLE}</span>
  return (
    <span className="cifra">
      {formatearNumero(actual, decimales)}
      {sufijo}
    </span>
  )
}

function Bloque({ titulo, retraso, atencion = false, children }) {
  return (
    <section className="cifra-bloque entra" style={{ animationDelay: `${retraso}ms` }} aria-label={titulo}>
      <div className={`regla${atencion ? ' regla--atencion' : ''}`} style={{ animationDelay: `${retraso}ms` }} />
      <span className={`cifra-titulo${atencion ? ' cifra-titulo--atencion' : ''}`}>{titulo}</span>
      {children}
    </section>
  )
}

function Distribucion({ distribucion, aprobacion, estudiantes }) {
  const datos = (distribucion ?? []).map((t) => ({ tramo: String(t.desde), desde: t.desde, cantidad: t.cantidad }))
  const quieto = sinMovimiento()
  return (
    <div className="seccion ancho-2">
      <div className="regla" style={{ animationDelay: '400ms' }} />
      <div className="seccion-cabecera">
        <h2 className="seccion-titulo">Distribución de notas</h2>
        <span className="ayuda">{estudiantes} estudiantes · tramos de 1 punto</span>
      </div>
      <div className="histograma" role="img" aria-label={`Cuántos estudiantes hay en cada tramo de 1 punto, con la línea de aprobación en ${formatearNumero(aprobacion, 1)}`}>
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={datos} margin={{ top: 24, right: 8, bottom: 0, left: 0 }} barCategoryGap={4}>
            <XAxis dataKey="tramo" tickLine={false} axisLine={{ stroke: 'var(--tinta)' }} />
            <YAxis allowDecimals={false} tickLine={false} axisLine={false} width={32} />
            <ReferenceLine
              x={String(Math.floor(aprobacion))}
              className="linea-aprobacion"
              position="start"
              label={{ value: `${formatearNumero(aprobacion, 1)} aprobación`, position: 'insideTopLeft' }}
            />
            <Bar dataKey="cantidad" isAnimationActive={!quieto} animationDuration={800} animationBegin={500}>
              {datos.map((t) => (
                <Cell key={t.tramo} fill={t.desde < BAJO ? 'var(--arena)' : 'var(--grafico)'} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}

function Reparto({ grupos, total }) {
  const filas = [
    { texto: '8,5 o más', cantidad: grupos.alto, arena: false },
    { texto: 'Entre 6,0 y 8,4', cantidad: grupos.medio, arena: false },
    { texto: 'Menos de 6,0', cantidad: grupos.bajo, arena: true },
  ]
  return (
    <section className="seccion" aria-label="Cómo se reparte el curso">
      <div className="regla" style={{ animationDelay: '480ms' }} />
      <h2 className="seccion-titulo">Cómo se reparte el curso</h2>
      <div className="reparto">
        {filas.map((f, i) => (
          <div className="reparto-fila" key={f.texto}>
            <div>
              <span>{f.texto}</span>
              <b className="num">{f.cantidad}</b>
            </div>
            <div className="reparto-pista">
              <div
                className={`reparto-barra${f.arena ? ' reparto-barra--arena' : ''}`}
                style={{ width: `${total ? (f.cantidad / total) * 100 : 0}%`, animationDelay: `${900 + i * 100}ms` }}
              />
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}

function TablaEstudiantes({ estudiantes, promedioCurso }) {
  if (!estudiantes.length) {
    return <p className="ayuda">El curso no tiene estudiantes matriculados en la última sincronización.</p>
  }
  return (
    <div className="tabla-envoltura">
      <table className="tabla" aria-label="Estudiantes">
        <thead>
          <tr>
            <th>Estudiante</th>
            <th className="num">Promedio</th>
            <th>Frente al curso</th>
            <th className="num">Notas registradas</th>
            <th className="num">Con cero</th>
            <th className="num">Sin entregar</th>
            <th className="num">Por calificar</th>
            <th>Últimas notas</th>
          </tr>
        </thead>
        <tbody>
          {estudiantes.map((e, i) => {
            const bajo = e.promedioPonderado !== null && e.promedioPonderado < BAJO
            const bajando = e.tendencia !== null && e.tendencia !== undefined && e.tendencia <= -1.5
            return (
              <tr key={e.id} className="entra" style={{ animationDelay: `${Math.min(700 + i * 30, 1800)}ms` }}>
                <td>
                  {e.apellido}, {e.nombre}
                </td>
                <td className={`num fuerte${bajo ? ' arena-texto' : ''}`}>
                  {formatearNumero(e.promedioPonderado) ?? NO_DISPONIBLE}
                  {e.parcial && <span title="Faltó una categoría y el promedio se calculó con las que hay"> *</span>}
                </td>
                <td className={bajo ? 'arena-texto' : undefined}>{frenteAlCurso(e.promedioPonderado, promedioCurso)}</td>
                <td className="num">{e.notas.calificadas}</td>
                <td className={`num${e.notas.ceros ? ' fuerte' : ''}`}>{e.notas.ceros}</td>
                <td className={`num${e.notas.sinEntrega ? ' fuerte' : ''}`}>{e.notas.sinEntrega}</td>
                <td className={`num${e.notas.pendientes ? ' fuerte' : ''}`}>{e.notas.pendientes}</td>
                <td className={bajando ? 'arena-texto' : undefined}>{ultimasNotas(e.tendencia)}</td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

/**
 * HU-09 · Panel del curso con el diseño D (mockup MK-09): cuatro cifras que cuentan una vez, la distribución con la línea de 7,0,
 * cómo se reparte el curso y la tabla de estudiantes en lenguaje docente («Frente al curso», «Con cero», «Sin entregar», «Por
 * calificar», «Últimas notas»). Una nota cero, una actividad sin entrega y una por calificar se cuentan por separado. Un dato en
 * noDisponibles nunca se muestra como 0.
 */
export default function Panel() {
  const { id } = useParams()
  const [cargado, setCargado] = useState({ id: null, datos: null, alertas: null, error: null })

  useEffect(() => {
    let vigente = true
    Promise.all([obtenerIndicadores(id), contarAlertasActivas(id)])
      .then(([datos, alertas]) => vigente && setCargado({ id, datos, alertas, error: null }))
      .catch((e) => vigente && setCargado({ id, datos: null, alertas: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id])

  const { datos, alertas, error } = cargado.id === id ? cargado : { datos: null, alertas: null, error: null }

  if (error?.tipo === 'sin-sincronizar') {
    return (
      <div className="aviso aviso--atencion" role="alert">
        <strong>Curso sin sincronizar</strong>
        <span>
          {error.detalle ?? 'El curso todavía no tiene datos del aula virtual.'} <Link to="/cursos">Ir a Mis cursos</Link>
        </span>
      </div>
    )
  }

  if (error) {
    return (
      <div className="aviso aviso--error" role="alert">
        <strong>{error.titulo}</strong>
        {error.detalle}
      </div>
    )
  }

  if (!datos) return <p className="ayuda">Cargando indicadores…</p>

  const { curso, grupos, sincronizacion, noDisponibles = [] } = datos
  const finalizacion =
    noDisponibles.includes('tasaFinalizacion') || curso.tasaFinalizacion === null ? null : Math.round(curso.tasaFinalizacion * 100)
  const desviacion = formatearNumero(curso.desviacion, 1)

  return (
    <>
      <section className="panel-cifras" aria-label="Cifras del curso">
        <Bloque titulo="Promedio del curso" retraso={100}>
          <Cifra valor={curso.promedio} />
          <span className="cifra-contexto">
            sobre {datos.escala} · aprobación {formatearNumero(datos.aprobacion, 1)}
          </span>
        </Bloque>
        <Bloque titulo="La mitad del curso tiene más de" retraso={180}>
          <Cifra valor={curso.mediana} />
          <span className="cifra-contexto">
            {desviacion ? `las notas varían unos ${desviacion} puntos alrededor del promedio` : 'variación no disponible'}
          </span>
        </Bloque>
        <Bloque titulo="Actividades completadas" retraso={260}>
          <Cifra valor={finalizacion} decimales={0} sufijo={finalizacion === null ? null : ' %'} />
          <span className="cifra-contexto">
            {finalizacion === null
              ? 'El aula virtual no entregó el estado de finalización en la última sincronización.'
              : `${curso.actividadesEvaluadas} actividades con seguimiento`}
          </span>
        </Bloque>
        <Bloque titulo="Requiere atención" retraso={340} atencion>
          <Cifra valor={alertas} decimales={0} />
          <span className="cifra-contexto">
            {alertas === null ? (
              'Las alertas de este curso no están disponibles.'
            ) : (
              <>
                alertas activas · <Link to={`/cursos/${id}/alertas`}>ver alertas →</Link>
              </>
            )}
          </span>
        </Bloque>
      </section>

      <div className="panel-medio">
        <Distribucion distribucion={datos.distribucion} aprobacion={datos.aprobacion} estudiantes={curso.estudiantes} />
        <Reparto grupos={grupos} total={grupos.alto + grupos.medio + grupos.bajo} />
      </div>

      <section className="seccion" aria-labelledby="titulo-estudiantes">
        <div className="regla" style={{ animationDelay: '560ms' }} />
        <div className="seccion-cabecera">
          <h2 id="titulo-estudiantes" className="seccion-titulo">
            Estudiantes
          </h2>
          <span className="ayuda">Solo las notas calificadas entran al promedio. Cero, sin entregar y por calificar se cuentan aparte.</span>
        </div>
        <TablaEstudiantes estudiantes={datos.estudiantes} promedioCurso={curso.promedio} />
      </section>

      <p className="fuente">
        Datos del aula virtual, sincronización del {formatearFecha(sincronizacion.fecha)} · <Estado estado={sincronizacion.estado} />. El
        promedio pondera exámenes 40 %, tareas 30 % y APE 30 %; * faltó una categoría y se calculó con las que hay.
      </p>
    </>
  )
}
