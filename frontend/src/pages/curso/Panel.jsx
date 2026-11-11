import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Bar, BarChart, CartesianGrid, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import Estado from '../../components/s2/Estado'
import { formatearFecha, formatearNumero, formatearPorcentaje } from '../../components/s2/formato'
import { contarAlertasActivas, obtenerIndicadores } from '../../services/indicadores'
import { describirError } from '../../services/errores'

const NO_DISPONIBLE = 'no disponible'

function Cifra({ valor, sufijo }) {
  if (valor === null || valor === undefined) {
    return <p className="s2-cifra s2-cifra--no-disponible">{NO_DISPONIBLE}</p>
  }
  return (
    <p className="s2-cifra">
      {valor}
      {sufijo}
    </p>
  )
}

function Tarjeta({ titulo, clase = 's2-col-4', children }) {
  return (
    <section className={`s2-tarjeta ${clase}`} aria-label={titulo}>
      <h3 className="s2-tarjeta-titulo">{titulo}</h3>
      {children}
    </section>
  )
}

function texto(valor, decimales = 2) {
  return formatearNumero(valor, decimales) ?? NO_DISPONIBLE
}

function tendenciaConSigno(valor) {
  if (valor === null || valor === undefined) return NO_DISPONIBLE
  const numero = formatearNumero(valor)
  return valor > 0 ? `+${numero}` : numero
}

/** Avance por unidad: calificadas sobre actividades, agrupado por parcial (sale de «unidades» del contrato). */
function AvanceUnidades({ unidades }) {
  if (!unidades?.length) return null
  const parciales = [...new Set(unidades.map((u) => u.parcial))].sort()
  return (
    <div className="s2-avance" aria-label="Avance por unidad">
      {parciales.map((parcial) => (
        <div key={parcial} className="s2-avance-grupo">
          <span className="s2-tarjeta-titulo">Parcial {parcial}</span>
          <div className="s2-avance-unidades">
            {unidades
              .filter((u) => u.parcial === parcial)
              .map((u) => (
                <div key={u.numero} className="s2-avance-unidad" title={u.nombre}>
                  <span
                    className={`s2-avance-barra ${u.actividades > 0 && u.calificadas === u.actividades ? 's2-avance-barra--completa' : ''}`}
                  />
                  <span className="s2-num s2-ayuda">
                    U{u.numero} · {u.calificadas}/{u.actividades}
                  </span>
                </div>
              ))}
          </div>
        </div>
      ))}
    </div>
  )
}

function Distribucion({ distribucion, aprobacion }) {
  const datos = (distribucion ?? []).map((t) => ({ tramo: `${t.desde}–${t.hasta}`, cantidad: t.cantidad }))
  const tramoAprobacion = `${Math.floor(aprobacion)}–${Math.floor(aprobacion) + 1}`
  return (
    <div className="s2-grafico" role="img" aria-label="Histograma de promedios ponderados en 10 tramos con la línea de aprobación">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={datos} margin={{ top: 24, right: 8, bottom: 0, left: -16 }}>
          <CartesianGrid vertical={false} />
          <XAxis dataKey="tramo" tickLine={false} />
          <YAxis allowDecimals={false} tickLine={false} axisLine={false} />
          <Tooltip formatter={(valor) => [`${valor} estudiantes`, 'Cantidad']} />
          <ReferenceLine
            x={tramoAprobacion}
            position="start"
            className="s2-linea-aprobacion"
            label={{ value: `Aprobación ${formatearNumero(aprobacion, 1)}`, position: 'top' }}
          />
          <Bar dataKey="cantidad" isAnimationActive={false} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

function TablaEstudiantes({ estudiantes }) {
  if (!estudiantes.length) {
    return <p className="s2-ayuda">El curso no tiene estudiantes matriculados en la última sincronización.</p>
  }

  return (
    <div className="s2-tabla-envoltura">
      <table className="s2-tabla" aria-label="Estudiantes">
        <thead>
          <tr>
            <th>Estudiante</th>
            <th className="s2-num">Promedio ponderado</th>
            <th className="s2-num">z</th>
            <th>Grupo</th>
            <th className="s2-num">Calificadas</th>
            <th className="s2-num">Ceros</th>
            <th className="s2-num">Sin entrega</th>
            <th className="s2-num">Pendientes</th>
            <th className="s2-num">Finalización</th>
            <th className="s2-num">Tendencia</th>
          </tr>
        </thead>
        <tbody>
          {estudiantes.map((e) => (
            <tr key={e.id}>
              <td>
                {e.apellido}, {e.nombre}
              </td>
              <td className="s2-num">
                {texto(e.promedioPonderado)}
                {e.parcial && <span className="s2-ayuda"> (parcial)</span>}
              </td>
              <td className="s2-num">{texto(e.z)}</td>
              <td>{e.grupo ? <span className="s2-grupo">{e.grupo}</span> : NO_DISPONIBLE}</td>
              <td className="s2-num">{e.notas.calificadas}</td>
              <td className="s2-num">{e.notas.ceros}</td>
              <td className="s2-num">{e.notas.sinEntrega}</td>
              <td className="s2-num">{e.notas.pendientes}</td>
              <td className="s2-num">{formatearPorcentaje(e.tasaFinalizacion) ?? NO_DISPONIBLE}</td>
              <td className="s2-num">{tendenciaConSigno(e.tendencia)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/**
 * HU-09 · Panel del curso en bento grid (diseno.md). Muestra el promedio con escala explícita, la mediana y la desviación,
 * la distribución con la línea de 7,0, los grupos, la finalización y la tabla de estudiantes donde una nota cero, una
 * actividad sin entrega y una entrega pendiente se cuentan por separado. Un dato en noDisponibles nunca se muestra como 0.
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
      <div className="s2-aviso s2-aviso--atencion" role="alert">
        <strong>Curso sin sincronizar</strong>
        {error.detalle ?? 'El curso todavía no tiene datos de Moodle.'} <Link to="/cursos">Ir a Mis cursos</Link>
      </div>
    )
  }

  if (error) {
    return (
      <div className="s2-aviso s2-aviso--error" role="alert">
        <strong>{error.titulo}</strong>
        {error.detalle}
      </div>
    )
  }

  if (!datos) {
    return <p className="s2-ayuda">Cargando indicadores…</p>
  }

  const { curso, grupos, sincronizacion, noDisponibles = [] } = datos
  const finalizacionNoDisponible = noDisponibles.includes('tasaFinalizacion') || curso.tasaFinalizacion === null

  return (
    <div className="s2-panel">
      <AvanceUnidades unidades={datos.unidades} />

      <div className="s2-bento">
        <Tarjeta titulo="Promedio del curso">
          <Cifra valor={formatearNumero(curso.promedio)} sufijo={curso.promedio !== null && <span className="s2-ayuda"> / {datos.escala}</span>} />
          <p className="s2-contexto s2-num">
            Escala 0 a {datos.escala} · aprobación {formatearNumero(datos.aprobacion, 1)} · {curso.estudiantes} estudiantes ·{' '}
            {curso.actividadesEvaluadas} actividades evaluadas
          </p>
        </Tarjeta>

        <Tarjeta titulo="Mediana y desviación">
          <Cifra valor={formatearNumero(curso.mediana)} />
          <p className="s2-contexto s2-num">
            Desviación estándar {texto(curso.desviacion)} · tendencia {tendenciaConSigno(curso.tendencia)}
          </p>
        </Tarjeta>

        <Tarjeta titulo="Requiere atención">
          <Cifra valor={alertas} />
          <p className="s2-contexto">
            {alertas === null ? 'Las alertas de este curso no están disponibles todavía.' : <Link to={`/cursos/${id}/alertas`}>Ver alertas activas</Link>}
          </p>
        </Tarjeta>

        <Tarjeta titulo="Distribución de notas" clase="s2-col-8 s2-fila-2">
          <Distribucion distribucion={datos.distribucion} aprobacion={datos.aprobacion} />
          <p className="s2-contexto">Promedios ponderados en tramos de 1 punto. La línea marca la nota de aprobación.</p>
        </Tarjeta>

        <Tarjeta titulo="Grupos de rendimiento">
          <div className="s2-grupos">
            <div>
              <p className="s2-grupo-cifra">{grupos.alto}</p>
              <p className="s2-contexto">Alto ≥ 8,5</p>
            </div>
            <div>
              <p className="s2-grupo-cifra">{grupos.medio}</p>
              <p className="s2-contexto">Medio 6,0 a 8,49</p>
            </div>
            <div>
              <p className="s2-grupo-cifra">{grupos.bajo}</p>
              <p className="s2-contexto">Bajo &lt; 6,0</p>
            </div>
          </div>
        </Tarjeta>

        <Tarjeta titulo="Finalización">
          <Cifra valor={finalizacionNoDisponible ? null : formatearPorcentaje(curso.tasaFinalizacion)} />
          <p className="s2-contexto">
            {finalizacionNoDisponible
              ? 'Moodle no entregó el estado de finalización en la última sincronización.'
              : 'Actividades completadas sobre actividades con seguimiento.'}
          </p>
        </Tarjeta>

        <section className="s2-col-12 s2-seccion" aria-labelledby="titulo-estudiantes">
          <h3 id="titulo-estudiantes" className="s2-seccion-titulo">
            Estudiantes
          </h3>
          <TablaEstudiantes estudiantes={datos.estudiantes} />
        </section>
      </div>

      <p className="s2-ayuda s2-num s2-fuente">
        Fuente: Moodle, sincronización del {formatearFecha(sincronizacion.fecha)} · <Estado estado={sincronizacion.estado} />. Solo las notas
        calificadas entran a los promedios; «parcial» indica que faltó una categoría y los pesos 40/30/30 se normalizaron.
      </p>
    </div>
  )
}
