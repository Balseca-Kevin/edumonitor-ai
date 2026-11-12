
import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import Button from './Button'
import Input from './Input'
import Card from './Card'
import Table from './Table'
import StatusBadge from './StatusBadge'
import Alert from './Alert'

describe('HU-19 · Componentes reutilizables', () => {
  it('permite pulsar el botón primario', () => {
    const onClick = vi.fn()

    render(<Button onClick={onClick}>Guardar</Button>)

    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }))
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('deshabilita el botón mientras procesa', () => {
    render(<Button loading>Guardar</Button>)

    const button = screen.getByRole('button', {
      name: 'Procesando…',
    })

    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('asocia la etiqueta y el error con el campo', () => {
    render(
      <Input
        label="Usuario"
        error="El usuario es obligatorio"
      />,
    )

    const input = screen.getByRole('textbox', { name: 'Usuario' })

    expect(input).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByText('El usuario es obligatorio'))
      .toBeInTheDocument()
  })

  it('muestra el contenido de una tarjeta', () => {
    render(
      <Card title="Indicadores">
        <p>Promedio del curso</p>
      </Card>,
    )

    expect(
      screen.getByRole('heading', { name: 'Indicadores' }),
    ).toBeInTheDocument()

    expect(screen.getByText('Promedio del curso'))
      .toBeInTheDocument()
  })

  it('muestra registros en una tabla', () => {
    render(
      <Table
        columns={[
          { key: 'nombre', label: 'Nombre' },
          { key: 'periodo', label: 'Período' },
        ]}
        rows={[
          { id: 1, nombre: 'AGIL-4B', periodo: '2026-2' },
        ]}
      />,
    )

    expect(screen.getByText('AGIL-4B')).toBeInTheDocument()
    expect(screen.getByText('2026-2')).toBeInTheDocument()
  })

  it('muestra un estado con texto', () => {
    render(<StatusBadge status="FALLIDA" />)

    expect(screen.getByText('FALLIDA')).toBeInTheDocument()
  })

  it('muestra los avisos de error de forma accesible', () => {
    render(
      <Alert variant="error" title="Error">
        No se pudo sincronizar el curso.
      </Alert>,
    )

    expect(screen.getByRole('alert'))
      .toHaveTextContent('No se pudo sincronizar el curso.')
  })
})
