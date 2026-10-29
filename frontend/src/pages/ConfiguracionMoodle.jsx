import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { cerrarSesion } from '../services/auth'
import {
  guardarConfiguracion,
  obtenerConfiguracion,
  probarConexion,
} from '../services/moodleConfig'

function formatearFecha(fecha) {
  if (!fecha) return 'No disponible'
  const valor = new Date(fecha)
  return Number.isNaN(valor.getTime())
    ? fecha
    : valor.toLocaleString('es-EC', { dateStyle: 'short', timeStyle: 'short' })
}

function mensajeDeError(err) {
  if (err.response?.status === 403) return 'Solo un administrador puede configurar Moodle.'
  return (
    err.response?.data?.detail ||
    err.response?.data?.title ||
    (err.response
      ? 'No se pudo completar la operación.'
      : 'No se pudo conectar con el servidor. Intenta de nuevo.')
  )
}

/** HU-02 · Configuración y prueba de la conexión con Moodle (MK-02). Solo ADMIN. */
export default function ConfiguracionMoodle() {
  const navigate = useNavigate()
  const [configuracion, setConfiguracion] = useState(null)
  const [url, setUrl] = useState('')
  const [token, setToken] = useState('')
  const [error, setError] = useState('')
  const [aviso, setAviso] = useState('')
  const [guardando, setGuardando] = useState(false)
  const [probando, setProbando] = useState(false)
  const [prueba, setPrueba] = useState(null)

  useEffect(() => {
    let activo = true

    obtenerConfiguracion()
      .then((datos) => {
        if (activo) {
          setConfiguracion(datos)
          setUrl(datos.url || '')
        }
      })
      .catch((err) => {
        if (activo) setError(mensajeDeError(err))
      })

    return () => {
      activo = false
    }
  }, [])

  async function manejarGuardar(event) {
    event.preventDefault()
    setError('')
    setAviso('')
    setGuardando(true)

    try {
      await guardarConfiguracion(url.trim(), token.trim())
      setToken('')
      setConfiguracion(await obtenerConfiguracion())
      setAviso('Configuración guardada.')
    } catch (err) {
      setError(mensajeDeError(err))
    } finally {
      setGuardando(false)
    }
  }

  async function manejarProbar() {
    setError('')
    setProbando(true)

    try {
      setPrueba(await probarConexion())
    } catch (err) {
      setError(mensajeDeError(err))
    } finally {
      setProbando(false)
    }
  }

  function manejarCierreSesion() {
    cerrarSesion()
    navigate('/login', { replace: true })
  }

  const noDisponibles = prueba?.funciones?.filter((f) => !f.disponible) ?? []

  return (
    <main className="config-page">
      <header className="courses-header">
        <div>
          <h1>Configuración de la conexión con Moodle</h1>
          <p>Última actualización: {formatearFecha(configuracion?.actualizado)}</p>
        </div>
        <button className="logout-button" type="button" onClick={manejarCierreSesion}>
          Cerrar sesión
        </button>
      </header>

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}

      <div className="config-grid">
        <section className="config-card" aria-labelledby="datos-conexion">
          <h2 id="datos-conexion">Datos de conexión</h2>

          <form onSubmit={manejarGuardar}>
            <label htmlFor="url-moodle">URL del sitio Moodle</label>
            <input
              id="url-moodle"
              type="url"
              value={url}
              onChange={(event) => setUrl(event.target.value)}
              placeholder="http://localhost:8080"
              required
            />

            <label htmlFor="token-moodle">Token del servicio web</label>
            <input
              id="token-moodle"
              type="password"
              autoComplete="off"
              value={token}
              onChange={(event) => setToken(event.target.value)}
              placeholder={configuracion?.tokenEnmascarado || 'Sin token configurado'}
            />
            <p className="config-hint">
              El token se guarda solo en el servidor y nunca se muestra completo. Déjalo vacío para conservar el actual.
            </p>

            <label htmlFor="servicio-moodle">Servicio externo</label>
            <input
              id="servicio-moodle"
              type="text"
              value={configuracion ? `${configuracion.servicio} (solo lectura)` : ''}
              readOnly
            />

            <div className="config-actions">
              <button type="button" onClick={manejarProbar} disabled={probando}>
                {probando ? 'Probando...' : 'Probar conexión'}
              </button>
              <button type="submit" className="secondary-button" disabled={guardando}>
                {guardando ? 'Guardando...' : 'Guardar'}
              </button>
            </div>

            {aviso && (
              <p className="config-success" role="status">
                {aviso}
              </p>
            )}
          </form>
        </section>

        <section className="config-card" aria-labelledby="resultado-prueba">
          <h2 id="resultado-prueba">Resultado de la prueba</h2>

          {!prueba && <p>Pulsa «Probar conexión» para verificar Moodle.</p>}

          {prueba && !prueba.conectado && (
            <p className="sync-warning" role="alert">
              No se pudo conectar con Moodle: {prueba.error}
            </p>
          )}

          {prueba?.conectado && (
            <>
              <p className="config-success">
                Conexión correcta · {prueba.sitio} · Moodle {prueba.version} · Usuario técnico: {prueba.usuarioTecnico}
              </p>

              <table className="config-table">
                <thead>
                  <tr>
                    <th>Función del servicio web</th>
                    <th>Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {prueba.funciones.map((funcion) => (
                    <tr key={funcion.nombre}>
                      <td>
                        <code>{funcion.nombre}</code>
                      </td>
                      <td>
                        <span className={funcion.disponible ? 'estado-disponible' : 'estado-no-disponible'}>
                          {funcion.disponible ? 'Disponible' : 'No disponible'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {noDisponibles.length > 0 && (
                <p className="sync-warning">
                  Hay funciones no disponibles. No bloquean la sincronización: los datos que dependen de ellas se mostrarán como dato no disponible.
                </p>
              )}
            </>
          )}
        </section>
      </div>
    </main>
  )
}
