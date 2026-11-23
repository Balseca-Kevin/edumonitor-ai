import { useCallback, useEffect, useState } from 'react'
import { Outlet, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import Marco from '../../components/Marco'
import PaginaError from '../../components/PaginaError'
import CabeceraCurso from '../../components/CabeceraCurso'
import PantallaCarga from '../../components/PantallaCarga'
import AvisoSincronizacion from '../../components/AvisoSincronizacion'
import { PASOS_APERTURA, PAUSA_FINAL_MS, pasosDeSincronizacion } from '../../components/sincronizacion'
import { formatearFecha } from '../../components/formato'
import { TODO, busquedaTramo, segmentosDe, useTramo } from '../../components/tramo'
import { obtenerCambiosSincronizacion, obtenerCurso, sincronizarCurso } from '../../services/cursos'
import { obtenerLibro } from '../../services/libro'
import { contarEstudiantesConAlertas, obtenerIndicadores } from '../../services/indicadores'
import { obtenerPeriodo } from '../../services/periodo'
import { describirError } from '../../services/errores'

/**
 * R.2 · Barra blanca bajo la cabecera: el control segmentado (Todo el ciclo, cada parcial y cada unidad del curso) que filtra de
 * verdad el panel y las alertas, porque el backend calcula con las actividades del tramo; la fecha de la última sincronización,
 * «Sincronizar» y «Preguntar al asistente».
 */
export function BarraCurso({ curso, unidades, sincronizando, resultado, onSincronizar }) {
  const tramo = useTramo()
  const [parametros, setParametros] = useSearchParams()
  const navigate = useNavigate()
  const segmentos = segmentosDe(unidades)
  const ultima = curso.ultimaSincronizacion

  function elegir(codigo) {
    const siguientes = new URLSearchParams(parametros)
    if (codigo === TODO) siguientes.delete('tramo')
    else siguientes.set('tramo', codigo)
    setParametros(siguientes)
  }

  return (
    <div className="barra-curso">
      <div className="segmentado" role="group" aria-label="Filtrar por parcial o unidad">
        {segmentos.map((s) => (
          <button
            key={s.codigo}
            type="button"
            className="segmento"
            title={s.titulo}
            aria-pressed={tramo === s.codigo}
            onClick={() => elegir(s.codigo)}
          >
            {s.texto}
          </button>
        ))}
      </div>
      <span className="barra-curso-sync num" role="status">
        {sincronizando
          ? 'Sincronizando con el aula virtual…'
          : resultado
            ? resultado
            : ultima
              ? `Datos de la última sincronización · ${formatearFecha(ultima.fecha)}`
              : 'El curso todavía no se sincronizó'}
      </span>
      <button type="button" className="boton boton--secundario" onClick={onSincronizar} disabled={sincronizando}>
        {sincronizando ? 'Sincronizando…' : 'Sincronizar'}
      </button>
      <button type="button" className="boton boton--ia" onClick={() => navigate(`/cursos/${curso.id}/asistente${busquedaTramo(tramo)}`)}>
        Preguntar al asistente
      </button>
    </div>
  )
}

/**
 * HU-19, R.2 y TA-22.A5 · Estructura de las pantallas de un curso: barra lateral oscura con la navegación del curso y el contador de
 * alertas, cabecera oscura con la semana del período y la línea de tiempo, y la barra con el control segmentado; el Asistente va dentro de la
 * misma estructura (R.6). 401, 403 y 404 se ven en pantalla completa, sin barra lateral.
 */
export default function CursoMarco() {
  const { id } = useParams()
  const tramo = useTramo()
  const location = useLocation()
  const navigate = useNavigate()
  // TA-21.A2 · «Abrir curso» llega con state.abrir y «Sincronizar y abrir» con state.sincronizacion (el resultado del POST).
  const [apertura, setApertura] = useState(() => (location.state?.abrir ? { id, pasos: PASOS_APERTURA.map(() => 'girando') } : null))
  const [cargaSync, setCargaSync] = useState(null)
  const [aviso, setAviso] = useState(() =>
    location.state?.sincronizacion ? { id, sincronizacion: location.state.sincronizacion, cambios: null, atencion: null } : null,
  )

  const marcarPaso = useCallback((cursoId, indice) => {
    setApertura((anterior) =>
      anterior && anterior.id === cursoId ? { ...anterior, pasos: anterior.pasos.map((p, i) => (i === indice ? 'listo' : p)) } : anterior,
    )
  }, [])
  const [cargado, setCargado] = useState({ id: null, curso: null, error: null })
  const [contexto, setContexto] = useState({ id: null, unidades: [], alertas: null, periodo: null, estudiantes: null })
  const [version, setVersion] = useState(0)
  const [sincronizando, setSincronizando] = useState(false)
  const [resultado, setResultado] = useState(null)

  useEffect(() => {
    let vigente = true
    obtenerCurso(id)
      .then((datos) => vigente && setCargado({ id, curso: datos, error: null }))
      .catch((error) => vigente && setCargado({ id, curso: null, error: describirError(error) }))
    return () => {
      vigente = false
    }
  }, [id, version])

  // Unidades reales del curso para el control segmentado y estudiantes con alertas para el contador. Sin sincronizar (409) no hay
  // unidades y queda solo «Todo el ciclo»; el contador sin dato no se muestra, nunca como 0.
  useEffect(() => {
    let vigente = true
    Promise.all([obtenerIndicadores(id).catch(() => null), contarEstudiantesConAlertas(id)]).then(([indicadores, alertas]) => {
      if (!vigente) return
      setContexto((anterior) => ({
        ...(anterior.id === id ? anterior : { periodo: null }),
        id,
        unidades: indicadores?.unidades ?? [],
        alertas,
        estudiantes: indicadores?.curso?.estudiantes ?? null,
      }))
      marcarPaso(id, 1)
    })
    obtenerPeriodo(id)
      .catch(() => null)
      .then((periodo) => {
        if (!vigente) return
        setContexto((anterior) => ({ ...(anterior.id === id ? anterior : { unidades: [], alertas: null, estudiantes: null }), id, periodo }))
        marcarPaso(id, 2)
      })
    return () => {
      vigente = false
    }
  }, [id, version, marcarPaso])

  // «Abriendo curso…»: el libro de calificaciones es el primer paso; sus datos los vuelve a pedir la pantalla del libro.
  const abriendo = apertura?.id === id
  useEffect(() => {
    if (!abriendo) return
    let vigente = true
    obtenerLibro(id)
      .catch(() => null)
      .then(() => vigente && marcarPaso(id, 0))
    return () => {
      vigente = false
    }
  }, [abriendo, id, marcarPaso])

  const aperturaTerminada = Boolean(apertura?.pasos.every((p) => p !== 'girando'))
  useEffect(() => {
    if (!aperturaTerminada) return
    const t = setTimeout(() => setApertura(null), PAUSA_FINAL_MS)
    return () => clearTimeout(t)
  }, [aperturaTerminada])

  // Aviso con las cifras reales de la sincronización: cambios frente a la anterior (TA-21.A1) y estudiantes que requieren atención.
  const syncDelAviso = aviso?.sincronizacion
  useEffect(() => {
    if (!syncDelAviso?.id || syncDelAviso.estado === 'FALLIDA') return
    let vigente = true
    Promise.all([obtenerCambiosSincronizacion(id, syncDelAviso.id).catch(() => null), contarEstudiantesConAlertas(id)]).then(
      ([cambios, atencion]) =>
        vigente && setAviso((anterior) => (anterior?.sincronizacion === syncDelAviso ? { ...anterior, cambios, atencion } : anterior)),
    )
    return () => {
      vigente = false
    }
  }, [id, syncDelAviso])
  const cerrarAviso = useCallback(() => setAviso(null), [])

  // Lo que llegó desde Mis cursos (abrir o el resultado de la sincronización) se usa una vez: se limpia para no repetirlo al recargar.
  const llegada = location.state
  useEffect(() => {
    if (!llegada?.abrir && !llegada?.sincronizacion) return
    navigate(`${location.pathname}${location.search}`, { replace: true, state: null })
  }, [llegada, navigate, location.pathname, location.search])

  async function sincronizar() {
    setSincronizando(true)
    setResultado(null)
    setAviso(null)
    setCargaSync({ id, resultado: null })
    try {
      const sync = await sincronizarCurso(id)
      setCargaSync({ id, resultado: sync })
      setTimeout(() => {
        setCargaSync(null)
        setAviso({ id, sincronizacion: sync, cambios: null, atencion: null })
      }, PAUSA_FINAL_MS)
      setResultado(
        sync?.estado === 'COMPLETADA'
          ? 'Sincronización completada'
          : sync?.estado === 'PARCIAL'
            ? `Sincronización parcial: falló ${sync.errores?.map((e) => e.funcion).join(', ') || 'una función del aula virtual'}`
            : 'La sincronización falló; los datos guardados no cambiaron',
      )
      setVersion((v) => v + 1)
    } catch (error) {
      const descrito = describirError(error)
      setCargaSync(null)
      setResultado(`${descrito.titulo}. ${descrito.detalle ?? ''}`.trim())
    } finally {
      setSincronizando(false)
    }
  }

  // Lo que llegó de otro curso no se muestra mientras carga el nuevo.
  const { curso, error } = cargado.id === id ? cargado : { curso: null, error: null }
  const { unidades, alertas, periodo, estudiantes } = contexto.id === id ? contexto : { unidades: [], alertas: null, periodo: null, estudiantes: null }

  if (error && ['sesion', 'acceso', 'no-existe'].includes(error.tipo)) {
    return <PaginaError error={error} />
  }

  return (
    <Marco
      curso={curso ?? { id }}
      alertas={alertas}
      busqueda={busquedaTramo(tramo)}
      cabecera={curso ? <CabeceraCurso curso={curso} periodo={periodo} estudiantes={estudiantes} /> : null}
      barra={
        curso ? (
          <BarraCurso curso={curso} unidades={unidades} sincronizando={sincronizando} resultado={resultado} onSincronizar={sincronizar} />
        ) : null
      }
    >
      {error && (
        <div className="aviso aviso--error" role="alert">
          <strong>{error.titulo}</strong>
          {error.detalle}
        </div>
      )}
      {!curso && !error && <p className="ayuda">Cargando el curso…</p>}
      {curso && <Outlet context={{ curso, version, periodo }} />}
      {abriendo && (
        <PantallaCarga
          titulo="Abriendo curso…"
          codigo={curso?.nombreCorto}
          subtitulo="Con los datos de la última sincronización."
          pasos={PASOS_APERTURA.map((texto, i) => ({ texto, estado: apertura.pasos[i] }))}
        />
      )}
      {cargaSync?.id === id && (
        <PantallaCarga
          titulo="Sincronizando curso…"
          codigo={curso?.nombreCorto ? `${curso.nombreCorto} · AULA VIRTUAL` : null}
          subtitulo="Leyendo matriculados, notas y entregas del aula virtual. Se guarda sin duplicar registros."
          pasos={pasosDeSincronizacion(cargaSync.resultado)}
        />
      )}
      {aviso?.id === id && (
        <AvisoSincronizacion
          codigo={curso?.nombreCorto ?? ''}
          sincronizacion={aviso.sincronizacion}
          cambios={aviso.cambios}
          atencion={aviso.atencion}
          onCerrar={cerrarAviso}
        />
      )}
    </Marco>
  )
}
