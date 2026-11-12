import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import Alertas from './Alertas'

vi.mock('../../services/alertas', () => ({
  obtenerReglas: vi.fn(),
  actualizarReglas: vi.fn(),
  obtenerAlertas: vi.fn(),
  marcarAlerta: vi.fn(),
}))

import { actualizarReglas, marcarAlerta, obtenerAlertas, obtenerReglas } from '../../services/alertas'

// Reglas con la forma de GET /api/cursos/{id}/reglas (docs/contratos-api.md).
const reglas = [
  {
    codigo: 'R1',
    nombre: 'Descenso reciente',
    umbral: 1.5,
    activa: true,
    condicion: 'Tendencia menor o igual a −umbral con las últimas 3 notas',
  },
  { codigo: 'R2', nombre: 'Atrasos', umbral: 2, activa: true, condicion: 'Actividades vencidas sin entrega mayor o igual a umbral' },
  {
    codigo: 'R4',
    nombre: 'Promedio bajo',
    umbral: 7.0,
    activa: true,
    condicion: 'Promedio ponderado menor a umbral con al menos 4 notas',
  },
]

// Alertas con la forma de GET /api/cursos/{id}/alertas.
const alertaR4 = {
  id: 31,
  regla: 'R4',
  titulo: 'Requiere atención',
  estudiante: { id: 5, nombre: 'Ana', apellido: 'Pérez' },
  estado: 'ACTIVA',
  generada: '2026-11-10T07:12:00-05:00',
  revisada: null,
  evidencia: { promedio: 6.1, notas: 12, umbral: 7.0 },
}

const alertaR2 = {
  id: 32,
  regla: 'R2',
  titulo: 'Requiere atención',
  estudiante: { id: 6, nombre: 'Luis', apellido: 'Mora' },
  estado: 'ACTIVA',
  generada: '2026-11-10T07:13:00-05:00',
  revisada: null,
  evidencia: { actividades: [{ nombre: 'Tarea 5', fechaLimite: '2026-11-03T23:59:00-05:00' }], umbral: 2 },
}

function renderAlertas() {
  return render(
    <MemoryRouter initialEntries={['/cursos/1/alertas']}>
      <Routes>
        <Route path="/cursos/:id/alertas" element={<Alertas />} />
      </Routes>
    </MemoryRouter>,
  )
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('TA-12.4 · Pantalla Alertas', () => {
  it('agrupa las alertas por estudiante, muestra la evidencia en una línea y las reglas con sus umbrales', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([alertaR4, alertaR2])

    renderAlertas()

    expect(await screen.findByRole('heading', { name: 'Pérez, Ana' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Mora, Luis' })).toBeInTheDocument()

    // Evidencia en una línea con sus números, por regla.
    expect(screen.getByText('Promedio 6,10 · 12 notas · umbral 7,00')).toBeInTheDocument()
    expect(screen.getByText(/Actividades vencidas sin entrega: Tarea 5 .* umbral 2,00/)).toBeInTheDocument()

    // Título, código y nombre de la regla, y el botón en cada alerta.
    const tarjetas = screen.getAllByRole('article')
    expect(tarjetas).toHaveLength(2)
    expect(within(tarjetas[0]).getByText('Requiere atención')).toBeInTheDocument()
    expect(within(tarjetas[0]).getByText('R4')).toBeInTheDocument()
    expect(within(tarjetas[0]).getByText(/Promedio bajo/)).toBeInTheDocument()
    expect(within(tarjetas[1]).getByRole('button', { name: 'Marcar como revisada' })).toBeInTheDocument()

    // «Requiere atención» de una alerta activa lleva el punto ámbar, nunca solo color.
    expect(tarjetas[0].querySelector('.s2-estado--atencion')).toBeInTheDocument()

    // Panel de umbrales con un campo por regla y el botón Guardar.
    expect(screen.getByLabelText(/R1 Descenso reciente/)).toHaveValue(1.5)
    expect(screen.getByLabelText(/R2 Atrasos/)).toHaveValue(2)
    expect(screen.getByLabelText(/R4 Promedio bajo/)).toHaveValue(7)
    expect(screen.getByRole('button', { name: 'Guardar' })).toBeInTheDocument()

    expect(obtenerAlertas).toHaveBeenCalledWith('1', 'ACTIVA')
  })

  it('marca una alerta como revisada y sale de la lista de activas', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValueOnce([alertaR4]).mockResolvedValueOnce([])
    marcarAlerta.mockResolvedValue({ ...alertaR4, estado: 'REVISADA' })

    renderAlertas()

    fireEvent.click(await screen.findByRole('button', { name: 'Marcar como revisada' }))

    expect(marcarAlerta).toHaveBeenCalledWith('1', 31, 'REVISADA')
    expect(await screen.findByText('No hay alertas activas. Al sincronizar el curso, las reglas detectan quiénes requieren atención.')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Alerta marcada como revisada')
  })

  it('al guardar un umbral nuevo envía las tres reglas y confirma que se recalculó', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([alertaR4])
    actualizarReglas.mockResolvedValue(undefined)

    renderAlertas()

    const campoR4 = await screen.findByLabelText(/R4 Promedio bajo/)
    fireEvent.change(campoR4, { target: { value: '5' } })
    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }))

    await screen.findByText('Umbrales guardados')
    expect(actualizarReglas).toHaveBeenCalledWith('1', [
      { codigo: 'R1', umbral: 1.5, activa: true },
      { codigo: 'R2', umbral: 2, activa: true },
      { codigo: 'R4', umbral: 5, activa: true },
    ])
    expect(screen.getByRole('status')).toHaveTextContent('Las alertas del curso se recalcularon.')
    expect(obtenerReglas).toHaveBeenCalledTimes(2)
  })

  it('un umbral vacío no se envía y muestra el aviso de validación', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([])

    renderAlertas()

    fireEvent.change(await screen.findByLabelText(/R1 Descenso reciente/), { target: { value: '' } })
    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Captura un número en cada campo antes de guardar.')
    expect(actualizarReglas).not.toHaveBeenCalled()
  })

  it('cambia el filtro a revisadas y pide ese estado al backend', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([])

    renderAlertas()

    await screen.findByText('No hay alertas activas. Al sincronizar el curso, las reglas detectan quiénes requieren atención.')
    fireEvent.click(screen.getByRole('button', { name: 'Revisadas' }))

    expect(await screen.findByText('Todavía no hay alertas revisadas.')).toBeInTheDocument()
    expect(obtenerAlertas).toHaveBeenCalledWith('1', 'REVISADA')
    expect(screen.getByRole('button', { name: 'Revisadas' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('un error del backend se muestra en pantalla, nunca una lista vacía como si fuera 0', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockRejectedValue(
      Object.assign(new Error('HTTP 500'), {
        response: { status: 500, data: { title: 'Error interno', detail: 'Ocurrió un error inesperado.' } },
      }),
    )

    renderAlertas()

    const aviso = await screen.findByRole('alert')
    expect(aviso).toHaveTextContent('Error interno')
    expect(aviso).toHaveTextContent('Ocurrió un error inesperado.')
    expect(screen.queryByRole('button', { name: 'Guardar' })).not.toBeInTheDocument()
  })
})
