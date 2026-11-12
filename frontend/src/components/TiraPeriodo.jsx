import { useEffect, useState } from 'react'
import { obtenerIndicadores } from '../services/indicadores'
import Estado from './Estado'
import { formatearFecha } from './formato'
import { unidadActual, valida } from './lenguaje'

const PARCIALES = [
  { numero: 1, unidades: [1, 2] },
  { numero: 2, unidades: [3, 4] },
]

function Unidad({ numero, unidad, cargando, actual }) {
  const disponible = valida(unidad)
  const completa = disponible && unidad.actividades > 0 && unidad.calificadas === unidad.actividades
  const estado = !disponible ? 'sin-datos' : completa ? 'completa' : numero === actual ? 'actual' : 'futura'
  const avance = disponible && unidad.actividades > 0 ? Math.round((unidad.calificadas / unidad.actividades) * 100) : 0

  return (
    <div
      className={`tira-unidad tira-unidad--${estado}`}
      style={estado === 'actual' ? { '--avance': `${avance}%` } : undefined}
      aria-label={`Unidad ${numero}: ${disponible ? `${unidad.calificadas} de ${unidad.actividades} actividades calificadas` : 'sin datos disponibles'}`}
    >
      <b>U{numero}</b>
      <span>
        <span>{cargando ? 'Consultando' : disponible ? `${unidad.calificadas}/${unidad.actividades}` : 'Sin datos'}</span>
        {estado === 'actual' && <span> · actual</span>}
      </span>
    </div>
  )
}

/**
 * HU-19 · TA-19.2 · Tira del período en la cabecera oscura del curso (diseño D): Parcial 1 con U1 y U2, Parcial 2 con U3 y U4,
 * cada unidad con «calificadas/actividades» de `unidades` de GET /api/cursos/{id}/indicadores; a la derecha, la fecha y el estado
 * de la última sincronización. Un 409 o un error se ven como «Sin datos», nunca como 0/0.
 */
export default function TiraPeriodo({ cursoId, ultimaSincronizacion }) {
  const [consulta, setConsulta] = useState({ cursoId: null, cargando: true, unidades: [] })

  useEffect(() => {
    let activo = true
    obtenerIndicadores(cursoId)
      .then((datos) => activo && setConsulta({ cursoId, cargando: false, unidades: Array.isArray(datos?.unidades) ? datos.unidades : [] }))
      .catch(() => activo && setConsulta({ cursoId, cargando: false, unidades: [] }))
    return () => {
      activo = false
    }
  }, [cursoId])

  const cargando = consulta.cursoId !== cursoId || consulta.cargando
  const unidades = cargando ? [] : consulta.unidades
  const actual = unidadActual(unidades)

  return (
    <div className="cabecera-curso-derecha">
      <section className="tira" aria-label="Avance del período académico">
        {PARCIALES.map((parcial) => (
          <div className="tira-parcial" key={parcial.numero}>
            <span className="tira-etiqueta">PARCIAL {parcial.numero}</span>
            <div className="tira-unidades">
              {parcial.unidades.map((numero) => (
                <Unidad
                  key={numero}
                  numero={numero}
                  cargando={cargando}
                  actual={actual}
                  unidad={unidades.find((u) => u.numero === numero)}
                />
              ))}
            </div>
          </div>
        ))}
      </section>

      <div className="sincronizacion">
        <span>
          {ultimaSincronizacion?.fecha ? `Sincronizado ${formatearFecha(ultimaSincronizacion.fecha)}` : 'Sin sincronizar'}
        </span>
        {ultimaSincronizacion?.estado && (
          <span>
            <Estado estado={ultimaSincronizacion.estado} />
          </span>
        )}
      </div>
    </div>
  )
}
