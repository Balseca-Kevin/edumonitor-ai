import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { formatearFecha, formatearNumero } from './formato'
import { busquedaTramo } from './tramo'

const NO_DISPONIBLE = 'no disponible'
const APROBACION = 7

function Desglose({ categorias }) {
  if (!categorias?.length) return null
  return (
    <section className="ficha-bloque" aria-label="Por ponderación">
      <h3>Por ponderación</h3>
      {categorias.map((c) => (
        <div className="ficha-categoria" key={c.categoria}>
          <div className="num">
            <span>
              {c.categoria} · {formatearNumero(c.peso * 100, 0)} %
            </span>
            <b>{c.promedio === null ? 'sin notas' : formatearNumero(c.promedio)}</b>
          </div>
          <div className="pista-fina">
            {c.promedio !== null && (
              <div className={`pista-fina-valor${c.promedio < APROBACION ? ' pista-fina-valor--bajo' : ''}`} style={{ width: `${c.promedio * 10}%` }} />
            )}
          </div>
        </div>
      ))}
    </section>
  )
}

/** Una barra por actividad del tramo, en el orden del libro: la nota real, sin entrega en rojo, por calificar o aún no vence en gris. */
function Evolucion({ celdas }) {
  if (!celdas.length) return null
  return (
    <section className="ficha-bloque" aria-label="Evolución">
      <h3>Evolución</h3>
      <div className="evolucion">
        <div className="evolucion-raya" aria-hidden="true" />
        {celdas.map((c) => {
          const clase =
            c.estado === 'CALIFICADO'
              ? c.nota < APROBACION
                ? 'evolucion-barra evolucion-barra--bajo'
                : 'evolucion-barra'
              : c.estado === 'SIN_ENTREGA'
                ? 'evolucion-barra evolucion-barra--sin-entrega'
                : 'evolucion-barra evolucion-barra--espera'
          const alto = c.estado === 'CALIFICADO' ? Math.max(c.nota * 10, 2) : c.estado === 'SIN_ENTREGA' ? 8 : 4
          const texto =
            c.estado === 'CALIFICADO'
              ? formatearNumero(c.nota)
              : c.estado === 'SIN_ENTREGA'
                ? 'sin entrega'
                : c.estado === 'PENDIENTE'
                  ? 'por calificar'
                  : c.estado === 'AUN_NO_VENCE'
                    ? 'aún no vence'
                    : NO_DISPONIBLE
          return (
            <div
              key={c.actividadId}
              className={clase}
              style={{ height: `${alto}%` }}
              title={`${c.nombre}: ${texto}${c.fechaLimite ? ` · vence ${formatearFecha(c.fechaLimite)}` : ''}`}
              aria-label={`${c.nombre}: ${texto}`}
              role="img"
            />
          )
        })}
      </div>
      <p className="ayuda">Línea punteada: 7,0. Rojo: sin entrega. Gris: por calificar o aún no vence.</p>
    </section>
  )
}

/**
 * R.4 · Ficha lateral del estudiante (docs/sprint-3/diseno.md): entra desde la derecha en 0,28 s sobre un velo. Muestra el promedio
 * ponderado del tramo, el desglose 40/30/30, la evolución nota a nota y lo que no entregó, con «Armar plan de refuerzo» y «Preguntar
 * al asistente por este estudiante». Todas las cifras vienen de la API (indicadores y libro del tramo).
 */
export default function FichaLateral({ cursoId, estudiante, requiereAtencion, celdas = [], tramo, nombreTramo, onCerrar }) {
  const navigate = useNavigate()

  useEffect(() => {
    function alPresionar(evento) {
      if (evento.key === 'Escape') onCerrar()
    }
    window.addEventListener('keydown', alPresionar)
    return () => window.removeEventListener('keydown', alPresionar)
  }, [onCerrar])

  if (!estudiante) return null
  const nombre = `${estudiante.apellido}, ${estudiante.nombre}`
  const sinEntregar = celdas.filter((c) => c.estado === 'SIN_ENTREGA')
  const consulta = `?estudiante=${estudiante.id}${busquedaTramo(tramo).replace('?', '&')}`

  return (
    <>
      <div className="velo" onClick={onCerrar} aria-hidden="true" />
      <aside className="ficha-lateral" role="dialog" aria-modal="true" aria-label={`Ficha de ${nombre}`}>
        <header className="ficha-cabecera">
          <div>
            <span className="mono">FICHA DEL ESTUDIANTE</span>
            <h2>{nombre}</h2>
            {requiereAtencion !== null && requiereAtencion !== undefined && (
              <span className={requiereAtencion ? 'chip-bajo' : 'chip-ok'}>{requiereAtencion ? 'Requiere atención' : 'Al día'}</span>
            )}
          </div>
          <button type="button" className="ficha-cerrar" aria-label="Cerrar ficha" onClick={onCerrar}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <path d="M6 6l12 12M18 6L6 18" />
            </svg>
          </button>
        </header>
        <div className="ficha-cuerpo">
          <div className="ficha-promedio">
            <span className={`cifra${estudiante.promedioPonderado === null ? ' cifra--no-disponible' : ''}`}>
              {formatearNumero(estudiante.promedioPonderado) ?? NO_DISPONIBLE}
            </span>
            <span className="ayuda">promedio ponderado en {nombreTramo}</span>
          </div>
          <Desglose categorias={estudiante.categorias} />
          <Evolucion celdas={celdas} />
          {sinEntregar.length > 0 && (
            <section className="ficha-bloque" aria-label="Sin entregar">
              <h3>Sin entregar</h3>
              <div className="chips">
                {sinEntregar.map((c) => (
                  <span key={c.actividadId} className="chip-sin-entrega">
                    {c.nombre}
                  </span>
                ))}
              </div>
            </section>
          )}
          <div className="ficha-acciones">
            <button type="button" className="boton boton--ia" onClick={() => navigate(`/cursos/${cursoId}/refuerzos${consulta}`)}>
              Armar plan de refuerzo
            </button>
            <button type="button" className="boton boton--secundario" onClick={() => navigate(`/cursos/${cursoId}/asistente${consulta}`)}>
              Preguntar al asistente por este estudiante
            </button>
          </div>
        </div>
      </aside>
    </>
  )
}
