import { useState } from 'react'
import { formatearFecha } from './formato'
import { textoResultadoRefuerzo } from './lenguaje'
import { cambiarPlan, descargarDocumento, ESTADO_PLAN } from '../services/refuerzos'
import { describirError } from '../services/errores'
import { guardarArchivo } from './archivo'

/**
 * HU-15 · TA-15.A4 · El plan de refuerzo como foco del asistente: el borrador que redactó el asistente, editable por el docente;
 * «Aprobar borrador» lo deja fijo y habilita «Descargar PDF» y «Descargar DOCX». En un plan aprobado muestra qué pasó con cada tema
 * después del refuerzo (TA-15.A1). Nada se envía al estudiante.
 */
export default function PlanBorrador({ cursoId, plan, consultando, onRedactar, onCambio }) {
  const [texto, setTexto] = useState(plan.texto ?? '')
  const [estado, setEstado] = useState({ ocupado: false, error: null, aviso: null })
  const aprobado = plan.estado === 'APROBADO'
  const cambio = texto.trim() !== (plan.texto ?? '').trim()

  async function guardar(aprobar) {
    setEstado({ ocupado: true, error: null, aviso: null })
    try {
      const actualizado = await cambiarPlan(cursoId, plan.id, { texto: cambio ? texto : undefined, aprobar })
      setTexto(actualizado.texto ?? '')
      setEstado({ ocupado: false, error: null, aviso: aprobar ? 'Borrador aprobado' : 'Cambios guardados' })
      onCambio(actualizado)
    } catch (e) {
      setEstado({ ocupado: false, error: describirError(e).detalle ?? 'No se pudo guardar.', aviso: null })
    }
  }

  // HU-15 · La retroalimentación es de toda la actividad, sin temas.
  const retro = plan.tipo === 'RETROALIMENTACION'

  async function descargar(formato) {
    setEstado({ ocupado: true, error: null, aviso: null })
    try {
      guardarArchivo(await descargarDocumento(cursoId, plan.id, formato))
      setEstado({ ocupado: false, error: null, aviso: null })
    } catch (e) {
      setEstado({ ocupado: false, error: describirError(e).detalle ?? 'No se pudo descargar.', aviso: null })
    }
  }

  return (
    <section className="plan-borrador" aria-label={`${retro ? 'Retroalimentación' : 'Plan de refuerzo'} en el asistente`}>
      <div className="plan-borrador-cabeza">
        <div>
          <span className="mono">
            {retro ? 'RETROALIMENTACIÓN' : 'PLAN DE REFUERZO'} · {formatearFecha(plan.creado)}
          </span>
          <b>{retro ? plan.actividad.nombre : `${plan.actividad.nombre}: ${plan.temas.map((t) => t.nombre).join(', ')}`}</b>
          <span className="ayuda">{plan.estudiantes.map((e) => `${e.nombre} ${e.apellido}`).join(', ')}</span>
        </div>
        <span className={`chip-estado chip-estado--${plan.estado.toLowerCase()}`}>{ESTADO_PLAN[plan.estado] ?? plan.estado}</span>
      </div>

      {!plan.texto && !aprobado && (
        <div className="plan-borrador-vacio">
          <p className="ayuda">{consultando ? 'El asistente está redactando el borrador con las notas y los criterios del plan…' : 'El plan todavía no tiene borrador.'}</p>
          {!consultando && (
            <button type="button" className="boton boton--ia" onClick={onRedactar}>
              Redactar el borrador
            </button>
          )}
        </div>
      )}

      {plan.texto && !aprobado && (
        <>
          <label className="plan-borrador-texto">
            <span className="mono">BORRADOR · puedes editarlo antes de aprobarlo</span>
            {plan.conEstilo && (
              <span className="plan-borrador-estilo" title="Redactado siguiendo cómo corregiste tus dos últimos borradores aprobados">
                Con tu estilo
              </span>
            )}
            <textarea value={texto} onChange={(e) => setTexto(e.target.value)} rows={10} maxLength={20000} />
          </label>
          <div className="plan-borrador-acciones">
            {cambio && (
              <button type="button" className="boton boton--secundario" onClick={() => guardar(false)} disabled={estado.ocupado}>
                Guardar cambios
              </button>
            )}
            <button type="button" className="boton boton--ia" onClick={() => guardar(true)} disabled={estado.ocupado || !texto.trim()}>
              Aprobar borrador
            </button>
            <button type="button" className="boton boton--secundario" onClick={onRedactar} disabled={consultando || estado.ocupado}>
              Pedir otro borrador
            </button>
          </div>
        </>
      )}

      {aprobado && (
        <>
          <div className="plan-borrador-aprobado">{plan.texto}</div>
          <div className="plan-borrador-acciones">
            <button type="button" className="boton boton--ia" onClick={() => descargar('pdf')} disabled={estado.ocupado}>
              Descargar PDF
            </button>
            <button type="button" className="boton boton--secundario" onClick={() => descargar('docx')} disabled={estado.ocupado}>
              Descargar DOCX
            </button>
            <span className="ayuda">Aprobado el {formatearFecha(plan.aprobadoEn)}</span>
          </div>
          {plan.resultados.length > 0 && (
            <ul className="plan-resultados">
              {plan.resultados.map((r) => (
                <li key={`${r.estudianteId}-${r.temaId}`} className={`plan-resultado plan-resultado--${r.estado.toLowerCase()}`}>
                  {textoResultadoRefuerzo(r)}
                </li>
              ))}
            </ul>
          )}
        </>
      )}

      {estado.aviso && (
        <span className="ayuda" role="status">
          {estado.aviso}
        </span>
      )}
      {estado.error && (
        <span className="ayuda mapa-error" role="alert">
          {estado.error}
        </span>
      )}
    </section>
  )
}
