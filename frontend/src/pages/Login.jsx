import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { guardarSesion, login } from '../services/auth'
import FondoPuntos from '../components/FondoPuntos'

const PREGUNTAS = ['¿Quién bajó en la Unidad 3?', '¿Quién no entregó la Tarea 5?', '¿Cómo va el curso antes del Parcial 2?']


/**
 * HU-19 y R.7 · Inicio de sesión del diseño D (mockup MK-19) con lo que suma el prototipo del Sprint 3: pantalla oscura con los
 * puntos que suben, la curva que se dibuja y la raya de 7,0; el sello con el punto «en línea» que late, el lema, el subtexto que
 * menciona el asistente y el período, tres preguntas que rotan cada 3 s y el formulario real contra POST /api/auth/login con las
 * etiquetas «Notas tal como en Moodle», «Alertas con evidencia» y «Asistente con respaldo local». El error no dice si falló el
 * usuario o la contraseña.
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
      <FondoPuntos />

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
          <p className="suave">
            Encuentra a quién apoyar en tu curso y por qué, con las notas reales del aula virtual y un asistente que sabe en qué punto
            del período estás.
          </p>
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
          <div>
            <h2>Ingresar</h2>
            <p className="login-ayuda">Con tu usuario docente de EduMonitor.</p>
          </div>
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
          <ul className="login-etiquetas" aria-label="Qué ofrece EduMonitor">
            <li>Notas tal como en Moodle</li>
            <li>Alertas con evidencia</li>
            <li>Asistente con respaldo local</li>
          </ul>
        </form>
      </section>
    </main>
  )
}
