import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import Estado from '../../components/s2/Estado'
import { formatearFecha, formatearNumero } from '../../components/s2/formato'
import { actualizarReglas, marcarAlerta, obtenerAlertas, obtenerReglas } from '../../services/alertas'
import { describirError } from '../../services/errores'

/** Línea de evidencia de una alerta con sus números (docs/contratos-api.md), según la regla que la activó. */
function textoEvidencia(alerta) {
  const e = alerta.evidencia ?? {}
  const numero = (valor) => formatearNumero(valor) ?? 'no disponible'

  switch (alerta.regla) {
    case 'R1':
      return `Últimas ${(e.ultimas ?? []).map(numero).join(' · ')} · anteriores ${(e.anteriores ?? [])
        .map(numero)
        .join(' · ')} · diferencia ${numero(e.diferencia)} · umbral ${numero(e.umbral)}`
    case 'R2':
      return `Actividades vencidas sin entrega: ${(e.actividades ?? [])
        .map((a) => `${a.nombre} (${formatearFecha(a.fechaLimite)})`)
        .join(' · ')} · umbral ${numero(e.umbral)}`
    case 'R4':
      return `Promedio ${numero(e.promedio)} · ${e.notas ?? 'no disponible'} notas · umbral ${numero(e.umbral)}`
    default:
      return '—'
  }
}

/** Alertas agrupadas por estudiante, en el orden en que llegan del backend. */
function agruparPorEstudiante(alertas) {
  const grupos = new Map()
  for (const alerta of alertas) {
    const clave = `${alerta.estudiante.apellido}, ${alerta.estudiante.nombre}`
    if (!grupos.has(clave)) grupos.set(clave, [])
    grupos.get(clave).push(alerta)
  }
  return [...grupos.entries()]
}

/**
 * HU-12 · Pantalla Alertas (diseno.md): a la izquierda, la lista de alertas agrupada por estudiante con su evidencia
 * y el botón «Marcar como revisada»; a la derecha, el panel de umbrales con un campo por regla y «Guardar», que
 * recalcula las alertas en el backend. El título de toda alerta es «Requiere atención», nunca «reprobará».
 */
export default function Alertas() {
  const { id } = useParams()
  const [reglas, setReglas] = useState({ id: null, datos: null, error: null })
  const [lista, setLista] = useState({ id: null, datos: null, error: null })
  const [filtro, setFiltro] = useState('ACTIVA')
  const [umbrales, setUmbrales] = useState({})
  const [guardando, setGuardando] = useState(false)
  const [marcando, setMarcando] = useState(null)
  const [aviso, setAviso] = useState(null)

  useEffect(() => {
    let vigente = true
    obtenerReglas(id)
      .then((datos) => {
        if (!vigente) return
        setReglas({ id, datos, error: null })
        setUmbrales(Object.fromEntries(datos.map((r) => [r.codigo, String(r.umbral)])))
      })
      .catch((e) => vigente && setReglas({ id, datos: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id])

  useEffect(() => {
    let vigente = true
    obtenerAlertas(id, filtro)
      .then((datos) => vigente && setLista({ id, filtro, datos, error: null }))
      .catch((e) => vigente && setLista({ id, filtro, datos: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id, filtro])

  // Estado vigente: si cambió el curso o el filtro, lo que llegó antes no cuenta y se ve «Cargando…».
  const reglasVigentes = reglas.id === id ? reglas : { id, datos: null, error: null }
  const listaVigente = lista.id === id && lista.filtro === filtro ? lista : { id, datos: null, error: null }

  const error = reglasVigentes.error ?? listaVigente.error
  if (error) {
    return (
      <div className="s2-aviso s2-aviso--error" role="alert">
        <strong>{error.titulo}</strong>
        {error.detalle}
      </div>
    )
  }

  if (reglasVigentes.datos === null || listaVigente.datos === null) {
    return <p className="s2-ayuda">Cargando alertas…</p>
  }

  async function guardar(event) {
    event.preventDefault()
    const cambios = reglasVigentes.datos.map((r) => ({
      codigo: r.codigo,
      umbral: Number(String(umbrales[r.codigo] ?? '').replace(',', '.')),
      activa: r.activa,
    }))

    if (cambios.some((c) => String(umbrales[c.codigo] ?? '').trim() === '' || Number.isNaN(c.umbral))) {
      setAviso({ tipo: 'error', titulo: 'Umbral no válido', detalle: 'Captura un número en cada campo antes de guardar.' })
      return
    }

    setGuardando(true)
    setAviso(null)
    try {
      await actualizarReglas(id, cambios)
      const [nuevasReglas, nuevasAlertas] = await Promise.all([obtenerReglas(id), obtenerAlertas(id, filtro)])
      setReglas({ id, datos: nuevasReglas, error: null })
      setUmbrales(Object.fromEntries(nuevasReglas.map((r) => [r.codigo, String(r.umbral)])))
      setLista({ id, filtro, datos: nuevasAlertas, error: null })
      setAviso({ tipo: 'ok', titulo: 'Umbrales guardados', detalle: 'Las alertas del curso se recalcularon.' })
    } catch (e) {
      setAviso({ tipo: 'error', ...describirError(e) })
    } finally {
      setGuardando(false)
    }
  }

  async function revisar(alerta) {
    setMarcando(alerta.id)
    setAviso(null)
    try {
      await marcarAlerta(id, alerta.id, 'REVISADA')
      const nuevasAlertas = await obtenerAlertas(id, filtro)
      setLista({ id, filtro, datos: nuevasAlertas, error: null })
      setAviso({ tipo: 'ok', titulo: 'Alerta marcada como revisada', detalle: 'Ya no aparece en las alertas activas.' })
    } catch (e) {
      setAviso({ tipo: 'error', ...describirError(e) })
    } finally {
      setMarcando(null)
    }
  }

  const nombreRegla = (codigo) => reglasVigentes.datos.find((r) => r.codigo === codigo)?.nombre ?? codigo

  return (
    <div className="s2-panel">
      <div className="s2-bento">
        <section className="s2-col-8 s2-seccion s2-seccion--columna" aria-labelledby="titulo-alertas">
          <h3 id="titulo-alertas" className="s2-seccion-titulo">
            Alertas
          </h3>

          <div className="s2-alertas">
            {aviso && (
              <div
                className={`s2-aviso s2-aviso--${aviso.tipo === 'ok' ? 'neutro' : 'error'}`}
                role={aviso.tipo === 'ok' ? 'status' : 'alert'}
              >
                <strong>{aviso.titulo}</strong>
                {aviso.detalle}
              </div>
            )}

            <div className="s2-alerta-filtros" role="group" aria-label="Filtrar alertas por estado">
              <button
                type="button"
                className={`s2-boton s2-boton--chico${filtro === 'ACTIVA' ? '' : ' s2-boton--secundario'}`}
                aria-pressed={filtro === 'ACTIVA'}
                onClick={() => setFiltro('ACTIVA')}
              >
                Activas
              </button>
              <button
                type="button"
                className={`s2-boton s2-boton--chico${filtro === 'REVISADA' ? '' : ' s2-boton--secundario'}`}
                aria-pressed={filtro === 'REVISADA'}
                onClick={() => setFiltro('REVISADA')}
              >
                Revisadas
              </button>
            </div>

            {listaVigente.datos.length === 0 ? (
              <p className="s2-ayuda">
                {filtro === 'ACTIVA'
                  ? 'No hay alertas activas. Al sincronizar el curso, las reglas detectan quiénes requieren atención.'
                  : 'Todavía no hay alertas revisadas.'}
              </p>
            ) : (
              agruparPorEstudiante(listaVigente.datos).map(([grupo, alertas]) => (
                <div key={grupo} className="s2-alerta-grupo">
                  <h4 className="s2-tarjeta-titulo">{grupo}</h4>
                  {alertas.map((alerta) => (
                    <article key={alerta.id} className="s2-tarjeta">
                      <Estado estado={alerta.estado} texto={alerta.titulo} />
                      <p>
                        <span className="s2-grupo">{alerta.regla}</span> {nombreRegla(alerta.regla)}
                      </p>
                      <p className="s2-contexto s2-num">{textoEvidencia(alerta)}</p>
                      <div className="s2-alerta-pie">
                        <span className="s2-ayuda s2-num">
                          <Estado estado={alerta.estado} texto={alerta.estado === 'ACTIVA' ? 'Activa' : 'Revisada'} /> ·
                          Generada {formatearFecha(alerta.generada)}
                          {alerta.revisada && ` · Revisada ${formatearFecha(alerta.revisada)}`}
                        </span>
                        {alerta.estado === 'ACTIVA' && (
                          <button
                            type="button"
                            className="s2-boton s2-boton--secundario s2-boton--chico"
                            disabled={marcando === alerta.id}
                            onClick={() => revisar(alerta)}
                          >
                            {marcando === alerta.id ? 'Marcando…' : 'Marcar como revisada'}
                          </button>
                        )}
                      </div>
                    </article>
                  ))}
                </div>
              ))
            )}
          </div>
        </section>

        <section className="s2-col-4 s2-seccion s2-seccion--columna" aria-labelledby="titulo-umbrales">
          <h3 id="titulo-umbrales" className="s2-seccion-titulo">
            Umbrales
          </h3>

          <form className="s2-umbrales" onSubmit={guardar}>
            <p className="s2-ayuda">Cada regla compara con su umbral. Al guardar, las alertas se recalculan.</p>
            {reglasVigentes.datos.map((regla) => (
              <div key={regla.codigo} className="s2-umbral">
                <label htmlFor={`umbral-${regla.codigo}`} className="s2-ayuda">
                  <span className="s2-grupo">{regla.codigo}</span> {regla.nombre}
                </label>
                <input
                  id={`umbral-${regla.codigo}`}
                  className="s2-campo"
                  type="number"
                  step="0.1"
                  inputMode="decimal"
                  value={umbrales[regla.codigo] ?? ''}
                  onChange={(e) => setUmbrales((u) => ({ ...u, [regla.codigo]: e.target.value }))}
                  aria-describedby={`condicion-${regla.codigo}`}
                />
                <span id={`condicion-${regla.codigo}`} className="s2-ayuda">
                  {regla.condicion}
                </span>
              </div>
            ))}
            <button type="submit" className="s2-boton" disabled={guardando}>
              {guardando ? 'Guardando…' : 'Guardar'}
            </button>
          </form>
        </section>
      </div>
    </div>
  )
}
