import { useEffect, useState } from 'react'
import Marco from '../../components/Marco'
import PaginaError from '../../components/PaginaError'
import Estado from '../../components/Estado'
import { formatearFecha } from '../../components/formato'
import { obtenerAuditoria } from '../../services/auditoria'
import { describirError } from '../../services/errores'

/** Nombre de cada acción en palabras del docente. */
export const ACCIONES = {
  INICIAR_SESION: 'Inicio de sesión',
  SINCRONIZAR: 'Sincronización con Moodle',
  CONSULTAR_ASISTENTE: 'Pregunta al asistente',
  HERRAMIENTA: 'Consulta del asistente a los datos',
  CAMBIAR_REGLAS: 'Cambio de umbrales de alerta',
  REVISAR_ALERTA: 'Alerta marcada como revisada',
  CAMBIAR_MODELO: 'Cambio del modelo del asistente',
  CONFIGURAR_MOODLE: 'Cambio de la conexión con Moodle',
  CREAR_REFUERZO: 'Plan de refuerzo creado',
  EDITAR_REFUERZO: 'Plan de refuerzo editado o aprobado',
  DESCARGAR_DOCUMENTO: 'Descarga de documento',
  EVALUAR_ASISTENTE: 'Evaluación del asistente',
  ACCESO_DENEGADO: 'Acceso no permitido',
}

function resultado(r) {
  const codigo = Number(r.resultado)
  if (codigo >= 200 && codigo < 300) return { estado: 'OK', texto: 'Correcto' }
  if (codigo === 401 || codigo === 403) return { estado: 'ERROR', texto: 'Denegado' }
  if (codigo >= 400 && codigo < 500) return { estado: 'ERROR', texto: 'Rechazado' }
  return { estado: 'ERROR', texto: 'Error' }
}

/**
 * HU-17 · «Auditoría», solo ADMIN: quién hizo qué y cuándo (sesiones, sincronizaciones, preguntas al asistente con el modelo que
 * respondió, consultas del asistente a los datos, cambios de configuración y accesos no permitidos). Sin contraseñas, tokens ni la
 * clave del modelo externo.
 */
export default function Auditoria() {
  const [accion, setAccion] = useState('')
  const [usuario, setUsuario] = useState('')
  const [filtro, setFiltro] = useState({ accion: '', usuario: '' })
  const [cargado, setCargado] = useState({ clave: null, datos: null, error: null })
  const clave = `${filtro.accion}|${filtro.usuario}`

  useEffect(() => {
    let vigente = true
    obtenerAuditoria(filtro)
      .then((datos) => vigente && setCargado({ clave, datos, error: null }))
      .catch((e) => vigente && setCargado({ clave, datos: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [filtro, clave])

  const { datos, error } = cargado.clave === clave ? cargado : { datos: null, error: null }
  if (error && ['sesion', 'acceso'].includes(error.tipo)) return <PaginaError error={error} />

  const acciones = datos?.acciones?.length ? datos.acciones : Object.keys(ACCIONES)

  return (
    <Marco>
      <header className="cabecera">
        <div>
          <p className="sobretitulo">Administración</p>
          <h1 className="titulo">Auditoría</h1>
        </div>
      </header>

      <form
        className="auditoria-filtros"
        onSubmit={(e) => {
          e.preventDefault()
          setFiltro({ accion, usuario })
        }}
      >
        <label>
          Acción
          <select value={accion} onChange={(e) => setAccion(e.target.value)}>
            <option value="">Todas</option>
            {acciones.map((a) => (
              <option key={a} value={a}>
                {ACCIONES[a] ?? a}
              </option>
            ))}
          </select>
        </label>
        <label>
          Usuario
          <input value={usuario} onChange={(e) => setUsuario(e.target.value)} placeholder="Usuario" />
        </label>
        <button type="submit" className="boton">
          Filtrar
        </button>
      </form>

      {error && (
        <div className="aviso aviso--error" role="alert">
          <strong>{error.titulo}</strong>
          {error.detalle}
        </div>
      )}
      {!datos && !error && <p className="ayuda">Consultando el registro…</p>}

      {datos && (
        <section className="seccion" aria-labelledby="titulo-registro">
          <div className="regla" />
          <h2 id="titulo-registro" className="seccion-titulo">
            Registro
          </h2>
          <p className="ayuda num">
            {datos.registros.length === datos.total
              ? `${datos.total} registros`
              : `Últimos ${datos.registros.length} de ${datos.total} registros`}
          </p>
          {datos.registros.length === 0 ? (
            <p className="ayuda">No hay registros con esos filtros.</p>
          ) : (
            <div className="tabla-envoltura">
              <table className="tabla" aria-label="Registro de auditoría">
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th>Usuario</th>
                    <th>Acción</th>
                    <th>Recurso</th>
                    <th>Detalle</th>
                    <th>Resultado</th>
                  </tr>
                </thead>
                <tbody>
                  {datos.registros.map((r) => {
                    const { estado, texto } = resultado(r)
                    return (
                      <tr key={r.id}>
                        <td className="num">{formatearFecha(r.fecha)}</td>
                        <td>{r.usuario ? `${r.usuario.nombre} (${r.usuario.usuario})` : 'Sin sesión'}</td>
                        <td>{ACCIONES[r.accion] ?? r.accion}</td>
                        <td className="mono">{r.entidad}</td>
                        <td>{r.detalle}</td>
                        <td>
                          <Estado estado={estado} texto={`${texto} · ${r.resultado}`} />
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </Marco>
  )
}
