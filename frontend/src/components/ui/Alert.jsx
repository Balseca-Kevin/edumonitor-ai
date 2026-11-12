
import './ui.css'

export default function Alert({
  title,
  children,
  variant = 'info',
  className = '',
}) {
  const variants = ['info', 'success', 'warning', 'error']
  const safeVariant = variants.includes(variant)
    ? variant
    : 'info'

  return (
    <div
      className={`ui-alert ui-alert--${safeVariant} ${className}`.trim()}
      role={safeVariant === 'error' ? 'alert' : 'status'}
    >
      {title && (
        <strong className="ui-alert__title">{title}</strong>
      )}
      {children && (
        <div className="ui-alert__content">{children}</div>
      )}
    </div>
  )
}
