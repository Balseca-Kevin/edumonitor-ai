import { useEffect, useMemo, useState } from 'react'
import { Link, useOutletContext, useParams } from 'react-router-dom'
import { formatearFecha, formatearNumero, formatearPorcentaje } from '../../components/formato'
import { asociarTemas, crearTema, obtenerMapeo, obtenerTematicas } from '../../services/tematicas'
import { describirError } from '../../services/errores'

/** Desempeño de un tema: la cifra se abre para ver de qué notas sale (estudiante, actividad y nota). */
function DesempenoTema({ desempeno, actividades }) {
  const [abierto, setAbierto] = useState(false)
  if (!desempeno) return <span className="ayuda">Sin actividades asociadas</span>
  const nombreActividad = (id) => actividades.find((a) => a.id === id)?.nombre ?? id
  return (
    <div className="mapa-desempeno">
      <button type="button" className="enlace num" aria-expanded={abierto} onClick={() => setAbierto(!abierto)}>
        {desempeno.domina === null
          ? 'Todavía sin notas'
          : `Domina ${formatearPorcentaje(desempeno.domina)} · promedio ${formatearNumero(desempeno.promedio)} · ${desempeno.bajo6.length} bajo 6,0`}
      </button>
      {abierto && (
        <div className="mapa-detalle">
          <p className="ayuda">
            Sale de:{' '}
            {desempeno.evaluaciones
              .map((e) => (e.fuente === 'CRITERIOS' ? `${e.actividad} (criterio: ${e.criterios.join(', ')})` : `${e.actividad} (nota de la actividad)`))
              .join(' · ')}
          </p>
          {desempeno.estudiantes.length === 0 ? (
            <p className="ayuda">Ningún estudiante tiene nota en este tema.</p>
          ) : (
            <table className="tabla mapa-notas num" aria-label={`Notas en ${desempeno.nombre}`}>
              <thead>
                <tr>
                  <th scope="col">Estudiante</th>
                  <th scope="col">Nota en el tema</th>
                  <th scope="col">De dónde sale</th>
                </tr>
              </thead>
              <tbody>
                {desempeno.estudiantes.map((e) => (
                  <tr key={e.id}>
                    <th scope="row">
                      {e.apellido} {e.nombre}
                    </th>
                    <td>
                      <span className={`nota ${e.nota < 6 ? 'nota--bajo' : e.nota < 7 ? 'nota--pendiente' : 'nota--ok'}`}>{formatearNumero(e.nota)}</span>
                    </td>
                    <td className="ayuda">{e.notas.map((n) => `${nombreActividad(n.actividadId)}: ${formatearNumero(n.nota)}`).join(' · ')}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          {desempeno.proximas.length > 0 && (
            <p className="ayuda">Se vuelve a evaluar en: {desempeno.proximas.map((p) => `${p.nombre} (vence ${formatearFecha(p.fechaLimite)})`).join(', ')}</p>
          )}
        </div>
      )}
    </div>
  )
}

function NuevoTema({ cursoId, unidad, onCreado }) {
  const [nombre, setNombre] = useState('')
  const [error, setError] = useState(null)
  const [guardando, setGuardando] = useState(false)

  async function guardar(evento) {
    evento.preventDefault()
    setGuardando(true)
    setError(null)
    try {
      onCreado(await crearTema(cursoId, unidad.id, nombre))
      setNombre('')
    } catch (e) {
      setError(describirError(e).detalle ?? 'No se pudo agregar el tema.')
    } finally {
      setGuardando(false)
    }
  }

  return (
    <form className="mapa-nuevo-tema" onSubmit={guardar}>
      <label>
        <span className="sr-only">Nuevo tema de la unidad {unidad.numero}</span>
        <input value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Agregar un tema" maxLength={200} />
      </label>
      <button type="submit" className="boton boton--secundario boton--chico" disabled={guardando || !nombre.trim()}>
        Agregar
      </button>
      {error && (
        <span className="ayuda mapa-error" role="alert">
          {error}
        </span>
      )}
    </form>
  )
}

/** Temas de una actividad y tema de cada criterio de su rúbrica, con «Guardar» cuando cambian. */
function ActividadMapeo({ cursoId, actividad, unidades, onGuardada }) {
  const [temas, setTemas] = useState(actividad.temas)
  const [criterios, setCriterios] = useState(() => Object.fromEntries(actividad.criterios.map((c) => [c.id, c.temaId])))
  const [estado, setEstado] = useState(null)
  const todos = unidades.flatMap((u) => u.temas.map((t) => ({ ...t, unidad: u.numero })))
  const propios = unidades.find((u) => u.numero === actividad.unidad)?.temas ?? []
  const otros = todos.filter((t) => temas.includes(t.id) && !propios.some((p) => p.id === t.id))
  const cambio =
    JSON.stringify([...temas].sort()) !== JSON.stringify([...actividad.temas].sort()) ||
    actividad.criterios.some((c) => (criterios[c.id] ?? null) !== (c.temaId ?? null))

  function alternar(id) {
    setTemas((actuales) => (actuales.includes(id) ? actuales.filter((t) => t !== id) : [...actuales, id]))
  }

  async function guardar() {
    setEstado('guardando')
    try {
      const guardada = await asociarTemas(
        cursoId,
        actividad.id,
        temas,
        actividad.criterios.map((c) => ({ id: c.id, temaId: criterios[c.id] ?? null })),
      )
      setTemas(guardada.temas)
      setEstado('guardado')
      onGuardada(guardada)
    } catch (e) {
      setEstado(describirError(e).detalle ?? 'No se pudo guardar.')
    }
  }

  return (
    <li className="mapa-actividad">
      <div className="mapa-actividad-cabeza">
        <div>
          <Link to={`/cursos/${cursoId}/actividades/${actividad.id}`} className="mapa-actividad-enlace">
            <b>{actividad.nombre}</b>
          </Link>
          <span className="mono">
            {actividad.categoria ?? 'Sin categoría'}
            {actividad.fechaLimite ? ` · vence ${formatearFecha(actividad.fechaLimite)}` : ''}
          </span>
        </div>
        {cambio && (
          <button type="button" className="boton boton--chico" onClick={guardar} disabled={estado === 'guardando'}>
            {estado === 'guardando' ? 'Guardando…' : 'Guardar'}
          </button>
        )}
        {!cambio && estado === 'guardado' && (
          <span className="ayuda" role="status">
            Guardado
          </span>
        )}
      </div>
      <fieldset className="mapa-temas">
        <legend className="sr-only">Temas que evalúa {actividad.nombre}</legend>
        {propios.length === 0 && <span className="ayuda">La unidad todavía no tiene temas.</span>}
        {[...propios, ...otros].map((t) => (
          <label key={t.id} className="mapa-chip">
            <input type="checkbox" checked={temas.includes(t.id)} onChange={() => alternar(t.id)} />
            {t.nombre}
          </label>
        ))}
      </fieldset>
      {actividad.criterios.length > 0 && (
        <div className="mapa-criterios">
          <span className="mono">CRITERIOS DE LA RÚBRICA</span>
          {actividad.criterios.map((c) => (
            <label key={c.id} className="mapa-criterio">
              <span>{c.descripcion}</span>
              <select
                aria-label={`Tema del criterio ${c.descripcion}`}
                value={criterios[c.id] ?? ''}
                onChange={(e) => setCriterios((actuales) => ({ ...actuales, [c.id]: e.target.value ? Number(e.target.value) : null }))}
              >
                <option value="">Sin tema</option>
                {unidades.map((u) => (
                  <optgroup key={u.id} label={`U${u.numero} · ${u.nombre}`}>
                    {u.temas.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.nombre}
                      </option>
                    ))}
                  </optgroup>
                ))}
              </select>
            </label>
          ))}
        </div>
      )}
      {typeof estado === 'string' && !['guardando', 'guardado'].includes(estado) && (
        <span className="ayuda mapa-error" role="alert">
          {estado}
        </span>
      )}
    </li>
  )
}

/** Tramo de color de una nota en el mapa de calor: bajo 6,0, de 6,0 a 6,99, o 7,0 o más. */
function tramoCalor(nota) {
  if (nota === null || nota === undefined) return 'vacio'
  if (nota < 6) return 'bajo'
  if (nota < 7) return 'medio'
  return 'ok'
}

/**
 * HU-22 · Mapa de calor estudiante × tema: cada celda es la nota del estudiante en el tema que calcula el backend («Desempeño por
 * tema»). Una celda sin nota dice «sin nota», nunca 0. Cada fila abre la ficha del estudiante.
 */
export function MapaCalor({ cursoId, temas }) {
  const conNotas = temas.filter((t) => t.estudiantes.length > 0)
  if (conNotas.length === 0) return null
  const estudiantes = new Map()
  for (const t of conNotas) {
    for (const e of t.estudiantes) {
      if (!estudiantes.has(e.id)) estudiantes.set(e.id, { id: e.id, nombre: e.nombre, apellido: e.apellido, notas: new Map() })
      estudiantes.get(e.id).notas.set(t.id, e.nota)
    }
  }
  const filas = [...estudiantes.values()].sort((a, b) => a.apellido.localeCompare(b.apellido) || a.nombre.localeCompare(b.nombre))
  return (
    <section className="hoja mapa-calor-seccion" aria-labelledby="titulo-calor">
      <h3 id="titulo-calor">Nota de cada estudiante en cada tema</h3>
      <p className="ayuda">Bajo 6,0 en arena, de 6,0 a 6,99 en gris, 7,0 o más en verde. Haz clic en un estudiante para abrir su ficha.</p>
      <div className="tabla-envoltura">
        <table className="mapa-calor num" aria-label="Mapa de calor estudiante por tema">
          <thead>
            <tr>
              <th scope="col">Estudiante</th>
              {conNotas.map((t) => (
                <th key={t.id} scope="col" title={t.nombre}>
                  {t.nombre}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {filas.map((e) => (
              <tr key={e.id}>
                <th scope="row">
                  <Link to={`/cursos/${cursoId}/estudiantes/${e.id}`}>
                    {e.apellido}, {e.nombre}
                  </Link>
                </th>
                {conNotas.map((t) => {
                  const nota = e.notas.get(t.id)
                  return (
                    <td key={t.id} className={`mapa-calor-celda mapa-calor-celda--${tramoCalor(nota)}`}>
                      {nota === undefined || nota === null ? 'sin nota' : formatearNumero(nota)}
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

/**
 * HU-08 y HU-22 · Mapa del curso (docs/sprint-3/diseno.md): parciales, unidades con los temas del sílabo y las actividades de cada
 * unidad. El docente asocia cada actividad y cada criterio de rúbrica a sus temas (se guarda en EduMonitor, nunca en Moodle), y
 * cada tema muestra cuánto lo domina el curso; la cifra se abre para ver de qué notas sale. Todo sale de la API.
 */
export default function Mapa() {
  const { id } = useParams()
  const { version = 0 } = useOutletContext() ?? {}
  const clave = `${id}|${version}`
  const [cargado, setCargado] = useState({ clave: null, mapeo: null, tematicas: null, error: null })
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    let vigente = true
    Promise.all([obtenerMapeo(id), obtenerTematicas(id).catch(() => null)])
      .then(([mapeo, tematicas]) => vigente && setCargado({ clave, mapeo, tematicas, error: null }))
      .catch((e) => vigente && setCargado({ clave, mapeo: null, tematicas: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id, clave, revision])

  const { mapeo, tematicas, error } = cargado.clave === clave ? cargado : { mapeo: null, tematicas: null, error: null }
  const desempenoDe = useMemo(() => new Map((tematicas?.temas ?? []).map((t) => [t.id, t])), [tematicas])

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
  if (!mapeo) return <p className="ayuda">Cargando el mapa del curso…</p>

  const recargar = () => setRevision((r) => r + 1)
  const parciales = [...new Set(mapeo.unidades.map((u) => u.parcial))].sort()
  const sinUnidad = mapeo.actividades.filter((a) => a.unidad === null)

  return (
    <section className="mapa" aria-labelledby="titulo-mapa">
      <div className="libro-cabecera">
        <div>
          <h2 id="titulo-mapa">Mapa del curso</h2>
          <p className="ayuda">
            Asocia cada actividad y cada criterio de la rúbrica a los temas de su unidad. Con eso se calcula cuánto domina el curso cada
            tema y el plan de refuerzo.
          </p>
        </div>
        {tematicas?.sinTemas?.length > 0 && (
          <span className="libro-nuevas" role="status">
            {tematicas.sinTemas.length} {tematicas.sinTemas.length === 1 ? 'actividad con notas no tiene' : 'actividades con notas no tienen'} temas
          </span>
        )}
      </div>

      {parciales.map((p) => (
        <div key={p} className="mapa-parcial">
          <h3 className="mono">PARCIAL {p}</h3>
          {mapeo.unidades
            .filter((u) => u.parcial === p)
            .map((u) => {
              const actividades = mapeo.actividades.filter((a) => a.unidad === u.numero)
              return (
                <article key={u.id} className="hoja mapa-unidad" aria-label={`U${u.numero} · ${u.nombre}`}>
                  <header className="mapa-unidad-cabeza">
                    <h4>
                      U{u.numero} · {u.nombre}
                    </h4>
                    <span className="ayuda num">
                      {actividades.length} {actividades.length === 1 ? 'actividad' : 'actividades'}
                    </span>
                  </header>
                  <ul className="mapa-temas-unidad">
                    {u.temas.map((t) => (
                      <li key={t.id}>
                        <b>{t.nombre}</b>
                        <DesempenoTema desempeno={desempenoDe.get(t.id) ?? null} actividades={mapeo.actividades} />
                      </li>
                    ))}
                  </ul>
                  <NuevoTema cursoId={id} unidad={u} onCreado={recargar} />
                  <ul className="mapa-actividades">
                    {actividades.map((a) => (
                      <ActividadMapeo key={`${a.id}|${a.temas.join(',')}`} cursoId={id} actividad={a} unidades={mapeo.unidades} onGuardada={recargar} />
                    ))}
                  </ul>
                </article>
              )
            })}
        </div>
      ))}

      {sinUnidad.length > 0 && (
        <article className="hoja mapa-unidad" aria-label="Sin unidad">
          <header className="mapa-unidad-cabeza">
            <h4>Sin unidad</h4>
          </header>
          <ul className="mapa-actividades">
            {sinUnidad.map((a) => (
              <ActividadMapeo key={`${a.id}|${a.temas.join(',')}`} cursoId={id} actividad={a} unidades={mapeo.unidades} onGuardada={recargar} />
            ))}
          </ul>
        </article>
      )}
      <MapaCalor cursoId={id} temas={tematicas?.temas ?? []} />
      <p className="fuente">Las notas y las rúbricas son las del aula virtual, de la última sincronización.</p>
    </section>
  )
}
