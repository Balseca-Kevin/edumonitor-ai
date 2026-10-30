import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { login, guardarSesion } from '../services/auth'

// Ponderación UTA que dibuja la animación del panel institucional.
const PONDERACION = [
  { nombre: 'Exámenes', peso: 40 },
  { nombre: 'Tareas', peso: 30 },
  { nombre: 'APE', peso: 30 },
]

function LibroCalificaciones() {
  return (
    <div className="libro" aria-hidden="true">
      <div className="libro-celdas">
        {Array.from({ length: 24 }, (_, i) => (
          <span key={i} style={{ '--i': i }} />
        ))}
      </div>

      <div className="libro-barras">
        {PONDERACION.map((categoria, i) => (
          <div className="libro-barra" key={categoria.nombre} style={{ '--i': i, '--peso': categoria.peso }}>
            <span className="libro-barra-nombre">{categoria.nombre}</span>
            <span className="libro-barra-pista">
              <span className="libro-barra-valor" />
            </span>
            <span className="libro-barra-peso mono">{categoria.peso} %</span>
          </div>
        ))}
      </div>
    </div>
  )
}

export default function Login() {
  const [usuario, setUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [error, setError] = useState('')
  const [cargando, setCargando] = useState(false)

  const navigate = useNavigate()

  async function handleSubmit(event) {
    event.preventDefault()

    setError('')
    setCargando(true)

    try {
      const sesion = await login(usuario.trim(), contrasena)

      guardarSesion(sesion)
      // El administrador entra a la configuración de Moodle; el docente, a sus cursos.
      navigate(sesion.usuario?.rol === 'ADMIN' ? '/admin/moodle' : '/cursos')
    } catch (err) {
      setError(
        err.response?.data?.title ||
          (err.response
            ? 'No se pudo iniciar sesión.'
            : 'No se pudo conectar con el servidor. Intenta de nuevo.'),
      )
    } finally {
      setCargando(false)
    }
  }

  return (
    <main className="login-page">
      <section className="login-panel">
        <p className="login-panel-sello">UTA · FISEI · Software</p>
        <p className="login-panel-marca">EduMonitor</p>
        <p className="login-panel-valor">
          Seguimiento académico con datos reales de tu aula virtual
        </p>
        <LibroCalificaciones />
      </section>

      <section className="login-form-area">
        <div className="login-card">
          <h1>Iniciar sesión</h1>

          <p className="login-description">
            Ingresa con tu usuario para consultar tus cursos.
          </p>

          <form onSubmit={handleSubmit}>
            <label htmlFor="usuario">Usuario</label>

            <input
              id="usuario"
              name="usuario"
              type="text"
              autoComplete="username"
              placeholder="Ingresa tu usuario"
              value={usuario}
              onChange={(event) => setUsuario(event.target.value)}
              required
            />

            <label htmlFor="contrasena">Contraseña</label>

            <input
              id="contrasena"
              name="contrasena"
              type="password"
              autoComplete="current-password"
              placeholder="Ingresa tu contraseña"
              value={contrasena}
              onChange={(event) => setContrasena(event.target.value)}
              required
            />

            {error && (
              <p className="form-error" role="alert">
                {error}
              </p>
            )}

            <button type="submit" className="boton-primario" disabled={cargando} aria-busy={cargando}>
              {cargando && <span className="boton-indicador" aria-hidden="true" />}
              {cargando ? 'Iniciando sesión...' : 'Iniciar sesión'}
            </button>
          </form>

          <p className="login-footer">
            EduMonitor · Monitoreo académico · Metodologías Ágiles 2026
          </p>
        </div>
      </section>
    </main>
  )
}
