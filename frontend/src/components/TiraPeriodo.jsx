
import { useEffect, useState } from 'react'
import { obtenerIndicadores } from '../services/indicadores'
import StatusBadge from './ui/StatusBadge'
import './TiraPeriodo.css'

const PARCIALES = [
  { numero: 1, unidades: [1, 2] },
  { numero: 2, unidades: [3, 4] },
]

function SegmentoUnidad({ numero, unidad, cargando }) {
  const total = unidad?.actividades
  const calificadas = unidad?.calificadas

  const disponible =
    Number.isInteger(total) &&
    Number.isInteger(calificadas) &&
    total >= 0 &&
    calificadas >= 0 &&
    calificadas <= total

  const completa =
    disponible && total > 0 && calificadas === total

  const estado = completa
    ? 'completa'
    : disponible
      ? 'pendiente'
      : 'sin-datos'

  return (
    <div
      className={`hu19-unidad hu19-unidad--${estado}`}
      aria-label={`Unidad ${numero}: ${
        disponible
          ? `${calificadas} de ${total} actividades calificadas`
          : 'sin datos disponibles'
      }`}
    >
      <strong>U{numero}</strong>

      <span className="hu19-unidad__avance">
        {cargando
          ? 'Consultando'
          : disponible
            ? `${calificadas}/${total}`
            : 'Sin datos'}
      </span>
    </div>
  )
}

export default function TiraPeriodo({
  cursoId,
  ultimaSincronizacion,
}) {
  const [consulta, setConsulta] = useState({
    cursoId: null,
    cargando: true,
    unidades: [],
  })

  useEffect(() => {
    let activo = true

    obtenerIndicadores(cursoId)
      .then((datos) => {
        if (!activo) return

        setConsulta({
          cursoId,
          cargando: false,
          unidades: Array.isArray(datos?.unidades)
            ? datos.unidades
            : [],
        })
      })
      .catch(() => {
        if (!activo) return

        // Un 409, o un servicio no disponible,
        // no equivale a cero actividades calificadas.
        setConsulta({
          cursoId,
          cargando: false,
          unidades: [],
        })
      })

    return () => {
      activo = false
    }
  }, [cursoId])

  const cargando = consulta.cursoId !== cursoId ||
    consulta.cargando

  const unidades = cargando ? [] : consulta.unidades

  return (
    <section
      className="hu19-periodo"
      aria-label="Progreso del período académico"
    >
      <div className="hu19-periodo__parciales">
        {PARCIALES.map((parcial) => (
          <div
            className="hu19-periodo__parcial"
            key={parcial.numero}
          >
            <h2>Parcial {parcial.numero}</h2>

            <div className="hu19-periodo__unidades">
              {parcial.unidades.map((numero) => (
                <SegmentoUnidad
                  key={numero}
                  numero={numero}
                  cargando={cargando}
                  unidad={unidades.find(
                    (unidad) => unidad.numero === numero,
                  )}
                />
              ))}
            </div>
          </div>
        ))}
      </div>

      <div className="hu19-periodo__sincronizacion">
        <span>Última sincronización</span>

        <StatusBadge
          status={
            ultimaSincronizacion?.estado ??
            'PENDIENTE'
          }
          label={
            ultimaSincronizacion?.estado ??
            'Sin sincronizar'
          }
        />

        {ultimaSincronizacion?.fecha && (
          <time
            dateTime={ultimaSincronizacion.fecha}
            className="hu19-periodo__fecha"
          >
            {new Date(
              ultimaSincronizacion.fecha,
            ).toLocaleString('es-EC', {
              dateStyle: 'short',
              timeStyle: 'short',
            })}
          </time>
        )}
      </div>
    </section>
  )
}
