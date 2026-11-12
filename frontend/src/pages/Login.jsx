import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { guardarSesion, login } from '../services/auth'

// Valores DECORATIVOS del fondo (32, como un curso del laboratorio). No son notas de nadie ni vienen de la API: solo dibujan
// los puntos que suben, la curva y la raya de 7,0 de la pantalla de inicio (mockup MK-19).
const NOTAS_DECORATIVAS = [
  7.4, 5.92, 8.62, 6.4, 9.1, 7.05, 4.2, 7.85, 6.95, 8.3, 7.6, 2.4, 8.9, 6.1, 7.2, 9.7, 5.4, 7.7, 6.6, 8.1, 3.5, 7.3, 8.5, 6.8, 7.9,
  5.1, 9.4, 7.5, 4.8, 8.4, 6.3, 5.7,
]
const ALTO_FONDO = 280
const PIXELES_POR_PUNTO = 26
const APROBACION = 7

const PREGUNTAS = ['¿Quién bajó en la Unidad 3?', '¿Quién no entregó la Tarea 5?', '¿Cómo va el curso antes del Parcial 2?']

const alto = (nota) => nota * PIXELES_POR_PUNTO

function Fondo() {
  const orden = [...NOTAS_DECORATIVAS].sort((a, b) => a - b)
  const x = (i) => (i / (orden.length - 1)) * 100
  const curva = orden.map((n, i) => `${i === 0 ? 'M' : 'L'}${(x(i) * 10).toFixed(1)} ${(ALTO_FONDO - alto(n)).toFixed(1)}`).join(' ')

  return (
    <div className="login-fondo" aria-hidden="true">
      <div className="login-raya7" style={{ bottom: alto(APROBACION) }} />
      <span className="login-raya7-texto" style={{ bottom: alto(APROBACION) + 6 }}>
        7,0
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

/**
 * HU-19 · TA-19.3 · Inicio de sesión del diseño D (mockup MK-19): pantalla oscura con los puntos que suben, la curva que se
 * dibuja y la raya de 7,0; el lema, tres preguntas que rotan cada 3 s y el formulario real contra POST /api/auth/login.
 * El error no dice si falló el usuario o la contraseña.
 */
export default function Login() {
  const [usuario, setUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [error, setError] = useState('')
  const [cargando, setCargando] = useState(false)
  const navigate = useNavigate()

  async function ingresar(event) {
    event.preventDefault()
    if (cargando) return

    setError('')
    setCargando(true)
    try {
      const sesion = await login(usuario.trim(), contrasena)
      guardarSesion(sesion)
      navigate(sesion.usuario?.rol === 'ADMIN' ? '/admin/moodle' : '/cursos', { replace: true })
    } catch (err) {
      setError(
        err.response?.data?.title ||
          (err.response ? 'No se pudo iniciar sesión.' : 'No se pudo conectar con el servidor. Intenta de nuevo.'),
      )
    } finally {
      setCargando(false)
    }
  }

  return (
    <main className="login">
      <Fondo />

      <section className="login-texto" aria-label="Presentación de EduMonitor">
        <div className="login-sello entra" style={{ animationDelay: '100ms' }}>
          <span className="vivo" aria-hidden="true" />
          Aula virtual · UTA · FISEI · Software
        </div>
        <h1 className="entra" style={{ animationDelay: '250ms' }}>
          Edu<b>Monitor</b>
        </h1>
        <div className="login-lema entra" style={{ animationDelay: '400ms' }}>
          <p className="grande">
            Seguimiento académico y <b>apoyo a tiempo</b>.
          </p>
          <p className="suave">Encuentra a quién apoyar en tu curso y por qué, con las notas reales del aula virtual.</p>
        </div>
        <div className="login-preguntas entra" style={{ animationDelay: '700ms' }}>
          <span className="mono">Pregúntale a tu curso</span>
          <div className="login-rotador" aria-hidden="true">
            {PREGUNTAS.map((pregunta, i) => (
              <span key={pregunta} className="login-rota" style={{ animationDelay: `${i * 3}s` }}>
                {pregunta}
              </span>
            ))}
          </div>
        </div>
        <div className="login-segmentos entra" style={{ animationDelay: '2000ms' }} aria-hidden="true">
          <div>
            <div className="seg" style={{ animationDelay: '2200ms' }} />
            <div className="seg" style={{ animationDelay: '2350ms' }} />
            <div className="seg-espacio" />
            <div className="seg" style={{ animationDelay: '2500ms' }} />
            <div className="seg seg--futuro" />
          </div>
          <span className="mono">P1 · U1 U2 · P2 · U3 U4</span>
        </div>
      </section>

      <section className="login-acceso">
        <form className="login-form entra" style={{ animationDelay: '550ms' }} onSubmit={ingresar} noValidate>
          <h2>Iniciar sesión</h2>
          <label className="etiqueta" htmlFor="usuario">
            Usuario
            <input
              id="usuario"
              className="campo"
              autoComplete="username"
              value={usuario}
              onChange={(e) => setUsuario(e.target.value)}
              required
            />
          </label>
          <label className="etiqueta" htmlFor="contrasena">
            Contraseña
            <input
              id="contrasena"
              className="campo"
              type="password"
              autoComplete="current-password"
              value={contrasena}
              onChange={(e) => setContrasena(e.target.value)}
              required
            />
          </label>
          {error && (
            <p className="login-error" role="alert">
              {error}
            </p>
          )}
          <button type="submit" className="login-boton" disabled={cargando}>
            {cargando ? 'Ingresando…' : 'Ingresar'} <span className="flecha" aria-hidden="true">→</span>
          </button>
          <p className="login-nota">Solo lectura del aula virtual. Modelo de IA local.</p>
        </form>
      </section>
    </main>
  )
}
