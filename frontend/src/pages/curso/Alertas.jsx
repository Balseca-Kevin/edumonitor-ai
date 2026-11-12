import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { formatearFecha } from '../../components/formato'
import { DESCRIPCION_REGLA, textoEvidencia } from '../../components/lenguaje'
import { actualizarReglas, marcarAlerta, obtenerAlertas, obtenerReglas } from '../../services/alertas'
import { describirError } from '../../services/errores'

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
      <div className="aviso aviso--error" role="alert">
        <strong>{error.titulo}</strong>
        {error.detalle}
      </div>
    )
  }

  if (reglasVigentes.datos === null || listaVigente.datos === null) {
    return <p className="ayuda">Cargando alertas…</p>
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

  const nombreRegla = (codigo) => reglasVigentes.datos.find((r) => r.codigo === codigo)?.nombre ?? 'Regla del curso'

  return (
    <div className="panel-medio">
      <section className="seccion ancho-2" aria-labelledby="titulo-alertas">
        <div className="regla" />
        <div className="seccion-cabecera">
          <h2 id="titulo-alertas" className="seccion-titulo">
            Alertas
          </h2>
          <div className="alertas-filtros" role="group" aria-label="Filtrar alertas por estado">
            <button
              type="button"
              className={`chip${filtro === 'ACTIVA' ? ' chip--activo' : ''}`}
              aria-pressed={filtro === 'ACTIVA'}
              onClick={() => setFiltro('ACTIVA')}
            >
              Activas
            </button>
            <button
              type="button"
              className={`chip${filtro === 'REVISADA' ? ' chip--activo' : ''}`}
              aria-pressed={filtro === 'REVISADA'}
              onClick={() => setFiltro('REVISADA')}
            >
              Revisadas
            </button>
          </div>
        </div>

        {aviso && (
          <div className={`aviso aviso--${aviso.tipo === 'ok' ? 'ok' : 'error'}`} role={aviso.tipo === 'ok' ? 'status' : 'alert'}>
            <strong>{aviso.titulo}</strong>
            {aviso.detalle}
          </div>
        )}

        {listaVigente.datos.length === 0 ? (
          <p className="ayuda">
            {filtro === 'ACTIVA'
              ? 'No hay alertas activas. Al sincronizar el curso, las reglas detectan quiénes requieren atención.'
              : 'Todavía no hay alertas revisadas.'}
          </p>
        ) : (
          <div className="alertas-lista">
            {agruparPorEstudiante(listaVigente.datos).map(([grupo, alertas], i) => (
              <div key={grupo} className="alerta-grupo entra" style={{ animationDelay: `${Math.min(i * 40, 1200)}ms` }}>
                <div>
                  <h3>{grupo}</h3>
                  <span className="alerta-titulo">{filtro === 'ACTIVA' ? 'Requiere atención' : 'Revisada'}</span>
                </div>
                <div className="alerta-grupo-items">
                  {alertas.map((alerta) => (
                    <article key={alerta.id} className="alerta" aria-label={`${nombreRegla(alerta.regla)} de ${grupo}`}>
                      <div>
                        <span className="alerta-regla">{nombreRegla(alerta.regla)}</span>
                        <span className="alerta-evidencia">{textoEvidencia(alerta)}</span>
                        <span className="alerta-fecha">
                          Detectada el {formatearFecha(alerta.generada)}
                          {alerta.revisada && ` · revisada el ${formatearFecha(alerta.revisada)}`}
                        </span>
                      </div>
                      {alerta.estado === 'ACTIVA' && (
                        <button
                          type="button"
                          className="boton boton--secundario boton--chico"
                          disabled={marcando === alerta.id}
                          onClick={() => revisar(alerta)}
                        >
                          {marcando === alerta.id ? 'Marcando…' : 'Marcar como revisada'}
                        </button>
                      )}
                    </article>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      <section className="seccion" aria-labelledby="titulo-umbrales">
        <div className="regla" style={{ animationDelay: '80ms' }} />
        <h2 id="titulo-umbrales" className="seccion-titulo">
          Cuándo avisar
        </h2>
        <form className="umbrales" onSubmit={guardar}>
          <p className="ayuda">Al guardar, las alertas del curso se recalculan con los valores nuevos.</p>
          {reglasVigentes.datos.map((regla) => (
            <div key={regla.codigo} className="umbral">
              <label htmlFor={`umbral-${regla.codigo}`}>{regla.nombre}</label>
              <input
                id={`umbral-${regla.codigo}`}
                className="campo"
                type="number"
                step="0.1"
                inputMode="decimal"
                value={umbrales[regla.codigo] ?? ''}
                onChange={(e) => setUmbrales((u) => ({ ...u, [regla.codigo]: e.target.value }))}
                aria-describedby={`condicion-${regla.codigo}`}
              />
              <span id={`condicion-${regla.codigo}`} className="ayuda">
                {DESCRIPCION_REGLA[regla.codigo] ?? regla.condicion}
              </span>
            </div>
          ))}
          <button type="submit" className="boton" disabled={guardando}>
            {guardando ? 'Guardando…' : 'Guardar'}
          </button>
        </form>
      </section>
    </div>
  )
}
