
import './ui.css'

const ESTADOS = {
  COMPLETADA: 'success',
  OK: 'success',
  PARCIAL: 'warning',
  ACTIVA: 'warning',
  FALLIDA: 'error',
  ERROR: 'error',
  NO_DISPONIBLE: 'neutral',
  PENDIENTE: 'neutral',
  SIN_ENTREGA: 'neutral',
  REVISADA: 'neutral',
}

export default function StatusBadge({
  status,
  label,
  className = '',
}) {
  const estado = String(status ?? 'PENDIENTE').toUpperCase()
  const variant = ESTADOS[estado] ?? 'neutral'

  return (
    <span
      className={`ui-status ui-status--${variant} ${className}`.trim()}
    >
      <span className="ui-status__dot" aria-hidden="true" />
      <span>{label ?? estado.replaceAll('_', ' ')}</span>
    </span>
  )
}
