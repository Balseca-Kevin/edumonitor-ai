import { useEffect, useState } from 'react'
import { Outlet, useParams } from 'react-router-dom'
import MarcoS2 from '../../components/s2/MarcoS2'
import PaginaError from '../../components/s2/PaginaError'
import Estado from '../../components/s2/Estado'
import { formatearFecha } from '../../components/s2/formato'
import { obtenerCurso } from '../../services/cursos'
import { describirError } from '../../services/errores'

/**
 * Marco de las pantallas de un curso (/cursos/:id/*): cabecera con nombre corto, nombre y última sincronización.
 * Un curso ajeno (403) o una sesión vencida (401) se muestran en pantalla completa, sin barra lateral.
 */
export default function CursoMarco() {
  const { id } = useParams()
  const [cargado, setCargado] = useState({ id: null, curso: null, error: null })

  useEffect(() => {
    let vigente = true
    obtenerCurso(id)
      .then((datos) => vigente && setCargado({ id, curso: datos, error: null }))
      .catch((e) => vigente && setCargado({ id, curso: null, error: describirError(e) }))
    return () => {
      vigente = false
    }
  }, [id])

  // Al cambiar de curso, lo cargado del curso anterior no se muestra.
  const { curso, error } = cargado.id === id ? cargado : { curso: null, error: null }

  if (error && ['sesion', 'acceso', 'no-existe'].includes(error.tipo)) {
    return <PaginaError error={error} />
  }

  const ultima = curso?.ultimaSincronizacion

  return (
    <MarcoS2 curso={curso ?? { id }}>
      {error && (
        <div className="s2-aviso s2-aviso--error" role="alert">
          <strong>{error.titulo}</strong>
          {error.detalle}
        </div>
      )}

      {!curso && !error && <p className="s2-ayuda">Cargando el curso…</p>}

      {curso && (
        <>
          <header className="s2-cabecera">
            <div>
              <p className="s2-sobretitulo">{curso.nombreCorto}</p>
              <h1 className="s2-titulo">{curso.nombre}</h1>
            </div>
            <div className="s2-cabecera-dato">
              {ultima ? (
                <>
                  <span className="s2-num">Última sincronización: {formatearFecha(ultima.fecha)}</span>
                  <br />
                  <Estado estado={ultima.estado} />
                </>
              ) : (
                <Estado estado="SIN_SINCRONIZAR" texto="Sin sincronizar" />
              )}
            </div>
          </header>
          <Outlet context={{ curso }} />
        </>
      )}
    </MarcoS2>
  )
}
