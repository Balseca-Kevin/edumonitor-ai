import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useOutletContext, useParams, useSearchParams } from 'react-router-dom'
import FichaLateral from '../../components/FichaLateral'
import Estado from '../../components/Estado'
import { formatearFecha, formatearNumero } from '../../components/formato'
import { nombreTramo, useTramo } from '../../components/tramo'
import { actividadesDelLibro, obtenerLibro } from '../../services/libro'
import { obtenerIndicadores } from '../../services/indicadores'
import { obtenerCambiosSincronizacion } from '../../services/cursos'
import { describirError } from '../../services/errores'

const NO_DISPONIBLE = 'no disponible'
const APROBACION = 7

/** Texto y clase de cada celda: cero, sin entrega, por calificar y «aún no vence» se distinguen; sin dato dice «no disponible». */
export function celda(nota) {
  switch (nota?.estado) {
    case 'CALIFICADO':
      return { texto: formatearNumero(nota.nota), clase: nota.nota < APROBACION ? 'nota--bajo' : 'nota--ok' }
    case 'SIN_ENTREGA':
      return { texto: 'Sin entrega', clase: 'nota--sin-entrega' }
    case 'PENDIENTE':
      return { texto: 'Por calificar', clase: 'nota--pendiente' }
    case 'AUN_NO_VENCE':
      return { texto: 'Aún no vence', clase: 'nota--aun-no-vence' }
    default:
      return { texto: NO_DISPONIBLE, clase: 'nota--no-disponible' }
  }
}

const TIPO_CAMBIO = {
  NOTA_NUEVA: 'nota nueva',
  NOTA_CAMBIADA: 'nota cambiada',
  ENTREGA_NUEVA: 'entrega nueva',
  OTRO: 'cambió',
}

function Leyenda() {
  return (
    <div className="libro-leyenda" aria-label="Leyenda del libro">
      <span className="nota nota--bajo">Bajo 7,0</span>
      <span className="nota nota--sin-entrega">Sin entrega (vencida)</span>
      <span className="nota nota--pendiente">Por calificar</span>
      <span className="nota nota--aun-no-vence">Aún no vence</span>
      <span className="nota nota--ok nota--nueva">Nuevo en la última sincronización</span>
    </div>
  )
}

/**
 * HU-21 · TA-21.2 · Libro de calificaciones, primera pantalla del curso (docs/sprint-3/diseno.md): encabezado de dos filas (unidad
 * con su parcial, y actividad con su categoría y su semana del período), la nota de cada estudiante en cada actividad tal como está
 * en Moodle, con cero, sin entrega, por calificar y «aún no vence» diferenciados, y el promedio 40/30/30. Un clic en la fila abre
 * la ficha. TA-21.A2 · las celdas que cambiaron en la última sincronización llevan el anillo verde petróleo. Todo sale de la API:
 * GET /api/cursos/{id}/libro para el tramo, la semana de GET /api/cursos/{id}/periodo y los cambios de
 * GET /api/cursos/{id}/sync/{sid}/cambios.
 */
export default function Libro() {
  const { id } = useParams()
  const tramo = useTramo()
  const { curso, version = 0, periodo } = useOutletContext() ?? {}
  const [busqueda, setBusqueda] = useState('')
  const [orden, setOrden] = useState('apellido')
  const clave = `${id}|${tramo}|${version}|${busqueda.trim()}|${orden}`
  const [cargado, setCargado] = useState({ clave: null, datos: null, error: null })
  const [cambios, setCambios] = useState({ sid: null, datos: null })
  const [indicadores, setIndicadores] = useState({ clave: null, datos: null })
  const [abierto, setAbierto] = useState(null)
  const sid = curso?.ultimaSincronizacion?.id ?? null
  // TA-23.A2 · Una cifra del asistente abre el libro con ?estudiante= y ?actividad=: la fila y la celda quedan señaladas.
  const [parametros] = useSearchParams()
  const senalado = Number(parametros.get('estudiante')) || null
  const actividadSenalada = Number(parametros.get('actividad')) || null

  useEffect(() => {
    let vigente = true
    obtenerLibro(id, { tramo, busqueda: busqueda.trim() || undefined, orden: orden === 'apellido' ? undefined : orden })
      .then((datos) => vigente && setCargado({ clave, datos, error: null }))
      .catch((e) => vigente && setCargado({ clave, datos: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id, tramo, busqueda, orden, clave])

  useEffect(() => {
    if (!sid) return
    let vigente = true
    obtenerCambiosSincronizacion(id, sid)
      .then((datos) => vigente && setCambios({ sid, datos }))
      .catch(() => vigente && setCambios({ sid, datos: null }))
    return () => {
      vigente = false
    }
  }, [id, sid])

  const nuevas = useMemo(() => {
    const celdas = cambios.sid === sid ? (cambios.datos?.celdas ?? []) : []
    return new Map(celdas.map((c) => [`${c.actividadId}|${c.estudiante.id}`, c]))
  }, [cambios, sid])

  const semanaDe = useMemo(() => new Map((periodo?.actividades ?? []).map((a) => [a.id, a.semana])), [periodo])

  const claveFicha = `${id}|${tramo}|${version}`
  const abrir = useCallback(
    (estudianteId) => {
      setAbierto(estudianteId)
      if (indicadores.clave !== claveFicha) {
        obtenerIndicadores(id, tramo)
          .then((datos) => setIndicadores({ clave: claveFicha, datos }))
          .catch(() => setIndicadores({ clave: claveFicha, datos: null }))
      }
    },
    [claveFicha, id, indicadores.clave, tramo],
  )
  const cerrar = useCallback(() => setAbierto(null), [])

  const { datos, error } = cargado.clave === clave ? cargado : { datos: null, error: null }

  useEffect(() => {
    if (!senalado || !datos) return
    const fila = document.getElementById(`libro-fila-${senalado}`)
    if (fila && typeof fila.scrollIntoView === 'function') fila.scrollIntoView({ block: 'center' })
  }, [senalado, datos])

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

  const columnas = actividadesDelLibro(datos)
  const unidades = (datos?.parciales ?? []).flatMap((p) => p.unidades.map((u) => ({ ...u, parcial: p.numero })))
  const nombreDelTramo = nombreTramo(tramo, unidades)
  const fila = datos?.estudiantes.find((e) => e.id === abierto)
  const estudianteFicha =
    indicadores.clave === claveFicha ? (indicadores.datos?.estudiantes.find((e) => e.id === abierto) ?? null) : null
  const celdasFicha = fila ? columnas.map((a, i) => ({ ...fila.notas[i], nombre: a.nombre, fechaLimite: a.fechaLimite })) : []
  const totalNuevas = nuevas.size

  return (
    <>
      <section className="libro" aria-labelledby="titulo-libro">
        <div className="libro-cabecera">
          <div>
            <h2 id="titulo-libro">Libro de calificaciones</h2>
            <p className="ayuda">Cada nota es la misma que está en el aula virtual. Haz clic en un estudiante para abrir su ficha.</p>
          </div>
          <Leyenda />
        </div>

        <div className="libro-filtros">
          <label className="campo-busqueda">
            <span className="sr-only">Buscar estudiante</span>
            <input type="search" placeholder="Buscar por nombre o apellido" value={busqueda} onChange={(e) => setBusqueda(e.target.value)} />
          </label>
          <label className="libro-orden">
            <span>Ordenar por</span>
            <select value={orden} onChange={(e) => setOrden(e.target.value)}>
              <option value="apellido">Apellido</option>
              <option value="promedio">Promedio, de menor a mayor</option>
            </select>
          </label>
          {totalNuevas > 0 && (
            <span className="libro-nuevas num" role="status">
              {totalNuevas} {totalNuevas === 1 ? 'celda cambió' : 'celdas cambiaron'} en la última sincronización
            </span>
          )}
        </div>

        {datos?.noDisponibles?.includes('estadosEntrega') && (
          <div className="aviso aviso--atencion" role="alert">
            <strong>Entregas sin actualizar</strong>
            La última sincronización no pudo leer las entregas: «sin entrega» y «por calificar» pueden no estar al día.
          </div>
        )}

        {!datos ? (
          <p className="ayuda">Cargando el libro…</p>
        ) : columnas.length === 0 ? (
          <p className="ayuda hoja-vacia">No hay actividades en {nombreDelTramo}.</p>
        ) : (
          <div className="hoja libro-hoja">
            <table className="libro-tabla num" aria-label={`Libro de calificaciones · ${nombreDelTramo}`}>
              <thead>
                <tr>
                  <th rowSpan={2} scope="col" className="libro-fijo">
                    Estudiante
                  </th>
                  {unidades.map((u) => (
                    <th key={`${u.parcial}-${u.numero}`} colSpan={u.actividades.length} scope="colgroup" className="libro-unidad">
                      {u.numero === 0 ? 'Sin unidad' : `P${u.parcial} · U${u.numero} · ${u.nombre}`}
                    </th>
                  ))}
                  <th rowSpan={2} scope="col" className="libro-promedio">
                    Promedio 40/30/30
                  </th>
                </tr>
                <tr>
                  {columnas.map((a) => (
                    <th key={a.id} scope="col" className="libro-actividad" title={a.fechaLimite ? `Vence ${formatearFecha(a.fechaLimite)}` : undefined}>
                      {a.nombre}
                      <span className="mono">
                        {a.categoria}
                        {semanaDe.get(a.id) ? ` · S${semanaDe.get(a.id)}` : ''}
                      </span>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {datos.estudiantes.length === 0 && (
                  <tr>
                    <td colSpan={columnas.length + 2} className="ayuda">
                      Ningún estudiante coincide con «{busqueda.trim()}».
                    </td>
                  </tr>
                )}
                {datos.estudiantes.map((e) => {
                  const promedio = formatearNumero(e.promedioPonderado)
                  return (
                    <tr
                      key={e.id}
                      id={`libro-fila-${e.id}`}
                      className={`libro-fila${e.id === senalado ? ' libro-fila--senalada' : ''}`}
                      aria-current={e.id === senalado ? 'true' : undefined}
                      onClick={() => abrir(e.id)}
                    >
                      <th scope="row" className="libro-fijo">
                        <button
                          type="button"
                          className="enlace-fila"
                          onClick={(evento) => {
                            evento.stopPropagation()
                            abrir(e.id)
                          }}
                        >
                          {e.apellido} {e.nombre}
                        </button>
                      </th>
                      {e.notas.map((n, i) => {
                        const { texto, clase } = celda(n)
                        const cambio = nuevas.get(`${n.actividadId}|${e.id}`)
                        return (
                          <td key={n.actividadId ?? i}>
                            <span
                              className={`nota ${clase}${cambio ? ' nota--nueva' : ''}${e.id === senalado && n.actividadId === actividadSenalada ? ' nota--senalada' : ''}`}
                              title={cambio ? `Nuevo en la última sincronización: ${TIPO_CAMBIO[cambio.tipo] ?? 'cambió'}` : undefined}
                            >
                              {texto}
                              {cambio && <span className="sr-only"> · nuevo en la última sincronización</span>}
                            </span>
                          </td>
                        )
                      })}
                      <td className="libro-promedio">
                        <span className={`nota ${promedio === null ? 'nota--no-disponible' : e.promedioPonderado < APROBACION ? 'nota--bajo' : 'nota--ok'}`}>
                          {promedio ?? NO_DISPONIBLE}
                          {promedio !== null && e.parcial ? '*' : ''}
                        </span>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}

        {datos && (
          <p className="fuente">
            Datos del aula virtual, sincronización del {formatearFecha(datos.sincronizacion?.fecha)} ·{' '}
            <Estado estado={datos.sincronizacion?.estado} />. El promedio pondera exámenes 40 %, tareas 30 % y APE 30 %; * faltó una
            categoría y se calculó con las que hay.
          </p>
        )}
      </section>

      <FichaLateral
        cursoId={id}
        estudiante={estudianteFicha}
        requiereAtencion={null}
        celdas={celdasFicha}
        tramo={tramo}
        nombreTramo={nombreDelTramo}
        onCerrar={cerrar}
      />
    </>
  )
}
