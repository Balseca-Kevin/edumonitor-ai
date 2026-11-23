// Valores DECORATIVOS del fondo (32, como un curso del laboratorio). No son notas de nadie ni vienen de la API: solo dibujan
// los puntos que suben, la curva y la raya de 7,0 de la pantalla de inicio (mockup MK-19) y la pantalla de carga del curso.
const NOTAS_DECORATIVAS = [
  7.4, 5.92, 8.62, 6.4, 9.1, 7.05, 4.2, 7.85, 6.95, 8.3, 7.6, 2.4, 8.9, 6.1, 7.2, 9.7, 5.4, 7.7, 6.6, 8.1, 3.5, 7.3, 8.5, 6.8, 7.9,
  5.1, 9.4, 7.5, 4.8, 8.4, 6.3, 5.7,
]
const ALTO_FONDO = 280
const PIXELES_POR_PUNTO = 26
const APROBACION = 7

const alto = (nota) => nota * PIXELES_POR_PUNTO

/**
 * Fondo decorativo del diseño D: los puntos que suben y flotan, la curva que se dibuja y la raya de 7,0. Lo usan el inicio de sesión
 * y la pantalla de carga del curso (TA-21.A2). Quieto con prefers-reduced-motion.
 */
export default function FondoPuntos() {
  const orden = [...NOTAS_DECORATIVAS].sort((a, b) => a - b)
  const x = (i) => (i / (orden.length - 1)) * 100
  const curva = orden.map((n, i) => `${i === 0 ? 'M' : 'L'}${(x(i) * 10).toFixed(1)} ${(ALTO_FONDO - alto(n)).toFixed(1)}`).join(' ')

  return (
    <div className="login-fondo" aria-hidden="true">
      <div className="login-raya7" style={{ bottom: alto(APROBACION) }} />
      <span className="login-raya7-texto" style={{ bottom: alto(APROBACION) + 6 }}>
        7,0 aprueba
      </span>
      <svg viewBox={`0 0 1000 ${ALTO_FONDO}`} preserveAspectRatio="none">
        <path
          className="login-curva"
          d={curva}
          pathLength="1"
          fill="none"
          stroke="rgba(174, 182, 198, 0.38)"
          strokeWidth="1.5"
        />
      </svg>
      {orden.map((nota, i) => {
        const bajo = nota < APROBACION
        return (
          <div
            key={i}
            className="punto-pos"
            style={{ left: `${x(i).toFixed(2)}%`, bottom: alto(nota), '--d': `${alto(nota)}px`, animationDelay: `${i * 28}ms` }}
          >
            <div
              className="punto"
              style={{
                background: bajo ? 'var(--oscuro-arena)' : 'var(--oscuro-punto)',
                '--t': `${(3 + (i % 5) * 0.6).toFixed(1)}s`,
                '--r': `${(1.4 + (i % 7) * 0.25).toFixed(2)}s`,
              }}
            />
          </div>
        )
      })}
    </div>
  )
}
