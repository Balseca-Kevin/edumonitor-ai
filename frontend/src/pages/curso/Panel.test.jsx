import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import Panel from './Panel'

vi.mock('../../services/indicadores', () => ({ obtenerIndicadores: vi.fn(), contarAlertasActivas: vi.fn() }))

import { contarAlertasActivas, obtenerIndicadores } from '../../services/indicadores'

// Recharts usa ResizeObserver, que jsdom no trae.
beforeAll(() => {
  globalThis.ResizeObserver ??= class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
})

// Respuesta con la forma de GET /api/cursos/{id}/indicadores (docs/contratos-api.md).
function indicadores({ finalizacion = true } = {}) {
  return {
    cursoId: 1,
    escala: 10,
    aprobacion: 7.0,
    sincronizacion: { id: 12, fecha: '2026-11-09T07:10:00-05:00', estado: finalizacion ? 'COMPLETADA' : 'PARCIAL' },
    unidades: [{ numero: 1, nombre: 'Unidad 1', parcial: 1, actividades: 4, calificadas: 4 }],
    curso: {
      estudiantes: 32,
      actividadesEvaluadas: 16,
      promedio: 7.42,
      mediana: 7.6,
      desviacion: 1.35,
      tasaFinalizacion: finalizacion ? 0.81 : null,
      tendencia: -0.4,
    },
    distribucion: Array.from({ length: 10 }, (_, i) => ({ desde: i, hasta: i + 1, cantidad: i === 7 ? 9 : 1 })),
    grupos: { alto: 6, medio: 18, bajo: 8 },
    estudiantes: [
      {
        id: 5,
        nombre: 'Ana',
        apellido: 'Pérez',
        promedioPonderado: 6.1,
        parcial: false,
        z: -0.98,
        grupo: 'MEDIO',
        notas: { calificadas: 12, ceros: 1, sinEntrega: 2, pendientes: 3 },
        tasaFinalizacion: finalizacion ? 0.75 : null,
        tendencia: -1.8,
      },
    ],
    noDisponibles: finalizacion ? [] : ['tasaFinalizacion'],
  }
}

function renderPanel() {
  return render(
    <MemoryRouter initialEntries={['/cursos/1/panel']}>
      <Routes>
        <Route path="/cursos/:id/panel" element={<Panel />} />
      </Routes>
    </MemoryRouter>,
  )
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('Panel del curso', () => {
  it('muestra el promedio con su escala, la mediana, los grupos y la finalización', async () => {
    obtenerIndicadores.mockResolvedValue(indicadores())
    contarAlertasActivas.mockResolvedValue(4)
    renderPanel()

    const promedio = await screen.findByRole('region', { name: 'Promedio del curso' })
    expect(within(promedio).getByText('7,42')).toBeInTheDocument()
    expect(within(promedio).getByText(/Escala 0 a 10/)).toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'Mediana y desviación' })).getByText('7,60')).toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'Requiere atención' })).getByText('4')).toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'Grupos de rendimiento' })).getByText('18')).toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'Finalización' })).getByText('81 %')).toBeInTheDocument()
    expect(obtenerIndicadores).toHaveBeenCalledWith('1')
  })

  it('distingue nota cero, sin entrega y pendiente en la tabla de estudiantes', async () => {
    obtenerIndicadores.mockResolvedValue(indicadores())
    contarAlertasActivas.mockResolvedValue(0)
    renderPanel()

    const tabla = await screen.findByRole('table', { name: 'Estudiantes' })
    const encabezados = within(tabla).getAllByRole('columnheader').map((th) => th.textContent)
    const fila = within(tabla).getByText('Pérez, Ana').closest('tr')
    const celdas = within(fila).getAllByRole('cell').map((td) => td.textContent)
    const valor = (columna) => celdas[encabezados.indexOf(columna)]

    expect(valor('Ceros')).toBe('1')
    expect(valor('Sin entrega')).toBe('2')
    expect(valor('Pendientes')).toBe('3')
    expect(valor('Calificadas')).toBe('12')
    expect(valor('Tendencia')).toBe('-1,80')
  })

  it('muestra «no disponible» y no 0 cuando la finalización viene en noDisponibles o faltan las alertas', async () => {
    obtenerIndicadores.mockResolvedValue(indicadores({ finalizacion: false }))
    contarAlertasActivas.mockResolvedValue(null)
    renderPanel()

    const finalizacion = await screen.findByRole('region', { name: 'Finalización' })
    expect(within(finalizacion).getByText('no disponible')).toBeInTheDocument()
    expect(within(finalizacion).queryByText('0 %')).not.toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'Requiere atención' })).getByText('no disponible')).toBeInTheDocument()
    const fila = within(screen.getByRole('table', { name: 'Estudiantes' })).getByText('Pérez, Ana').closest('tr')
    expect(within(fila).getAllByText('no disponible').length).toBeGreaterThan(0)
  })

  it('con 409 muestra «Curso sin sincronizar» con enlace a Mis cursos', async () => {
    obtenerIndicadores.mockRejectedValue(
      Object.assign(new Error('HTTP 409'), {
        response: { status: 409, data: { title: 'Curso sin sincronizar', detail: 'El curso todavía no tiene datos de Moodle.' } },
      }),
    )
    contarAlertasActivas.mockResolvedValue(null)
    renderPanel()

    const aviso = await screen.findByRole('alert')
    expect(aviso).toHaveTextContent('Curso sin sincronizar')
    expect(within(aviso).getByRole('link', { name: 'Ir a Mis cursos' })).toHaveAttribute('href', '/cursos')
  })
})
