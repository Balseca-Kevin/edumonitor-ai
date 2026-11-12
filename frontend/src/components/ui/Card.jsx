
import './ui.css'

/**
 * HU-19 · Contenedor reutilizable.
 * Sin sombras, degradados ni decoración.
 */
export default function Card({
  title,
  children,
  className = '',
  as: Component = 'section',
  ...props
}) {
  return (
    <Component
      {...props}
      className={`ui-card ${className}`.trim()}
    >
      {title && (
        <h2 className="ui-card__title">{title}</h2>
      )}
      <div className="ui-card__content">
        {children}
      </div>
    </Component>
  )
}
