
import {
  afterEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'
import {
  cleanup,
  render,
  screen,
} from '@testing-library/react'
import TiraPeriodo from './TiraPeriodo'
import { unidadActual } from './lenguaje'
import { obtenerIndicadores } from '../services/indicadores'

vi.mock('../services/indicadores', () => ({
  obtenerIndicadores: vi.fn(),
}))

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('HU-19 · Tira del período', () => {
  it('muestra el avance real de las cuatro unidades', async () => {
    obtenerIndicadores.mockResolvedValue({
      unidades: [
        { numero: 1, actividades: 4, calificadas: 3 },
        { numero: 2, actividades: 5, calificadas: 2 },
        { numero: 3, actividades: 4, calificadas: 1 },
        { numero: 4, actividades: 6, calificadas: 0 },
      ],
    })

    render(<TiraPeriodo cursoId={1} />)

    expect(
      await screen.findByLabelText(
        'Unidad 1: 3 de 4 actividades calificadas',
      ),
    ).toBeInTheDocument()

    expect(screen.getByText('3/4')).toBeInTheDocument()
    expect(screen.getByText('2/5')).toBeInTheDocument()
    expect(screen.getByText('1/4')).toBeInTheDocument()
    expect(screen.getByText('0/6')).toBeInTheDocument()

    expect(obtenerIndicadores).toHaveBeenCalledWith(1)
  })

  it('distingue una unidad completada', async () => {
    obtenerIndicadores.mockResolvedValue({
      unidades: [
        { numero: 1, actividades: 4, calificadas: 4 },
      ],
    })

    render(<TiraPeriodo cursoId={1} />)

    const unidad = await screen.findByLabelText(
      'Unidad 1: 4 de 4 actividades calificadas',
    )

    expect(unidad).toHaveClass('tira-unidad--completa')
  })

  it('muestra Sin datos cuando la API devuelve 409', async () => {
    obtenerIndicadores.mockRejectedValue({
      response: {
        status: 409,
        data: { title: 'Curso sin sincronizar' },
      },
    })

    render(<TiraPeriodo cursoId={1} />)

    await screen.findByLabelText(
      'Unidad 1: sin datos disponibles',
    )

    expect(screen.getAllByText('Sin datos')).toHaveLength(4)
    expect(screen.queryByText('0/0')).not.toBeInTheDocument()
  })

  it('marca como actual la primera unidad con actividades sin calificar', async () => {
    obtenerIndicadores.mockResolvedValue({
      unidades: [
        { numero: 1, actividades: 4, calificadas: 4 },
        { numero: 2, actividades: 4, calificadas: 4 },
        { numero: 3, actividades: 4, calificadas: 2 },
        { numero: 4, actividades: 4, calificadas: 0 },
      ],
    })

    render(<TiraPeriodo cursoId={1} ultimaSincronizacion={{ fecha: '2026-10-30T16:59:00-05:00', estado: 'COMPLETADA' }} />)

    const u3 = await screen.findByLabelText('Unidad 3: 2 de 4 actividades calificadas')
    expect(u3).toHaveClass('tira-unidad--actual')
    expect(u3).toHaveTextContent('2/4 · actual')
    expect(screen.getByLabelText('Unidad 4: 0 de 4 actividades calificadas')).toHaveClass('tira-unidad--futura')
    expect(screen.getByText('COMPLETADA')).toBeInTheDocument()
  })

  it('unidadActual devuelve null cuando todas las unidades están completas', () => {
    expect(unidadActual([{ numero: 1, actividades: 4, calificadas: 4 }])).toBeNull()
    expect(unidadActual([{ numero: 2, actividades: 4, calificadas: 1 }, { numero: 1, actividades: 4, calificadas: 3 }])).toBe(1)
  })
})
