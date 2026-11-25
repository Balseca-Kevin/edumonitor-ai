import { formatearFecha } from './formato'

const NO_DISPONIBLE = 'no disponible'

/** Posición horizontal del centro de la semana n en la línea de tiempo (0 a 100 %). Es geometría del dibujo, no un cálculo de fechas. */
const posicion = (semana, total) => `${(((semana - 0.5) / total) * 100).toFixed(3)}%`

/** Marcador de una actividad: color por su estado (lo calcula el backend), rombo para exámenes y círculo para lo demás. */
function claseMarcador(a) {
  const forma = /^EX/.test(a.sigla) ? ' marcador--rombo' : ''
  if (a.estado === 'VENCIDA') return `marcador${a.faltan > 0 ? ' marcador--faltan' : ' marcador--completa'}${forma}`
  if (a.estado === 'VENCE_ESTA_SEMANA') return `marcador marcador--esta-semana${forma}`
  return `marcador marcador--proxima${forma}`
}

function textoEstado(a) {
  if (a.estado === 'VENCIDA') return a.faltan > 0 ? `vencida, faltan ${a.faltan} entregas` : 'vencida, todos entregaron'
  if (a.estado === 'VENCE_ESTA_SEMANA') return `vence esta semana, ${a.entregaron} de ${a.matriculados} entregaron`
  return 'próxima'
}

/**
 * Línea de tiempo de 108 px: los parciales, el nombre de cada unidad, la pista con el relleno hasta hoy y los cortes entre unidades,
 * un marcador por actividad en su semana de vencimiento, los números de semana con «HOY n» y la sigla de cada actividad. Todas las
 * semanas, estados y unidades vienen de GET /api/cursos/{id}/periodo.
 */
function LineaTiempo({ periodo }) {
  const total = periodo.semana.total
  const actual = periodo.semana.actual
  const unidades = periodo.unidades.filter((u) => u.semanaDesde !== null && u.semanaHasta !== null)
  const primeraP2 = unidades.find((u) => u.parcial === 2)
  const corteParciales = primeraP2 ? ((primeraP2.semanaDesde - 1) / total) * 100 : null
  const conSemana = periodo.actividades.filter((a) => a.semana !== null && a.semana >= 1 && a.semana <= total)
  // Varias actividades en la misma semana se separan un poco para que no se tapen.
  const enSemana = new Map()
  const marcadores = conSemana.map((a) => {
    const orden = enSemana.get(a.semana) ?? 0
    enSemana.set(a.semana, orden + 1)
    return { ...a, desplazamiento: orden * 14 }
  })

  return (
    <div className="linea-tiempo" role="group" aria-label={`Línea de tiempo del período: semana ${actual} de ${total}`}>
      <span className="lt-parcial mono" style={{ left: 0 }}>
        PARCIAL 1
      </span>
      {corteParciales !== null && (
        <>
          <span className="lt-parcial mono" style={{ left: `calc(${corteParciales}% + 10px)` }}>
            PARCIAL 2
          </span>
          <div className="lt-corte-parcial" style={{ left: `${corteParciales}%` }} aria-hidden="true" />
        </>
      )}
      {unidades.map((u) => {
        const clase = u.numero === periodo.unidadActual?.numero ? 'lt-unidad lt-unidad--actual' : u.semanaHasta < actual ? 'lt-unidad lt-unidad--pasada' : 'lt-unidad lt-unidad--futura'
        return (
          <span
            key={u.numero}
            className={clase}
            style={{ left: `${((u.semanaDesde - 1) / total) * 100}%`, width: `${((u.semanaHasta - u.semanaDesde + 1) / total) * 100}%` }}
            title={`U${u.numero} · ${u.nombre}`}
          >
            U{u.numero} · {u.nombre}
          </span>
        )
      })}
      <div className="lt-pista" aria-hidden="true" />
      <div className="lt-relleno" style={{ width: posicion(actual, total) }} aria-hidden="true" />
      {unidades.slice(0, -1).map((u) => (
        <div key={u.numero} className="lt-corte" style={{ left: `${(u.semanaHasta / total) * 100}%` }} aria-hidden="true" />
      ))}
      {marcadores.map((a) => (
        <div
          key={a.id}
          className={claseMarcador(a)}
          style={{ left: `calc(${posicion(a.semana, total)} + ${a.desplazamiento}px)` }}
          title={`${a.nombre} · vence ${formatearFecha(a.fechaLimite)} · ${textoEstado(a)}`}
          role="img"
          aria-label={`${a.nombre}: ${textoEstado(a)}`}
        />
      ))}
      {Array.from({ length: total }, (_, i) => i + 1).map((s) => (
        <span key={s} className={`lt-semana mono num${s === actual ? ' lt-semana--hoy' : ''}`} style={{ left: posicion(s, total) }}>
          {s === actual ? `HOY ${s}` : s}
        </span>
      ))}
      {marcadores.map((a) => (
        <span
          key={`sigla-${a.id}`}
          className={`lt-sigla mono lt-sigla--${a.estado === 'VENCIDA' ? (a.faltan > 0 ? 'faltan' : 'completa') : a.estado === 'VENCE_ESTA_SEMANA' ? 'esta-semana' : 'proxima'}`}
          style={{ left: `calc(${posicion(a.semana, total)} + ${a.desplazamiento}px)` }}
          title={`${a.nombre} · ${formatearFecha(a.fechaLimite)}`}
        >
          {a.sigla}
        </span>
      ))}
    </div>
  )
}

function Bloques({ periodo }) {
  const { vencidas, venceEstaSemana, siguiente } = periodo
  return (
    <div className="lt-bloques">
      <section className="lt-bloque" aria-label="Vencidas">
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
      <section className="lt-bloque" aria-label="Vence esta semana">
        <span className="mono">VENCE ESTA SEMANA</span>
        {venceEstaSemana.length === 0 ? (
          <b>Nada vence esta semana</b>
        ) : (
          venceEstaSemana.map((a) => (
            <div key={a.id} className="lt-esta-semana">
              <b>{a.nombre}</b>
              <span className="num">
                {a.entregaron} de {a.matriculados} entregaron · vence {formatearFecha(a.fechaLimite)}
              </span>
              <div className="lt-barra" aria-hidden="true">
                <div style={{ width: `${a.matriculados ? (a.entregaron / a.matriculados) * 100 : 0}%` }} />
              </div>
            </div>
          ))
        )}
      </section>
      <section className="lt-bloque" aria-label="Siguiente">
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
    <div className="lt-leyenda" aria-label="Leyenda de la línea de tiempo">
      <span>
        <i className="marcador-muestra marcador--completa" />
        Vencida, todos entregaron
      </span>
      <span>
        <i className="marcador-muestra marcador--faltan" />
        Vencida, faltan entregas
      </span>
      <span>
        <i className="marcador-muestra marcador--esta-semana" />
        Vence esta semana
      </span>
      <span>
        <i className="marcador-muestra marcador--proxima" />
        Próxima
      </span>
      <span>
        <i className="marcador-muestra marcador-muestra--rombo" />
        Examen
      </span>
    </div>
  )
}

/**
 * TA-22.A5 · Cabecera oscura del curso (docs/sprint-3/diseno.md): código, período y estudiantes en letra técnica, nombre en 46 px;
 * a la derecha «SEMANA DEL PERÍODO» con la semana actual y el total y la unidad actual con su nombre del sílabo; la línea de
 * tiempo, los tres bloques (vencidas, vence esta semana, siguiente) y la leyenda. Todo sale de GET /api/cursos/{id}/periodo; si el
 * período no está disponible (sin sincronizar o sin fecha de inicio) lo dice, sin semanas inventadas.
 */
export default function CabeceraCurso({ curso, periodo, estudiantes }) {
  const semana = periodo?.semana
  return (
    <header className="cabecera-curso cabecera-s3">
      <div className="cabecera-fila">
        <div className="cabecera-titulo entra">
          <span className="mono">
            {curso.nombreCorto}
            {curso.periodo ? ` · Período ${curso.periodo}` : ''}
            {estudiantes != null ? ` · ${estudiantes} estudiantes` : ''}
            {curso.ultimaSincronizacion && (
              <>
                {' '}
                · <span className="vivo-punto" aria-hidden="true" /> sincronizado
              </>
            )}
          </span>
          <h1>{curso.nombre}</h1>
        </div>
        <div className="cabecera-semana entra" style={{ animationDelay: '120ms' }}>
          <span className="mono">SEMANA DEL PERÍODO</span>
          {semana ? (
            <div className="cifra-semana num" aria-label={`Semana ${semana.actual} de ${semana.total}`}>
              <span>{semana.actual}</span>
              <small> / {semana.total}</small>
            </div>
          ) : (
            <div className="cifra-semana cifra-semana--no-disponible">{NO_DISPONIBLE}</div>
          )}
          {periodo?.unidadActual && (
            <span className="cabecera-unidad">
              U{periodo.unidadActual.numero} · {periodo.unidadActual.nombre}
            </span>
          )}
          {periodo?.estado === 'TERMINADO' && <span className="cabecera-unidad">El período terminó</span>}
        </div>
      </div>

      {periodo && semana && <LineaTiempo periodo={periodo} />}
      {periodo && <Bloques periodo={periodo} />}
      {periodo && semana && <Leyenda />}
      {!periodo && (
        <p className="cabecera-sin-periodo">El estado del período aparece cuando el curso se sincroniza con el aula virtual.</p>
      )}
      {periodo && !semana && (
        <p className="cabecera-sin-periodo">
          La fecha de inicio del curso no está disponible en la última sincronización: las semanas no se pueden mostrar.
        </p>
      )}
    </header>
  )
}
