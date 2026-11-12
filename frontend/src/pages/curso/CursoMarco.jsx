
import { useEffect, useState } from 'react'
import { Outlet, useParams } from 'react-router-dom'
import MarcoS2 from '../../components/s2/MarcoS2'
import PaginaError from '../../components/s2/PaginaError'
import TiraPeriodo from '../../components/TiraPeriodo'
import { obtenerCurso } from '../../services/cursos'
import { describirError } from '../../services/errores'

/**
 * HU-19 / TA-19.2
 *
 * Estructura de las pantallas de un curso:
 * - Navegación lateral del Sprint 2.
 * - Cabecera con código y nombre del curso.
 * - Tira del período con U1, U2, U3 y U4.
 * - Última sincronización y su estado.
 * - Manejo de errores 401, 403 y 404.
 */
export default function CursoMarco() {
  const { id } = useParams()

  const [cargado, setCargado] = useState({
    id: null,
    curso: null,
    error: null,
  })

  useEffect(() => {
    let vigente = true

    obtenerCurso(id)
      .then((datos) => {
        if (vigente) {
          setCargado({
            id,
            curso: datos,
            error: null,
          })
        }
      })
      .catch((error) => {
        if (vigente) {
          setCargado({
            id,
            curso: null,
            error: describirError(error),
          })
        }
      })

    return () => {
      vigente = false
    }
  }, [id])

  // Evita mostrar información del curso anterior
  // mientras se carga uno nuevo.
  const { curso, error } =
    cargado.id === id
      ? cargado
      : { curso: null, error: null }

  // Errores de autorización y recursos inexistentes:
  // pantalla completa, sin barra lateral.
  if (
    error &&
    ['sesion', 'acceso', 'no-existe'].includes(error.tipo)
  ) {
    return <PaginaError error={error} />
  }

  return (
    <MarcoS2 curso={curso ?? { id }}>
      {error && (
        <div
          className="s2-aviso s2-aviso--error"
          role="alert"
        >
          <strong>{error.titulo}</strong>
          {error.detalle}
        </div>
      )}

      {!curso && !error && (
        <p className="s2-ayuda">
          Cargando el curso…
        </p>
      )}

      {curso && (
        <>
          {/* Cabecera del curso */}
          <header className="s2-cabecera">
            <div>
              <p className="s2-sobretitulo">
                {curso.nombreCorto}
              </p>

              <h1 className="s2-titulo">
                {curso.nombre}
              </h1>
            </div>
          </header>

          {/* Progreso de unidades y sincronización */}
          <TiraPeriodo
            cursoId={curso.id}
            ultimaSincronizacion={
              curso.ultimaSincronizacion
            }
          />

          {/* Panel, Alertas o Asistente */}
          <Outlet context={{ curso }} />
        </>
      )}
    </MarcoS2>
  )
}
