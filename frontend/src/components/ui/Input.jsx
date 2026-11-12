
import { useId } from 'react'
import './ui.css'

/**
 * HU-19 · Campo reutilizable con etiqueta,
 * descripción y validación accesible.
 */
export default function Input({
  label,
  id,
  error,
  helperText,
  className = '',
  type = 'text',
  ...props
}) {
  const generatedId = useId()
  const inputId = id || generatedId
  const errorId = `${inputId}-error`
  const helperId = `${inputId}-helper`

  const describedBy = [
    props['aria-describedby'],
    error ? errorId : null,
    helperText ? helperId : null,
  ]
    .filter(Boolean)
    .join(' ') || undefined

  return (
    <div className={`ui-field ${className}`.trim()}>
      {label && (
        <label className="ui-field__label" htmlFor={inputId}>
          {label}
        </label>
      )}

      <input
        {...props}
        id={inputId}
        type={type}
        className={`ui-input${error ? ' ui-input--error' : ''}`}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy}
      />

      {helperText && (
        <p id={helperId} className="ui-field__helper">
          {helperText}
        </p>
      )}

      {error && (
        <p id={errorId} className="ui-field__error">
          {error}
        </p>
      )}
    </div>
  )
}
