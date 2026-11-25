import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import FichaLateral from '../../components/FichaLateral'
import { formatearFecha } from '../../components/formato'
import { DESCRIPCION_REGLA, textoEvidencia } from '../../components/lenguaje'
import { busquedaTramo, nombreTramo, useTramo } from '../../components/tramo'
import { actualizarReglas, marcarAlerta, obtenerAlertas, obtenerReglas } from '../../services/alertas'
import { obtenerIndicadores } from '../../services/indicadores'
import { actividadesDelLibro, obtenerLibro } from '../../services/libro'
import { describirError } from '../../services/errores'

/** Chips por regla, en palabras del docente; el código de la regla es interno y no se muestra. */
const CHIPS = [
  { codigo: null, texto: 'Todas' },
  { codigo: 'R4', texto: 'Promedio bajo' },
  { codigo: 'R1', texto: 'Descenso reciente' },
  { codigo: 'R2', texto: 'Atrasos' },
]
const CLASE_REGLA = { R4: 'chip-bajo', R2: 'chip-sin-entrega', R1: 'chip-descenso' }
const ETIQUETA_UMBRAL = {
  R4: 'Promedio bajo: menor a',
  R1: 'Descenso reciente: baja de al menos (puntos)',
  R2: 'Atrasos: actividades vencidas sin entregar desde',
}

/** Actividades vencidas sin entregar de cada estudiante, tomadas de la evidencia de su alerta de atrasos. */
function deudasPorEstudiante(alertas) {
  const deudas = new Map()
  for (const alerta of alertas) {
    if (alerta.regla !== 'R2') continue
    const nombres = (alerta.evidencia?.actividades ?? []).map((a) => (typeof a === 'string' ? a : a.nombre)).filter(Boolean)
    deudas.set(alerta.estudiante.id, nombres)
  }
  return deudas
}

function ModalUmbrales({ reglas, umbrales, setUmbrales, guardando, aviso, onGuardar, onCerrar }) {
  const primero = useRef(null)
  useEffect(() => {
    primero.current?.focus()
    function alPresionar(evento) {
      if (evento.key === 'Escape') onCerrar()
    }
    window.addEventListener('keydown', alPresionar)
    return () => window.removeEventListener('keydown', alPresionar)
  }, [onCerrar])

  return (
    <div className="modal-fondo">
      <form className="modal hoja" role="dialog" aria-modal="true" aria-labelledby="titulo-umbrales" onSubmit={onGuardar}>
        <div>
          <h2 id="titulo-umbrales">Umbrales de alerta</h2>
          <p className="ayuda">Al guardar, las alertas del curso se recalculan con los valores nuevos.</p>
        </div>
        {reglas.map((regla, i) => (
          <div key={regla.codigo} className="umbral">
            <label htmlFor={`umbral-${regla.codigo}`}>{ETIQUETA_UMBRAL[regla.codigo] ?? regla.nombre}</label>
            <input
              id={`umbral-${regla.codigo}`}
              ref={i === 0 ? primero : undefined}
              className="campo num"
              type="number"
              step={regla.codigo === 'R2' ? '1' : '0.5'}
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
        {aviso?.tipo === 'error' && (
          <div className="aviso aviso--error" role="alert">
            <strong>{aviso.titulo}</strong>
            {aviso.detalle}
          </div>
        )}
        <div className="modal-acciones">
          <button type="submit" className="boton" disabled={guardando}>
            {guardando ? 'Guardando…' : 'Guardar'}
          </button>
          <button type="button" className="boton boton--secundario" onClick={onCerrar}>
            Cancelar
          </button>
        </div>
      </form>
    </div>
  )
}

/**
 * HU-12 y R.5 · Alertas en tarjetas (docs/sprint-3/diseno.md): chips por regla, una tarjeta por alerta con «Requiere atención», la
 * evidencia en una frase con sus números, «Debe: …» con las actividades vencidas sin entregar, «Armar plan de refuerzo» y «Marcar
 * revisada». El nombre abre la ficha lateral. «Umbrales de alerta» abre el modal que guarda y recalcula en el backend. Las alertas
 * son las del tramo elegido en la barra del curso.
 */
export default function Alertas() {
  const { id } = useParams()
  const tramo = useTramo()
  const navigate = useNavigate()
  const [reglas, setReglas] = useState({ id: null, datos: null, error: null })
  const [lista, setLista] = useState({ clave: null, datos: null, error: null })
  const [estado, setEstado] = useState('ACTIVA')
  const [regla, setRegla] = useState(null)
  const [umbrales, setUmbrales] = useState({})
  const [modal, setModal] = useState(false)
  const [guardando, setGuardando] = useState(false)
  const [marcando, setMarcando] = useState(null)
  const [aviso, setAviso] = useState(null)
  const [ficha, setFicha] = useState({ id: null, clave: null, indicadores: null, libro: null })
  const clave = `${id}|${estado}|${tramo}`

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
    obtenerAlertas(id, estado, tramo)
      .then((datos) => vigente && setLista({ clave, datos, error: null }))
      .catch((e) => vigente && setLista({ clave, datos: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id, estado, tramo, clave])

  const cerrarModal = useCallback(() => setModal(false), [])
  const cerrarFicha = useCallback(() => setFicha((f) => ({ ...f, id: null })), [])

  const reglasVigentes = reglas.id === id ? reglas : { datos: null, error: null }
  const listaVigente = lista.clave === clave ? lista : { datos: null, error: null }
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
      const [nuevasReglas, nuevasAlertas] = await Promise.all([obtenerReglas(id), obtenerAlertas(id, estado, tramo)])
      setReglas({ id, datos: nuevasReglas, error: null })
      setUmbrales(Object.fromEntries(nuevasReglas.map((r) => [r.codigo, String(r.umbral)])))
      setLista({ clave, datos: nuevasAlertas, error: null })
      setModal(false)
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
      const nuevasAlertas = await obtenerAlertas(id, estado, tramo)
      setLista({ clave, datos: nuevasAlertas, error: null })
      setAviso({ tipo: 'ok', titulo: 'Alerta marcada como revisada', detalle: 'Ya no aparece en las alertas activas.' })
    } catch (e) {
      setAviso({ tipo: 'error', ...describirError(e) })
    } finally {
      setMarcando(null)
    }
  }

  async function abrirFicha(estudianteId) {
    if (ficha.clave === `${id}|${tramo}`) {
      setFicha((f) => ({ ...f, id: estudianteId }))
      return
    }
    setFicha({ id: estudianteId, clave: `${id}|${tramo}`, indicadores: null, libro: null })
    const [indicadores, libro] = await Promise.all([obtenerIndicadores(id, tramo).catch(() => null), obtenerLibro(id, { tramo }).catch(() => null)])
    setFicha((f) => ({ ...f, indicadores, libro }))
  }

  const nombreRegla = (codigo) => reglasVigentes.datos.find((r) => r.codigo === codigo)?.nombre ?? 'Regla del curso'
  const deudas = deudasPorEstudiante(listaVigente.datos)
  const visibles = listaVigente.datos.filter((a) => regla === null || a.regla === regla)
  const estudianteFicha = ficha.indicadores?.estudiantes.find((e) => e.id === ficha.id) ?? null
  const columnas = actividadesDelLibro(ficha.libro)
  const filaLibro = ficha.libro?.estudiantes.find((e) => e.id === ficha.id)
  const celdas = filaLibro ? columnas.map((a, i) => ({ ...filaLibro.notas[i], nombre: a.nombre, fechaLimite: a.fechaLimite })) : []

  return (
    <div className="alertas-s3">
      <div className="alertas-cabecera">
        <h2>Alertas</h2>
        <div className="chips-filtro" role="group" aria-label="Filtrar alertas por regla">
          {CHIPS.map((c) => (
            <button key={c.texto} type="button" className="chip-filtro" aria-pressed={regla === c.codigo} onClick={() => setRegla(c.codigo)}>
              {c.texto}
            </button>
          ))}
        </div>
        <div className="chips-filtro" role="group" aria-label="Filtrar alertas por estado">
          <button type="button" className="chip-filtro" aria-pressed={estado === 'ACTIVA'} onClick={() => setEstado('ACTIVA')}>
            Activas
          </button>
          <button type="button" className="chip-filtro" aria-pressed={estado === 'REVISADA'} onClick={() => setEstado('REVISADA')}>
            Revisadas
          </button>
        </div>
        <button type="button" className="boton boton--secundario alertas-umbrales" onClick={() => setModal(true)}>
          Umbrales de alerta
        </button>
      </div>

      {aviso && !modal && (
        <div className={`aviso aviso--${aviso.tipo === 'ok' ? 'ok' : 'error'}`} role={aviso.tipo === 'ok' ? 'status' : 'alert'}>
          <strong>{aviso.titulo}</strong>
          {aviso.detalle}
        </div>
      )}

      {visibles.length === 0 ? (
        <div className="hoja hoja-vacia ayuda">
          {listaVigente.datos.length > 0
            ? 'No hay alertas con este filtro.'
            : estado === 'ACTIVA'
              ? `No hay alertas activas en ${nombreTramo(tramo, ficha.indicadores?.unidades)}. Al sincronizar el curso, las reglas detectan quiénes requieren atención.`
              : 'Todavía no hay alertas revisadas.'}
        </div>
      ) : (
        <div className="alertas-rejilla">
          {visibles.map((alerta, i) => {
            const nombre = `${alerta.estudiante.apellido}, ${alerta.estudiante.nombre}`
            const debe = deudas.get(alerta.estudiante.id) ?? []
            return (
              <article
                key={alerta.id}
                className="hoja levanta alerta-tarjeta entra"
                style={{ animationDelay: `${Math.min(i * 40, 1200)}ms` }}
                aria-label={`${nombreRegla(alerta.regla)} de ${nombre}`}
              >
                <div className="alerta-tarjeta-cabeza">
                  <span className={CLASE_REGLA[alerta.regla] ?? 'chip-bajo'}>{nombreRegla(alerta.regla)}</span>
                  <span className="ayuda">Requiere atención</span>
                  {alerta.estado === 'REVISADA' && <span className="alerta-revisada">Revisada</span>}
                </div>
                <button type="button" className="alerta-nombre" onClick={() => abrirFicha(alerta.estudiante.id)}>
                  {nombre}
                </button>
                <p className="alerta-evidencia num">{textoEvidencia(alerta)}</p>
                {debe.length > 0 && (
                  <div className="chips">
                    {debe.map((actividad) => (
                      <span key={actividad} className="chip-sin-entrega">
                        Debe: {actividad}
                      </span>
                    ))}
                  </div>
                )}
                <span className="alerta-fecha">
                  Detectada el {formatearFecha(alerta.generada)}
                  {alerta.revisada && ` · revisada el ${formatearFecha(alerta.revisada)}`}
                </span>
                <div className="alerta-acciones">
                  <button
                    type="button"
                    className="boton boton--ia"
                    onClick={() => navigate(`/cursos/${id}/refuerzos?estudiante=${alerta.estudiante.id}${busquedaTramo(tramo).replace('?', '&')}`)}
                  >
                    Armar plan de refuerzo
                  </button>
                  {alerta.estado === 'ACTIVA' && (
                    <button type="button" className="boton boton--secundario" disabled={marcando === alerta.id} onClick={() => revisar(alerta)}>
                      {marcando === alerta.id ? 'Marcando…' : 'Marcar revisada'}
                    </button>
                  )}
                </div>
              </article>
            )
          })}
        </div>
      )}

      {modal && (
        <ModalUmbrales
          reglas={reglasVigentes.datos}
          umbrales={umbrales}
          setUmbrales={setUmbrales}
          guardando={guardando}
          aviso={aviso}
          onGuardar={guardar}
          onCerrar={cerrarModal}
        />
      )}

      {ficha.id !== null && estudianteFicha && (
        <FichaLateral
          cursoId={id}
          estudiante={estudianteFicha}
          requiereAtencion={estado === 'ACTIVA'}
          celdas={celdas}
          tramo={tramo}
          nombreTramo={nombreTramo(tramo, ficha.indicadores?.unidades)}
          onCerrar={cerrarFicha}
        />
      )}
    </div>
  )
}
