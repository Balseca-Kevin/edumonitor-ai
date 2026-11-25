import { useCallback, useEffect, useState } from 'react'
import Marco from '../../components/Marco'
import PaginaError from '../../components/PaginaError'
import Estado from '../../components/Estado'
import { formatearFecha } from '../../components/formato'
import { cambiarModelo, obtenerEstadoSistema, obtenerModelo } from '../../services/estado'
import { describirError } from '../../services/errores'

const NOMBRES = {
  api: 'API de EduMonitor',
  sqlserver: 'Base de datos (SQL Server)',
  moodle: 'Aula virtual (Moodle)',
  agente: 'Agente de IA',
  modelo: 'Modelo local (Ollama)',
  'modelo externo': 'Modelo externo',
}

const TEXTO_MODELO = { OK: 'Disponible', NO_CONFIGURADO: 'Sin configurar', EN_PAUSA: 'En pausa', NO_DISPONIBLE: 'No disponible' }
const ACTIVO = { EXTERNO: 'modelo externo', LOCAL: 'modelo local', NINGUNO: 'ninguno: el agente no responde' }

/**
 * HU-23 · Los dos modelos del asistente, cuál responde ahora y el interruptor «Usar solo el modelo local». Con el interruptor
 * activo, cada respuesta del asistente dice «Respondió: modelo local».
 */
function Modelos() {
  const [modelo, setModelo] = useState(null)
  const [error, setError] = useState(null)
  const [guardando, setGuardando] = useState(false)

  useEffect(() => {
    obtenerModelo()
      .then(setModelo)
      .catch((e) => setError(describirError(e)))
  }, [])

  async function cambiar(soloLocal) {
    setGuardando(true)
    try {
      setModelo(await cambiarModelo(soloLocal))
      setError(null)
    } catch (e) {
      setError(describirError(e))
    } finally {
      setGuardando(false)
    }
  }

  return (
    <section className="seccion" aria-labelledby="titulo-modelos">
      <div className="regla" />
      <h2 id="titulo-modelos" className="seccion-titulo">
        Modelo del asistente
      </h2>
      {error && (
        <div className="aviso aviso--error" role="alert">
          <strong>{error.titulo}</strong>
          {error.detalle}
        </div>
      )}
      {!modelo && !error && <p className="ayuda">Consultando los modelos…</p>}
      {modelo && (
        <>
          <p>
            Responde ahora: <strong>{ACTIVO[modelo.activo] ?? modelo.activo}</strong>
          </p>
          <div className="tabla-envoltura">
            <table className="tabla" aria-label="Modelos del asistente">
              <thead>
                <tr>
                  <th>Modelo</th>
                  <th>Nombre</th>
                  <th>Estado</th>
                  <th>Detalle</th>
                </tr>
              </thead>
              <tbody>
                {[modelo.externo, modelo.local].map((m) => (
                  <tr key={m.tipo}>
                    <td>{m.tipo === 'externo' ? 'Externo (principal)' : 'Local (respaldo)'}</td>
                    <td className="mono">{m.modelo ?? '—'}</td>
                    <td>
                      <Estado estado={m.estado === 'OK' ? 'OK' : 'ERROR'} texto={TEXTO_MODELO[m.estado] ?? m.estado} />
                    </td>
                    <td>{m.detalle}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <label className="modelo-interruptor">
            <input
              type="checkbox"
              role="switch"
              checked={modelo.soloLocal}
              disabled={guardando}
              onChange={(e) => cambiar(e.target.checked)}
            />
            Usar solo el modelo local
          </label>
          <p className="ayuda">
            Si el modelo externo falla, tarda más de 20 segundos o responde sin el formato esperado, responde el modelo local. Con 3
            fallos en 5 minutos, el asistente usa el modelo local durante 5 minutos.
          </p>
        </>
      )}
    </section>
  )
}

const TEXTO_ESTADO = { OK: 'Disponible', NO_DISPONIBLE: 'No disponible' }

/**
 * HU-20 · «Estado del sistema» (TA-20.5), solo ADMIN: un servicio por fila con su estado (punto de color y texto), el detalle
 * y la hora de la consulta. «Actualizar» vuelve a consultar. Un servicio caído se ve en rojo sin ocultar a los demás.
 */
export default function EstadoSistema() {
  const [estado, setEstado] = useState(null)
  const [error, setError] = useState(null)
  const [consultando, setConsultando] = useState(true)

  const consultar = useCallback(async () => {
    setConsultando(true)
    try {
      setEstado(await obtenerEstadoSistema())
      setError(null)
    } catch (e) {
      setError(describirError(e))
    } finally {
      setConsultando(false)
    }
  }, [])

  useEffect(() => {
    obtenerEstadoSistema()
      .then((datos) => setEstado(datos))
      .catch((e) => setError(describirError(e)))
      .finally(() => setConsultando(false))
  }, [])

  if (error && ['sesion', 'acceso'].includes(error.tipo)) {
    return <PaginaError error={error} />
  }

  return (
    <Marco>
      <header className="cabecera">
        <div>
          <p className="sobretitulo">Administración</p>
          <h1 className="titulo">Estado del sistema</h1>
        </div>
        <button type="button" className="boton" onClick={consultar} disabled={consultando}>
          {consultando ? 'Consultando…' : 'Actualizar'}
        </button>
      </header>

      {error && (
        <div className="aviso aviso--error" role="alert">
          <strong>{error.titulo}</strong>
          {error.detalle}
        </div>
      )}

      {!estado && !error && <p className="ayuda">Consultando los servicios…</p>}

      {estado && (
        <section className="seccion" aria-labelledby="titulo-servicios">
          <div className="regla" />
          <h2 id="titulo-servicios" className="seccion-titulo">
            Servicios
          </h2>
          <div className="tabla-envoltura">
            <table className="tabla" aria-label="Servicios">
              <thead>
                <tr>
                  <th>Servicio</th>
                  <th>Estado</th>
                  <th>Detalle</th>
                  <th>Consulta</th>
                </tr>
              </thead>
              <tbody>
                {estado.servicios.map((s) => (
                  <tr key={s.nombre} className="entra">
                    <td>
                      {NOMBRES[s.nombre] ?? s.nombre}
                    </td>
                    <td>
                      <Estado estado={s.estado === 'OK' ? 'OK' : 'ERROR'} texto={TEXTO_ESTADO[s.estado] ?? s.estado} />
                    </td>
                    <td>{s.detalle}</td>
                    <td className="num">{formatearFecha(estado.generado)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <p className="ayuda">
            Cada servicio se consulta con un máximo de 3 segundos; uno caído no detiene a los demás. El aula virtual corre aparte,
            en su propio entorno.
          </p>
        </section>
      )}

      <Modelos />
    </Marco>
  )
}
