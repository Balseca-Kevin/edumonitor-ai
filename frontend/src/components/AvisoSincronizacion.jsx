import { useEffect } from 'react'
import { CIERRE_AVISO_MS } from './sincronizacion'

const NO_DISPONIBLE = 'no disponible'

function Cifra({ valor, texto, tono }) {
  const disponible = valor !== null && valor !== undefined
  return (
    <div className="aviso-sync-cifra">
      <div className={`num${tono ? ` aviso-sync-cifra--${tono}` : ''}${disponible ? '' : ' aviso-sync-cifra--no-disponible'}`}>
        {disponible ? valor : NO_DISPONIBLE}
      </div>
      <span>{texto}</span>
    </div>
  )
}

/** Cifras del aviso: las diferencias con la sincronización anterior (TA-21.A1) o, en la primera, lo que se importó. */
function cifrasDe(sincronizacion, cambios, atencion) {
  const errores = sincronizacion.errores?.length ?? 0
  const sinCambios = !cambios || cambios.noDisponibles?.includes('cambios')
  if (cambios && cambios.anteriorId === null) {
    return [
      { valor: sincronizacion.registros?.calificaciones ?? null, texto: 'notas importadas' },
      { valor: sincronizacion.registros?.estudiantes ?? null, texto: 'estudiantes' },
      { valor: atencion, texto: 'requieren atención', tono: 'atencion' },
      { valor: errores, texto: errores === 1 ? 'error' : 'errores' },
    ]
  }
  return [
    { valor: sinCambios ? null : cambios.conteos.notasNuevas, texto: 'notas nuevas' },
    { valor: sinCambios ? null : cambios.conteos.entregasNuevas, texto: 'entregas por calificar' },
    { valor: atencion, texto: 'requieren atención', tono: 'atencion' },
    { valor: errores, texto: errores === 1 ? 'error' : 'errores' },
  ]
}

function encabezado(codigo, sincronizacion, cambios) {
  if (sincronizacion.estado === 'FALLIDA') return { marca: 'LA SINCRONIZACIÓN FALLÓ', titulo: 'El aula virtual no respondió; los datos guardados no cambiaron' }
  const marca = sincronizacion.estado === 'PARCIAL' ? 'SINCRONIZACIÓN PARCIAL' : 'SINCRONIZACIÓN COMPLETADA'
  if (cambios?.anteriorId === null) return { marca, titulo: `${codigo} ya tiene los datos del aula virtual` }
  if (cambios && !cambios.noDisponibles?.includes('cambios') && cambios.conteos.total === 0) {
    return { marca, titulo: 'Sin cambios desde la sincronización anterior' }
  }
  return { marca, titulo: `${codigo} tiene datos nuevos del aula virtual` }
}

/**
 * TA-21.A2 · Aviso al terminar una sincronización (docs/sprint-3/diseno.md): baja desde arriba con un pequeño rebote, una línea de
 * brillo recorre el borde superior y se cierra solo a los 8 s. Cuatro cifras reales: notas nuevas y entregas por calificar nuevas
 * frente a la sincronización anterior (GET /api/cursos/{id}/sync/{sid}/cambios), estudiantes que requieren atención y errores. Si fue
 * PARCIAL usa arena y nombra la función que falló. `cambios` en null mientras carga o si no se pudo leer: «no disponible», nunca 0.
 */
export default function AvisoSincronizacion({ codigo, sincronizacion, cambios, atencion, onCerrar, onVerCambios }) {
  useEffect(() => {
    const temporizador = setTimeout(onCerrar, CIERRE_AVISO_MS)
    return () => clearTimeout(temporizador)
  }, [onCerrar, sincronizacion])

  const tono = sincronizacion.estado === 'COMPLETADA' ? 'ok' : sincronizacion.estado === 'PARCIAL' ? 'parcial' : 'fallida'
  const { marca, titulo } = encabezado(codigo, sincronizacion, cambios)
  const cambiadas = cambios?.conteos?.notasCambiadas ?? 0
  const fallaron = [...new Set((sincronizacion.errores ?? []).map((e) => e.funcion))]

  return (
    <div className={`aviso-sync aviso-sync--${tono}`} role="status" aria-live="polite" aria-label="Resultado de la sincronización">
      <div className="aviso-sync-brillo" aria-hidden="true" />
      <div className="aviso-sync-cuerpo">
        <div className="aviso-sync-cabeza">
          <span className="aviso-sync-icono" aria-hidden="true">
            {tono === 'ok' ? '✓' : '!'}
          </span>
          <div>
            <div className="mono aviso-sync-marca">{marca}</div>
            <div className="aviso-sync-titulo">{titulo}</div>
          </div>
        </div>
        {sincronizacion.estado !== 'FALLIDA' && (
          <div className="aviso-sync-cifras">
            {cifrasDe(sincronizacion, cambios, atencion).map((c) => (
              <Cifra key={c.texto} {...c} />
            ))}
          </div>
        )}
        {cambiadas > 0 && (
          <p className="aviso-sync-nota num">
            Además, {cambiadas} {cambiadas === 1 ? 'nota cambió' : 'notas cambiaron'} frente a la sincronización anterior.
          </p>
        )}
        {fallaron.length > 0 && <p className="aviso-sync-nota">Falló: {fallaron.join(', ')}</p>}
        <div className="aviso-sync-acciones">
          {onVerCambios && cambios?.conteos?.total > 0 && (
            <button type="button" className="boton aviso-sync-ver" onClick={onVerCambios}>
              Ver cambios en el libro
            </button>
          )}
          <button type="button" className="boton aviso-sync-cerrar" onClick={onCerrar}>
            Cerrar
          </button>
        </div>
      </div>
    </div>
  )
}
