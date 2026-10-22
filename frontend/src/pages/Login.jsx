
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { login, guardarSesion } from '../services/auth'

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
      navigate('/cursos')
    } catch (err) {
      setError(
        err.response?.data?.mensaje ||
        err.response?.data?.message ||
        err.message ||
        'No se pudo iniciar sesión.',
      )
    } finally {
      setCargando(false)
    }
  }

  return (
    <main className="login-page">
      <section className="login-card">
        <div className="brand-mark">E</div>

        <p className="eyebrow">PLATAFORMA ACADÉMICA</p>
        <h1>EduMonitor AI</h1>
        <p className="login-description">
          Inicia sesión para consultar tus cursos y su información.
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

          <button type="submit" disabled={cargando}>
            {cargando ? 'Iniciando sesión...' : 'Iniciar sesión'}
          </button>
        </form>

        <p className="login-footer">
          EduMonitor AI · Monitoreo académico
        </p>
      </section>
    </main>
  )
}
