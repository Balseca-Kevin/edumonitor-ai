import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useOutletContext, useParams } from 'react-router-dom'
import { Bar, BarChart, Cell, LabelList, ReferenceLine, ResponsiveContainer, XAxis, YAxis } from 'recharts'
import Estado from '../../components/Estado'
import FichaLateral from '../../components/FichaLateral'
import { formatearFecha, formatearNumero } from '../../components/formato'
import { sinMovimiento, useCuenta } from '../../components/movimiento'
import { ultimasNotas } from '../../components/lenguaje'
import { busquedaTramo, nombreTramo, useTramo } from '../../components/tramo'
import { obtenerIndicadores } from '../../services/indicadores'
import { obtenerAlertas } from '../../services/alertas'
import { actividadesDelLibro, obtenerLibro } from '../../services/libro'
import { describirError } from '../../services/errores'
import { PREGUNTA_INFORME } from '../../services/informe'

const NO_DISPONIBLE = 'no disponible'
const APROBACION = 7

/** Cifra grande que cuenta una vez desde 0. Null se ve como «no disponible», nunca 0. */
function Cifra({ valor, decimales = 2 }) {
  const actual = useCuenta(valor)
  if (valor === null || valor === undefined) return <span className="cifra cifra--no-disponible">{NO_DISPONIBLE}</span>
  return <span className="cifra">{formatearNumero(actual, decimales)}</span>
}

/** El estudiante cae en el tramo [desde, hasta) de 1 punto; el 10 cae en el último. */
function enTramo(promedio, desde) {
  if (promedio === null || promedio === undefined) return false
  return desde === 9 ? promedio >= 9 : promedio >= desde && promedio < desde + 1
}

function Reparto({ distribucion, elegido, onElegir }) {
  const datos = (distribucion ?? []).map((t) => ({ tramo: String(t.desde), desde: t.desde, cantidad: t.cantidad }))
  const quieto = sinMovimiento()
  return (
    <section className="hoja panel-reparto" aria-labelledby="titulo-reparto">
      <div>
        <h2 id="titulo-reparto" className="hoja-titulo">
          Cómo se reparte el curso
        </h2>
        <p className="ayuda">
          Haz clic en una barra para filtrar la tabla.
          {elegido !== null && ` Mostrando promedios de ${elegido} a ${elegido + 1}.`}
        </p>
      </div>
      <div className="histograma" role="group" aria-label="Estudiantes por tramo de 1 punto, con la línea de 7,0">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={datos} margin={{ top: 22, right: 4, bottom: 0, left: 0 }} barCategoryGap={4}>
            <XAxis dataKey="tramo" tickLine={false} axisLine={{ stroke: 'var(--tinta)' }} />
            <YAxis hide allowDecimals={false} />
            <ReferenceLine x={String(APROBACION)} className="linea-aprobacion" position="start" label={{ value: '7,0 aprueba', position: 'insideTopLeft' }} />
            <Bar dataKey="cantidad" isAnimationActive={!quieto} animationDuration={800} animationBegin={300} onClick={(t) => onElegir(t.desde)} cursor="pointer">
              <LabelList dataKey="cantidad" position="top" className="histograma-cantidad" />
              {datos.map((t) => (
                <Cell
                  key={t.tramo}
                  fill={t.desde < APROBACION ? 'var(--arena)' : 'var(--grafico)'}
                  fillOpacity={elegido === null || elegido === t.desde ? 1 : 0.35}
                />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
      <div className="histograma-botones" role="group" aria-label="Filtrar la tabla por tramo">
        {datos.map((t) => (
          <button
            key={t.tramo}
            type="button"
            aria-pressed={elegido === t.desde}
            aria-label={`${t.cantidad} estudiantes con promedio de ${t.desde} a ${t.desde + 1}`}
            onClick={() => onElegir(t.desde)}
          >
            {t.desde}
          </button>
        ))}
      </div>
    </section>
  )
}

function TablaEstudiantes({ estudiantes, conAlertas, filtro, onQuitarFiltro, onAbrir }) {
  return (
    <section className="hoja panel-tabla" aria-labelledby="titulo-estudiantes">
      <div className="hoja-cabecera">
        <h2 id="titulo-estudiantes" className="hoja-titulo">
          Estudiantes
        </h2>
        <span className="ayuda">
          {filtro !== null ? `${estudiantes.length} con promedio de ${filtro} a ${filtro + 1}` : 'Haz clic en un estudiante para ver su ficha.'}
        </span>
        {filtro !== null && (
          <button type="button" className="boton boton--secundario boton--chico hoja-cabecera-accion" onClick={onQuitarFiltro}>
            Quitar filtro
          </button>
        )}
      </div>
      {estudiantes.length === 0 ? (
        <p className="ayuda hoja-vacia">
          {filtro !== null ? 'Ningún estudiante tiene un promedio en ese tramo.' : 'El curso no tiene estudiantes matriculados en la última sincronización.'}
        </p>
      ) : (
        <div className="tabla-envoltura">
          <table className="tabla tabla--filas" aria-label="Estudiantes">
            <thead>
              <tr>
                <th>Estudiante</th>
                <th>Promedio</th>
                <th className="num">Sin entregar</th>
                <th>Últimas notas</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {estudiantes.map((e, i) => {
                const bajo = e.promedioPonderado !== null && e.promedioPonderado < APROBACION
                const atencion = conAlertas.has(e.id)
                return (
                  <tr
                    key={e.id}
                    className="entra fila-abre"
                    style={{ animationDelay: `${Math.min(300 + i * 30, 1500)}ms` }}
                    tabIndex={0}
                    onClick={() => onAbrir(e)}
                    onKeyDown={(evento) => (evento.key === 'Enter' || evento.key === ' ') && (evento.preventDefault(), onAbrir(e))}
                    aria-label={`Abrir la ficha de ${e.apellido}, ${e.nombre}`}
                  >
                    <td className="fuerte-600">
                      {e.apellido}, {e.nombre}
                    </td>
                    <td>
                      <div className="promedio-barra">
                        <span className={`fuerte${bajo ? ' arena-texto' : ''}`}>
                          {formatearNumero(e.promedioPonderado) ?? NO_DISPONIBLE}
                          {e.parcial && <span title="Faltó una categoría y el promedio se calculó con las que hay"> *</span>}
                        </span>
                        <div className="pista-fina pista-fina--corta">
                          {e.promedioPonderado !== null && (
                            <div className={`pista-fina-valor${bajo ? ' pista-fina-valor--bajo' : ''}`} style={{ width: `${e.promedioPonderado * 10}%` }} />
                          )}
                        </div>
                      </div>
                    </td>
                    <td className={`num${e.notas.sinEntrega ? ' fuerte' : ''}`}>{e.notas.sinEntrega}</td>
                    <td>{ultimasNotas(e.tendencia)}</td>
                    <td>
                      <span className={atencion ? 'chip-bajo' : 'chip-ok'}>{atencion ? 'Requiere atención' : 'Al día'}</span>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

/**
 * HU-09 y R.4 · Panel del curso (docs/sprint-3/diseno.md): 4 indicadores con regla de 2 px («Requieren atención» lleva a Alertas),
 * el histograma de 10 tramos con la línea de 7,0 que filtra la tabla, y la tabla de estudiantes; un clic en la fila abre la ficha
 * lateral. Todo sale de la API para el tramo elegido en la barra del curso; un dato que falta se ve «no disponible», nunca 0.
 */
export default function Panel() {
  const { id } = useParams()
  const tramo = useTramo()
  const navigate = useNavigate()
  const { version = 0 } = useOutletContext() ?? {}
  const clave = `${id}|${tramo}|${version}`
  const [cargado, setCargado] = useState({ clave: null, datos: null, alertas: null, error: null })
  const [libro, setLibro] = useState({ clave: null, datos: null })
  const [filtro, setFiltro] = useState({ clave: null, desde: null })
  const [abierto, setAbierto] = useState(null)

  useEffect(() => {
    let vigente = true
    Promise.all([obtenerIndicadores(id, tramo), obtenerAlertas(id, 'ACTIVA', tramo).catch(() => null)])
      .then(([datos, alertas]) => vigente && setCargado({ clave, datos, alertas, error: null }))
      .catch((e) => vigente && setCargado({ clave, datos: null, alertas: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id, tramo, clave])

  const vigente = cargado.clave === clave ? cargado : { datos: null, alertas: null, error: null }
  const { datos, alertas, error } = vigente
  const desde = filtro.clave === clave ? filtro.desde : null
  const conAlertas = useMemo(() => new Set((alertas ?? []).map((a) => a.estudiante?.id)), [alertas])

  const abrir = useCallback(
    (estudiante) => {
      setAbierto(estudiante.id)
      if (libro.clave !== clave) {
        obtenerLibro(id, { tramo })
          .then((datosLibro) => setLibro({ clave, datos: datosLibro }))
          .catch(() => setLibro({ clave, datos: null }))
      }
    },
    [clave, id, libro.clave, tramo],
  )
  const cerrar = useCallback(() => setAbierto(null), [])

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

  const { curso, sincronizacion, unidades } = datos
  const tramoTexto = nombreTramo(tramo, unidades)
  const calificadas = datos.estudiantes.reduce((total, e) => total + (e.notas?.calificadas ?? 0), 0)
  const atencion = alertas === null ? null : conAlertas.size
  const visibles = desde === null ? datos.estudiantes : datos.estudiantes.filter((e) => enTramo(e.promedioPonderado, desde))
  const estudiante = datos.estudiantes.find((e) => e.id === abierto) ?? null
  const columnas = libro.clave === clave ? actividadesDelLibro(libro.datos) : []
  const fila = libro.clave === clave ? libro.datos?.estudiantes.find((e) => e.id === abierto) : null
  const celdas = fila ? columnas.map((a, i) => ({ ...fila.notas[i], nombre: a.nombre, fechaLimite: a.fechaLimite })) : []

  return (
    <>
      <div className="panel-acciones">
        <button
          type="button"
          className="boton boton--secundario"
          onClick={() => navigate(`/cursos/${id}/asistente?${new URLSearchParams({ pregunta: PREGUNTA_INFORME })}`)}
        >
          Informe del curso
        </button>
      </div>
      <section className="panel-cifras-s3" aria-label="Cifras del curso">
        <div className="panel-cifra entra">
          <span className="cifra-titulo">Promedio del curso</span>
          <Cifra valor={curso.promedio} />
          <span className="cifra-contexto">
            de {datos.escala} · aprueba con {formatearNumero(datos.aprobacion, 1)}
          </span>
        </div>
        <div className="panel-cifra entra" style={{ animationDelay: '80ms' }}>
          <span className="cifra-titulo">La mitad del curso tiene más de</span>
          <Cifra valor={curso.mediana} />
          <span className="cifra-contexto">en {tramoTexto}</span>
        </div>
        <div className="panel-cifra entra" style={{ animationDelay: '160ms' }}>
          <span className="cifra-titulo">Entregas calificadas</span>
          <Cifra valor={calificadas} decimales={0} />
          <span className="cifra-contexto">en {curso.actividadesEvaluadas} actividades con notas</span>
        </div>
        <button
          type="button"
          className="panel-cifra panel-cifra--atencion entra"
          style={{ animationDelay: '240ms' }}
          onClick={() => navigate(`/cursos/${id}/alertas${busquedaTramo(tramo)}`)}
        >
          <span className="cifra-titulo">Requieren atención</span>
          <Cifra valor={atencion} decimales={0} />
          <span className="cifra-contexto">{atencion === null ? 'Las alertas no están disponibles.' : 'Ver alertas con su evidencia →'}</span>
        </button>
      </section>

      <div className="panel-medio-s3">
        <Reparto distribucion={datos.distribucion} elegido={desde} onElegir={(d) => setFiltro({ clave, desde: desde === d ? null : d })} />
        <TablaEstudiantes
          estudiantes={visibles}
          conAlertas={conAlertas}
          filtro={desde}
          onQuitarFiltro={() => setFiltro({ clave, desde: null })}
          onAbrir={abrir}
        />
      </div>

      <p className="fuente">
        Datos del aula virtual, sincronización del {formatearFecha(sincronizacion.fecha)} · <Estado estado={sincronizacion.estado} />. El
        promedio pondera exámenes 40 %, tareas 30 % y APE 30 %; * faltó una categoría y se calculó con las que hay.
      </p>

      <FichaLateral
        cursoId={id}
        estudiante={estudiante}
        requiereAtencion={estudiante ? conAlertas.has(estudiante.id) : false}
        celdas={celdas}
        tramo={tramo}
        nombreTramo={tramoTexto}
        onCerrar={cerrar}
      />
    </>
  )
}
