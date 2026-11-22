import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import Alertas from './Alertas'

vi.mock('../../services/alertas', () => ({
  obtenerReglas: vi.fn(),
  actualizarReglas: vi.fn(),
  obtenerAlertas: vi.fn(),
  marcarAlerta: vi.fn(),
}))
vi.mock('../../services/indicadores', () => ({ obtenerIndicadores: vi.fn() }))
vi.mock('../../services/libro', async (original) => ({ ...(await original()), obtenerLibro: vi.fn() }))

import { actualizarReglas, marcarAlerta, obtenerAlertas, obtenerReglas } from '../../services/alertas'
import { obtenerIndicadores } from '../../services/indicadores'
import { obtenerLibro } from '../../services/libro'

// Reglas con la forma de GET /api/cursos/{id}/reglas (docs/contratos-api.md).
const reglas = [
  { codigo: 'R1', nombre: 'Descenso reciente', umbral: 1.5, activa: true, condicion: 'Tendencia menor o igual a −umbral con las últimas 3 notas' },
  { codigo: 'R2', nombre: 'Atrasos', umbral: 2, activa: true, condicion: 'Actividades vencidas sin entrega mayor o igual a umbral' },
  { codigo: 'R4', nombre: 'Promedio bajo', umbral: 7.0, activa: true, condicion: 'Promedio ponderado menor a umbral con al menos 4 notas' },
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

function Ubicacion() {
  const { pathname, search } = useLocation()
  return <p data-testid="ruta">{pathname + search}</p>
}

function renderAlertas(ruta = '/cursos/1/alertas') {
  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <Routes>
        <Route path="/cursos/:id/alertas" element={<Alertas />} />
        <Route path="*" element={<Ubicacion />} />
      </Routes>
    </MemoryRouter>,
  )
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('R.5 · Alertas en tarjetas', () => {
  it('una tarjeta por alerta con la regla, «Requiere atención», la evidencia en una frase y «Debe: …»', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([alertaR4, alertaR2])
    renderAlertas()

    const tarjetaR4 = await screen.findByRole('article', { name: 'Promedio bajo de Pérez, Ana' })
    expect(within(tarjetaR4).getByText('Requiere atención')).toBeInTheDocument()
    expect(within(tarjetaR4).getByText('Promedio 6,10 con 12 notas registradas; el mínimo es 7,0')).toBeInTheDocument()
    const tarjetaR2 = screen.getByRole('article', { name: 'Atrasos de Mora, Luis' })
    expect(within(tarjetaR2).getByText('Debe: Tarea 5')).toBeInTheDocument()
    expect(within(tarjetaR4).queryByText(/Debe:/)).toBeNull()
    expect(obtenerAlertas).toHaveBeenCalledWith('1', 'ACTIVA', 'todo')
    expect(screen.queryByText(/\bR[124]\b/)).toBeNull() // los códigos de regla no se muestran
  })

  it('los chips por regla filtran las tarjetas', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([alertaR4, alertaR2])
    renderAlertas()
    await screen.findByRole('article', { name: 'Promedio bajo de Pérez, Ana' })
    const chips = screen.getByRole('group', { name: 'Filtrar alertas por regla' })

    fireEvent.click(within(chips).getByRole('button', { name: 'Atrasos' }))

    expect(screen.getAllByRole('article')).toHaveLength(1)
    expect(screen.getByRole('article', { name: 'Atrasos de Mora, Luis' })).toBeInTheDocument()
    fireEvent.click(within(chips).getByRole('button', { name: 'Descenso reciente' }))
    expect(screen.getByText('No hay alertas con este filtro.')).toBeInTheDocument()
  })

  it('«Marcar revisada» la saca de las activas y «Revisadas» pide ese estado al backend', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas
      .mockResolvedValueOnce([alertaR4])
      .mockResolvedValueOnce([])
      .mockResolvedValue([{ ...alertaR4, estado: 'REVISADA', revisada: '2026-11-22T10:00:00-05:00' }])
    marcarAlerta.mockResolvedValue({ ...alertaR4, estado: 'REVISADA' })
    renderAlertas()

    fireEvent.click(await screen.findByRole('button', { name: 'Marcar revisada' }))

    expect(await screen.findByText('Alerta marcada como revisada')).toBeInTheDocument()
    expect(marcarAlerta).toHaveBeenCalledWith('1', 31, 'REVISADA')
    fireEvent.click(screen.getByRole('button', { name: 'Revisadas' }))
    expect(await screen.findByText('Revisada')).toBeInTheDocument()
    expect(obtenerAlertas).toHaveBeenLastCalledWith('1', 'REVISADA', 'todo')
  })

  it('el modal «Umbrales de alerta» envía las tres reglas, recalcula y se cierra', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([alertaR4])
    actualizarReglas.mockResolvedValue()
    renderAlertas()

    fireEvent.click(await screen.findByRole('button', { name: 'Umbrales de alerta' }))
    const modal = screen.getByRole('dialog', { name: 'Umbrales de alerta' })
    fireEvent.change(within(modal).getByLabelText('Promedio bajo: menor a'), { target: { value: '6.5' } })
    fireEvent.click(within(modal).getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('Umbrales guardados')).toBeInTheDocument()
    expect(actualizarReglas).toHaveBeenCalledWith('1', [
      { codigo: 'R1', umbral: 1.5, activa: true },
      { codigo: 'R2', umbral: 2, activa: true },
      { codigo: 'R4', umbral: 6.5, activa: true },
    ])
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('un umbral vacío no se envía y el aviso se ve dentro del modal; Escape lo cierra', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([])
    renderAlertas()

    fireEvent.click(await screen.findByRole('button', { name: 'Umbrales de alerta' }))
    const modal = screen.getByRole('dialog', { name: 'Umbrales de alerta' })
    fireEvent.change(within(modal).getByLabelText('Atrasos: actividades vencidas sin entregar desde'), { target: { value: '' } })
    fireEvent.click(within(modal).getByRole('button', { name: 'Guardar' }))

    expect(within(modal).getByText('Umbral no válido')).toBeInTheDocument()
    expect(actualizarReglas).not.toHaveBeenCalled()
    fireEvent.keyDown(window, { key: 'Escape' })
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('«Armar plan de refuerzo» lleva el estudiante y el tramo, y el nombre abre la ficha', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([alertaR4])
    obtenerIndicadores.mockResolvedValue({ unidades: [], estudiantes: [{ id: 5, nombre: 'Ana', apellido: 'Pérez', promedioPonderado: 6.1, categorias: [] }] })
    obtenerLibro.mockResolvedValue({ parciales: [], estudiantes: [] })
    renderAlertas('/cursos/1/alertas?tramo=u3')

    fireEvent.click(await screen.findByRole('button', { name: 'Pérez, Ana' }))
    const ficha = await screen.findByRole('dialog', { name: 'Ficha de Pérez, Ana' })
    expect(within(ficha).getByText('6,10')).toBeInTheDocument()
    expect(obtenerIndicadores).toHaveBeenCalledWith('1', 'u3')
    fireEvent.click(within(ficha).getByRole('button', { name: 'Cerrar ficha' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())

    fireEvent.click(screen.getByRole('button', { name: 'Armar plan de refuerzo' }))
    expect(screen.getByTestId('ruta')).toHaveTextContent('/cursos/1/refuerzos?estudiante=5&tramo=u3')
  })

  it('un error del backend se muestra en pantalla, nunca una lista vacía como si fuera 0', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockRejectedValue({ response: { status: 500, data: { title: 'Error interno', detail: 'Falló el cálculo.' } } })
    renderAlertas()

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.queryByText(/No hay alertas activas/)).toBeNull()
  })

  it('la evidencia de descenso reciente dice cuánto bajaron las últimas notas', async () => {
    obtenerReglas.mockResolvedValue(reglas)
    obtenerAlertas.mockResolvedValue([
      { ...alertaR4, id: 40, regla: 'R1', evidencia: { ultimas: [5, 4.5, 4], anteriores: [7.5, 8], diferencia: -3.25, umbral: 1.5 } },
    ])
    renderAlertas()

    const tarjeta = await screen.findByRole('article', { name: 'Descenso reciente de Pérez, Ana' })
    expect(within(tarjeta).getByText(/3,3 puntos bajo las anteriores/)).toBeInTheDocument()
  })
})
