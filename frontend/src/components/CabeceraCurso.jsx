import { useState } from 'react'
import * as Popover from '@radix-ui/react-popover'
import * as Tooltip from '@radix-ui/react-tooltip'
// LazyMotion con el componente m: las funciones de animación se cargan aparte (animacionMotion.js), así el paquete principal no las lleva.
import { AnimatePresence, LazyMotion, MotionConfig } from 'motion/react'
import * as m from 'motion/react-m'

const cargarAnimacion = () => import('./animacionMotion.js').then((modulo) => modulo.default)
import { formatearDiaMes, formatearFecha } from './formato'

const NO_DISPONIBLE = 'no disponible'

/** Posición horizontal (0 a 100 %) del centro de la semana n en la línea de tiempo. Es geometría del dibujo, no un cálculo de fechas. */
const centro = (semana, total) => ((semana - 0.5) / total) * 100
const inicioDe = (semana, total) => ((semana - 1) / total) * 100
const anchoDe = (desde, hasta, total) => ((hasta - desde + 1) / total) * 100

const esExamen = (a) => /^EX/.test(a.sigla ?? '') || a.categoria === 'Exámenes'

/** Tono del marcador según el estado que calcula el backend. */
function tono(a) {
  if (a.estado === 'VENCIDA') return a.faltan > 0 ? 'faltan' : 'completa'
  if (a.estado === 'VENCE_ESTA_SEMANA') return 'esta-semana'
  return 'proxima'
}

/** «Tarea 3 · vence 29/09 · 11 de 12 entregaron»: lo que dice el tooltip y el nombre accesible de cada marcador. */
function textoMarcador(a) {
  const partes = [a.nombre, `vence ${formatearDiaMes(a.fechaLimite)}`]
  if (a.estado === 'PROXIMA') partes.push('próxima')
  else partes.push(`${a.entregaron} de ${a.matriculados} entregaron`)
  return partes.join(' · ')
}

/**
 * C6 · Línea de tiempo ligera: una barra continua por semanas con los parciales y las unidades como segmentos, un marcador por
 * evaluación con fecha (círculo para tarea o APE, rombo para examen) y el marcador «Hoy». Sin etiquetas fijas bajo los marcadores:
 * el nombre, la fecha límite y las entregas salen en un tooltip al pasar el cursor o al enfocar con el teclado. Semanas, estados y
 * unidades vienen de GET /api/cursos/{id}/periodo.
 */
export function LineaTiempo({ periodo, compacta = false }) {
  const total = periodo.semana.total
  const actual = periodo.semana.actual
  const unidades = periodo.unidades.filter((u) => u.semanaDesde !== null && u.semanaHasta !== null)
  const parciales = [...new Set(unidades.map((u) => u.parcial))].map((p) => {
    const delParcial = unidades.filter((u) => u.parcial === p)
    return { numero: p, desde: Math.min(...delParcial.map((u) => u.semanaDesde)), hasta: Math.max(...delParcial.map((u) => u.semanaHasta)) }
  })
  const marcadores = periodo.actividades.filter((a) => a.semana !== null && a.semana >= 1 && a.semana <= total)
  // Varias evaluaciones en la misma semana se separan un poco para que no se tapen.
  const enSemana = new Map()
  const posicionados = marcadores.map((a) => {
    const orden = enSemana.get(a.semana) ?? 0
    enSemana.set(a.semana, orden + 1)
    return { ...a, desplazamiento: orden * 12 }
  })
  const hoy = periodo.estado === 'ANTES' ? 0 : periodo.estado === 'TERMINADO' ? 100 : centro(actual, total)

  return (
    <Tooltip.Provider delayDuration={150}>
      <div
        className={`lt2${compacta ? ' lt2--compacta' : ''}`}
        role="group"
        aria-label={`Línea de tiempo del período: semana ${actual} de ${total}`}
      >
        <div className="lt2-segmentos lt2-segmentos--parcial" aria-hidden="true">
          {parciales.map((p) => (
            <span key={p.numero} className="lt2-parcial mono" style={{ left: `${inicioDe(p.desde, total)}%`, width: `${anchoDe(p.desde, p.hasta, total)}%` }}>
              PARCIAL {p.numero}
            </span>
          ))}
        </div>
        <div className="lt2-segmentos">
          {unidades.map((u) => (
            <span
              key={u.numero}
              className={`lt2-unidad${u.numero === periodo.unidadActual?.numero ? ' lt2-unidad--actual' : ''}`}
              style={{ left: `${inicioDe(u.semanaDesde, total)}%`, width: `${anchoDe(u.semanaDesde, u.semanaHasta, total)}%` }}
              title={`U${u.numero} · ${u.nombre}`}
            >
              U{u.numero}
              {!compacta && <span className="lt2-unidad-nombre"> · {u.nombre}</span>}
            </span>
          ))}
        </div>
        <div className="lt2-pista">
          <div className="lt2-relleno" style={{ width: `${hoy}%` }} aria-hidden="true" />
          {posicionados.map((a) => (
            <Tooltip.Root key={a.id}>
              <Tooltip.Trigger asChild>
                <button
                  type="button"
                  className={`lt2-marcador lt2-marcador--${tono(a)}${esExamen(a) ? ' lt2-marcador--rombo' : ''}`}
                  style={{ left: `calc(${centro(a.semana, total)}% + ${a.desplazamiento}px)` }}
                  aria-label={textoMarcador(a)}
                />
              </Tooltip.Trigger>
              <Tooltip.Portal>
                <Tooltip.Content className="lt2-tooltip" sideOffset={6}>
                  {textoMarcador(a)}
                  <Tooltip.Arrow className="lt2-tooltip-flecha" />
                </Tooltip.Content>
              </Tooltip.Portal>
            </Tooltip.Root>
          ))}
          <div className="lt2-hoy" style={{ left: `${hoy}%` }} aria-label={`Hoy, semana ${actual}`} role="img">
            <span className="mono">Hoy</span>
          </div>
        </div>
        {!compacta && (
          <div className="lt2-semanas mono num" aria-hidden="true">
            <span>Semana 1</span>
            <span>Semana {total}</span>
          </div>
        )}
      </div>
    </Tooltip.Provider>
  )
}

function Bloques({ periodo }) {
  const { vencidas, venceEstaSemana, siguiente } = periodo
  return (
    <div className="periodo-bloques">
      <section className="periodo-bloque" aria-label="Vencidas">
        <span className="mono">VENCIDAS</span>
        <b className="num">
          {vencidas.actividades} {vencidas.actividades === 1 ? 'actividad' : 'actividades'}
        </b>
        <span className="num">
          {vencidas.entregasQueFaltan === 0
            ? 'Todos entregaron las vencidas.'
            : `${vencidas.entregasQueFaltan} ${vencidas.entregasQueFaltan === 1 ? 'entrega falta' : 'entregas faltan'}: ${vencidas.conFaltas
                .map((a) => `${a.nombre} (${a.faltan})`)
                .join(', ')}`}
        </span>
      </section>
      <section className="periodo-bloque" aria-label="Vence esta semana">
        <span className="mono">VENCE ESTA SEMANA</span>
        {venceEstaSemana.length === 0 ? (
          <b>Nada vence esta semana</b>
        ) : (
          venceEstaSemana.map((a) => (
            <div key={a.id} className="periodo-esta-semana">
              <b>{a.nombre}</b>
              <span className="num">
                {a.entregaron} de {a.matriculados} entregaron · vence {formatearFecha(a.fechaLimite)}
              </span>
              <div className="periodo-barra" aria-hidden="true">
                <div style={{ width: `${a.matriculados ? (a.entregaron / a.matriculados) * 100 : 0}%` }} />
              </div>
            </div>
          ))
        )}
      </section>
      <section className="periodo-bloque" aria-label="Siguiente">
        <span className="mono">SIGUIENTE</span>
        {siguiente ? (
          <>
            <b>{siguiente.nombre}</b>
            <span className="num">
              {siguiente.semana !== null ? `Semana ${siguiente.semana} · ` : ''}vence {formatearFecha(siguiente.fechaLimite)}
              {siguiente.temas.length > 0 && ` · Temas: ${siguiente.temas.join(', ')}`}
            </span>
          </>
        ) : (
          <b>No quedan evaluaciones</b>
        )}
      </section>
    </div>
  )
}

function Leyenda() {
  return (
    <div className="periodo-leyenda" aria-label="Leyenda de la línea de tiempo">
      <span>
        <i className="lt2-muestra lt2-marcador--completa" />
        Vencida, todos entregaron
      </span>
      <span>
        <i className="lt2-muestra lt2-marcador--faltan" />
        Vencida, faltan entregas
      </span>
      <span>
        <i className="lt2-muestra lt2-marcador--esta-semana" />
        Vence esta semana
      </span>
      <span>
        <i className="lt2-muestra lt2-marcador--proxima" />
        Próxima
      </span>
      <span>
        <i className="lt2-muestra lt2-muestra--rombo" />
        Examen
      </span>
    </div>
  )
}

/** Panel flotante «Período»: la línea de tiempo, los tres bloques y la leyenda, con los datos reales del período. */
function PanelPeriodo({ periodo, onCerrar }) {
  const semana = periodo.semana
  return (
    <div className="periodo-panel-cuerpo">
      <div className="periodo-panel-cabeza">
        <h2 className="hoja-titulo">Período</h2>
        <span className="ayuda num">
          {semana ? `Semana ${semana.actual} de ${semana.total}` : `Semana ${NO_DISPONIBLE}`}
          {periodo.estado === 'TERMINADO' ? ' · el período terminó' : ''}
        </span>
        <Popover.Close asChild>
          <button type="button" className="periodo-cerrar" aria-label="Cerrar el período" onClick={onCerrar}>
            ×
          </button>
        </Popover.Close>
      </div>
      {semana ? (
        <LineaTiempo periodo={periodo} />
      ) : (
        <p className="ayuda">La fecha de inicio del curso no está disponible en la última sincronización: las semanas no se pueden mostrar.</p>
      )}
      <Bloques periodo={periodo} />
      {semana && <Leyenda />}
    </div>
  )
}

/** «Vence esta semana: Examen Parcial 2», o «… y 2 más» si vence más de una. */
function textoEstaSemana(lista) {
  return lista.length === 1 ? `Vence esta semana: ${lista[0].nombre}` : `Vence esta semana: ${lista[0].nombre} y ${lista.length - 1} más`
}

/**
 * C6 (TA-22.A5) · Barra de contexto del curso, de 48 px como máximo, en todos los módulos: código en letra técnica, nombre del
 * curso recortado con su nombre completo en un tooltip, «Semana X de Y», la unidad actual y, solo si aplican, los chips «Vence esta
 * semana: …» y «N vencidas». «Ver período» y los chips abren el panel flotante «Período» anclado a la barra (Esc, clic afuera o el
 * botón lo cierran; en pantallas pequeñas sale como panel inferior). Todo viene de GET /api/cursos/{id}/periodo; sin período la barra
 * dice «Sin sincronizar» (o «Período no disponible» si el curso sí se sincronizó) y no hay panel.
 */
export default function CabeceraCurso({ curso, periodo }) {
  const [abierto, setAbierto] = useState(false)
  const semana = periodo?.semana
  const estaSemana = periodo?.venceEstaSemana ?? []
  const vencidas = periodo?.vencidas?.actividades ?? 0

  return (
    <LazyMotion features={cargarAnimacion} strict>
    <MotionConfig reducedMotion="user">
      <Popover.Root open={abierto} onOpenChange={setAbierto}>
        <Popover.Anchor asChild>
          <m.header
            className="contexto-curso"
            aria-label={`Curso ${curso.nombreCorto ?? ''}`}
            initial={{ opacity: 0, y: -6 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.25, ease: 'easeOut' }}
          >
            <span className="contexto-codigo mono">{curso.nombreCorto}</span>
            <Tooltip.Provider delayDuration={300}>
              <Tooltip.Root>
                <Tooltip.Trigger asChild>
                  <span className="contexto-nombre" tabIndex={0}>
                    {curso.nombre}
                  </span>
                </Tooltip.Trigger>
                <Tooltip.Portal>
                  <Tooltip.Content className="lt2-tooltip" sideOffset={6}>
                    {curso.nombre}
                  </Tooltip.Content>
                </Tooltip.Portal>
              </Tooltip.Root>
            </Tooltip.Provider>

            {!periodo ? (
              <span className="contexto-estado">{curso.ultimaSincronizacion ? 'Período no disponible' : 'Sin sincronizar'}</span>
            ) : (
              <>
                <span className="contexto-semana num" aria-label={semana ? `Semana ${semana.actual} de ${semana.total}` : undefined}>
                  {semana ? `Semana ${semana.actual} de ${semana.total}` : `Semana ${NO_DISPONIBLE}`}
                </span>
                {periodo.estado === 'TERMINADO' ? (
                  <span className="contexto-unidad">Período terminado</span>
                ) : (
                  periodo.unidadActual && (
                    <span className="contexto-unidad" title={`U${periodo.unidadActual.numero} · ${periodo.unidadActual.nombre}`}>
                      U{periodo.unidadActual.numero} · {periodo.unidadActual.nombre}
                    </span>
                  )
                )}
                <span className="contexto-derecha">
                  {estaSemana.length > 0 && (
                    <button type="button" className="contexto-chip contexto-chip--vivo" onClick={() => setAbierto(true)}>
                      {textoEstaSemana(estaSemana)}
                    </button>
                  )}
                  {vencidas > 0 && (
                    <button type="button" className="contexto-chip num" onClick={() => setAbierto(true)}>
                      {vencidas} {vencidas === 1 ? 'vencida' : 'vencidas'}
                    </button>
                  )}
                  <Popover.Trigger asChild>
                    <button type="button" className="contexto-ver">
                      Ver período
                    </button>
                  </Popover.Trigger>
                </span>
              </>
            )}
          </m.header>
        </Popover.Anchor>

        <AnimatePresence>
          {abierto && periodo && (
            <Popover.Portal forceMount>
              <Popover.Content forceMount asChild align="end" sideOffset={8} collisionPadding={12} aria-label="Período">
                <m.div
                  className="periodo-panel"
                  initial={{ opacity: 0, y: -8 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -8 }}
                  transition={{ duration: 0.18, ease: 'easeOut' }}
                >
                  <PanelPeriodo periodo={periodo} onCerrar={() => setAbierto(false)} />
                </m.div>
              </Popover.Content>
            </Popover.Portal>
          )}
        </AnimatePresence>
      </Popover.Root>
    </MotionConfig>
    </LazyMotion>
  )
}
