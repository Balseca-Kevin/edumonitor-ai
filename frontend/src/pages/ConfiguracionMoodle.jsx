import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Marco from '../components/Marco'
import Button from '../components/ui/Button'
import Input from '../components/ui/Input'
import Card from '../components/ui/Card'
import Table from '../components/ui/Table'
import StatusBadge from '../components/ui/StatusBadge'
import Alert from '../components/ui/Alert'
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
  if (err?.response?.status === 403) {
    return 'Solo un administrador puede configurar Moodle.'
  }
  return (
    err?.response?.data?.detail ||
    err?.response?.data?.title ||
    (err?.response
      ? 'No se pudo completar la operación.'
      : 'No se pudo conectar con el servidor. Intenta de nuevo.')
  )
}

const columnasFunciones = [
  { key: 'nombre', label: 'Función del servicio web', render: (nombre) => <code>{nombre}</code> },
  {
    key: 'disponible',
    label: 'Disponibilidad',
    render: (disponible) => (
      <StatusBadge
        status={disponible ? 'OK' : 'NO_DISPONIBLE'}
        label={disponible ? 'Disponible' : 'No disponible'}
      />
    ),
  },
]

/** HU-02 + HU-19 / TA-19.4: interfaz de configuración Moodle para ADMIN. */
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
        if (!activo) return
        setConfiguracion(datos)
        setUrl(datos?.url || '')
      })
      .catch((err) => {
        if (activo) setError(mensajeDeError(err))
      })
    return () => { activo = false }
  }, [])

  async function manejarGuardar(event) {
    event.preventDefault()
    if (guardando || probando) return
    setError('')
    setAviso('')
    setGuardando(true)
    try {
      await guardarConfiguracion(url.trim(), token.trim())
      setToken('')
      const actualizada = await obtenerConfiguracion()
      setConfiguracion(actualizada)
      setUrl(actualizada?.url || url.trim())
      setPrueba(null) // El resultado anterior ya no valida una configuración modificada.
      setAviso('Configuración guardada.')
    } catch (err) {
      setError(mensajeDeError(err))
    } finally {
      setGuardando(false)
    }
  }

  async function manejarProbar() {
    if (probando || guardando) return
    setError('')
    setAviso('')
    setPrueba(null)
    setProbando(true)
    try {
      setPrueba(await probarConexion())
    } catch (err) {
      setError(mensajeDeError(err))
    } finally {
      setProbando(false)
    }
  }

  const funciones = Array.isArray(prueba?.funciones) ? prueba.funciones : []
  const disponibles = funciones.filter((funcion) => funcion.disponible).length
  const noDisponibles = funciones.filter((funcion) => !funcion.disponible)

  return (
    <Marco>
      <div className="hu19-config">
        <header className="hu19-config__cabecera">
          <div>
            <p className="hu19-config__sobretitulo">Administración · Aula virtual</p>
            <h1>Configuración de la conexión con Moodle</h1>
            <p className="hu19-config__meta">
              Última actualización: {formatearFecha(configuracion?.actualizado)}
            </p>
          </div>
          <Button variant="secondary" onClick={() => navigate('/admin/estado')}>
            Estado del sistema
          </Button>
        </header>

        {error && <Alert variant="error">{error}</Alert>}
        {aviso && <Alert variant="success">{aviso}</Alert>}

        <div className="hu19-config__columnas">
          <Card title="Datos de conexión" className="hu19-config__tarjeta">
            <form className="hu19-config__formulario" onSubmit={manejarGuardar}>
              <Input
                id="url-moodle"
                label="URL del sitio Moodle"
                type="url"
                value={url}
                onChange={(event) => setUrl(event.target.value)}
                placeholder="http://localhost:8080"
                disabled={guardando || probando}
                required
              />
              <Input
                id="token-moodle"
                label="Token del servicio web"
                type="password"
                autoComplete="off"
                value={token}
                onChange={(event) => setToken(event.target.value)}
                placeholder={configuracion?.tokenEnmascarado || 'Sin token configurado'}
                disabled={guardando || probando}
                helperText="Deja el campo vacío para conservar el token actual. No se muestra el valor completo."
              />
              <p className="hu19-config__token">
                Clave actual: <code>{configuracion?.tokenEnmascarado || 'Sin token configurado'}</code>
              </p>
              <Input
                id="servicio-moodle"
                label="Servicio externo"
                type="text"
                value={configuracion ? `${configuracion.servicio} (solo lectura)` : ''}
                readOnly
              />
              <div className="hu19-config__acciones">
                <Button
                  type="button"
                  onClick={manejarProbar}
                  loading={probando}
                  disabled={guardando}
                >
                  {probando ? 'Probando...' : 'Probar conexión'}
                </Button>
                <Button
                  type="submit"
                  variant="secondary"
                  loading={guardando}
                  disabled={probando}
                >
                  {guardando ? 'Guardando...' : 'Guardar'}
                </Button>
              </div>
            </form>
          </Card>

          <Card title="Resultado de la prueba" className="hu19-config__tarjeta">
            {!prueba && (
              <p className="hu19-config__ayuda">
                Pulsa «Probar conexión» para verificar Moodle.
              </p>
            )}
            {prueba && !prueba.conectado && (
              <Alert variant="error">
                No se pudo conectar con Moodle: {prueba.error || 'Error desconocido'}
              </Alert>
            )}
            {prueba?.conectado && (
              <div className="hu19-config__resultado">
                <Alert variant="success">
                  Conexión correcta · {prueba.sitio} · Moodle {prueba.version} · Usuario técnico: {prueba.usuarioTecnico}
                </Alert>
                <div className="hu19-config__subtitulo">
                  <h3>Funciones del servicio web</h3>
                  <span>{disponibles}/{funciones.length}</span>
                </div>
                <Table
                  caption="Disponibilidad de funciones del servicio web"
                  columns={columnasFunciones}
                  rows={funciones}
                  rowKey="nombre"
                  emptyMessage="El servicio no informó funciones."
                />
                {noDisponibles.length > 0 && (
                  <Alert variant="warning">
                    Hay funciones no disponibles. No bloquean la sincronización: los datos que dependen de ellas se mostrarán como dato no disponible.
                  </Alert>
                )}
              </div>
            )}
          </Card>
        </div>
      </div>
    </Marco>
  )
}
