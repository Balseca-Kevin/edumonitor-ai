
import './ui.css'

/**
 * HU-19 · Botón reutilizable del sistema de diseño.
 *
 * variant: primary | secondary | danger
 * size: normal | small
 */
export default function Button({
  children,
  variant = 'primary',
  size = 'normal',
  loading = false,
  disabled = false,
  className = '',
  type = 'button',
  ...props
}) {
  const classes = [
    'ui-button',
    `ui-button--${variant}`,
    `ui-button--${size}`,
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <button
      {...props}
      type={type}
      className={classes}
      disabled={disabled || loading}
      aria-busy={loading}
    >
      {loading ? 'Procesando…' : children}
    </button>
  )
}
