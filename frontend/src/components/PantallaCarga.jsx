import FondoPuntos from './FondoPuntos'

const MARCA = { girando: '', listo: '✓', error: '!' }
const ESTADO = { girando: 'en curso', listo: 'terminado', error: 'con error' }

/**
 * TA-21.A2 · Pantalla de carga del curso (docs/sprint-3/diseno.md): fondo oscuro con los puntos y la curva del inicio, entrada con
 * escala, círculo que gira, barra de avance de arena a verde y un paso por consulta real, que pasa de girando a terminado (o con su
 * error) cuando llega su respuesta. Quieta con prefers-reduced-motion.
 */
export default function PantallaCarga({ titulo, codigo, subtitulo, pasos }) {
  const terminados = pasos.filter((p) => p.estado !== 'girando').length
  const avance = pasos.length ? (terminados / pasos.length) * 100 : 0
  return (
    <div className="carga" role="dialog" aria-modal="true" aria-label={titulo} aria-busy={terminados < pasos.length}>
      <FondoPuntos />
      <div className="carga-caja">
        {codigo && <div className="mono carga-codigo">{codigo}</div>}
        <div className="carga-titulo">
          <span className="carga-giro" aria-hidden="true" />
          <h2>{titulo}</h2>
        </div>
        {subtitulo && <p className="carga-sub">{subtitulo}</p>}
        <div
          className="carga-barra"
          role="progressbar"
          aria-label="Avance"
          aria-valuemin={0}
          aria-valuemax={pasos.length}
          aria-valuenow={terminados}
        >
          <div style={{ width: `${avance}%` }} />
        </div>
        <ul className="carga-pasos">
          {pasos.map((p) => (
            <li key={p.texto} className={`carga-paso carga-paso--${p.estado}`}>
              <span className="carga-marca" aria-hidden="true">
                {MARCA[p.estado]}
              </span>
              <span>
                {p.texto}
                <span className="sr-only"> · {ESTADO[p.estado]}</span>
                {p.detalle && <small className="carga-detalle">{p.detalle}</small>}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}
