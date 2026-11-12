
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { login, guardarSesion } from '../services/auth'
import Button from '../components/ui/Button'
import Input from '../components/ui/Input'
import Alert from '../components/ui/Alert'
import './Login.css'

/**
 * HU-19 / TA-19.3
 * Inicio de sesión con el sistema de diseño suizo.
 */
export default function Login() {
  const [usuario, setUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [error, setError] = useState('')
  const [cargando, setCargando] = useState(false)

  const navigate = useNavigate()

  async function handleSubmit(event) {
    event.preventDefault()

    if (cargando) return

    setError('')
    setCargando(true)

    try {
      const sesion = await login(usuario.trim(), contrasena)

      guardarSesion(sesion)

      navigate(
        sesion.usuario?.rol === 'ADMIN'
          ? '/admin/moodle'
          : '/cursos',
        { replace: true },
      )
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
    <main className="hu19-login">
      <section
        className="hu19-login__identidad"
        aria-label="Presentación de EduMonitor"
      >
        <div className="hu19-login__marca">
          <p className="hu19-login__nombre">
            EduMonitor
          </p>

          <p className="hu19-login__descripcion">
            Seguimiento académico con datos reales
            de tu aula virtual.
          </p>
        </div>

        <p className="hu19-login__institucion">
          UTA · FISEI · Software
        </p>
      </section>

      <section className="hu19-login__acceso">
        <div className="hu19-login__formulario">
          <div className="hu19-login__encabezado">
            <h1>Iniciar sesión</h1>

            <p>
              Ingresa tus credenciales para consultar
              tus cursos.
            </p>
          </div>

          <form
            onSubmit={handleSubmit}
            className="hu19-login__campos"
          >
            <Input
              id="usuario"
              name="usuario"
              label="Usuario"
              type="text"
              autoComplete="username"
              placeholder="Ingresa tu usuario"
              value={usuario}
              onChange={(event) => setUsuario(event.target.value)}
              disabled={cargando}
              required
            />

            <Input
              id="contrasena"
              name="contrasena"
              label="Contraseña"
              type="password"
              autoComplete="current-password"
              placeholder="Ingresa tu contraseña"
              value={contrasena}
              onChange={(event) => setContrasena(event.target.value)}
              disabled={cargando}
              required
            />

            {error && (
              <Alert variant="error">
                {error}
              </Alert>
            )}

            <Button
              type="submit"
              loading={cargando}
              disabled={!usuario.trim() || !contrasena}
              className="hu19-login__boton"
            >
              Iniciar sesión
            </Button>
          </form>
        </div>
      </section>
    </main>
  )
}
