import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import Panel from './Panel'
import { frenteAlCurso, ultimasNotas } from '../../components/lenguaje'

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
  it('muestra las cuatro cifras en lenguaje docente: promedio, la mitad del curso, actividades completadas y requiere atención', async () => {
    obtenerIndicadores.mockResolvedValue(indicadores())
    contarAlertasActivas.mockResolvedValue(4)
    renderPanel()

    const promedio = await screen.findByRole('region', { name: 'Promedio del curso' })
    expect(within(promedio).getByText('7,42')).toBeInTheDocument()
    expect(within(promedio).getByText(/sobre 10 · aprobación 7,0/)).toBeInTheDocument()
    const mitad = screen.getByRole('region', { name: 'La mitad del curso tiene más de' })
    expect(within(mitad).getByText('7,60')).toBeInTheDocument()
    expect(within(mitad).getByText('las notas varían unos 1,4 puntos alrededor del promedio')).toBeInTheDocument()
    const atencion = screen.getByRole('region', { name: 'Requiere atención' })
    expect(within(atencion).getByText('4')).toBeInTheDocument()
    expect(within(atencion).getByRole('link', { name: 'ver alertas →' })).toHaveAttribute('href', '/cursos/1/alertas')
    expect(within(screen.getByRole('region', { name: 'Actividades completadas' })).getByText('81 %')).toBeInTheDocument()
    const reparto = screen.getByRole('region', { name: 'Cómo se reparte el curso' })
    expect(within(reparto).getByText('8,5 o más')).toBeInTheDocument()
    expect(within(reparto).getByText('18')).toBeInTheDocument()
    expect(within(reparto).getByText('Menos de 6,0')).toBeInTheDocument()
    expect(obtenerIndicadores).toHaveBeenCalledWith('1')
  })

  it('no muestra términos estadísticos ni códigos internos', async () => {
    obtenerIndicadores.mockResolvedValue(indicadores())
    contarAlertasActivas.mockResolvedValue(4)
    renderPanel()

    await screen.findByRole('table', { name: 'Estudiantes' })
    for (const termino of [/desviación/i, /mediana/i, /z/, /tendencia/i, /MEDIO/, /R4/]) {
      expect(screen.queryByText(termino)).not.toBeInTheDocument()
    }
  })

  it('distingue nota cero, sin entregar y por calificar en la tabla de estudiantes', async () => {
    obtenerIndicadores.mockResolvedValue(indicadores())
    contarAlertasActivas.mockResolvedValue(0)
    renderPanel()

    const tabla = await screen.findByRole('table', { name: 'Estudiantes' })
    const encabezados = within(tabla).getAllByRole('columnheader').map((th) => th.textContent)
    const fila = within(tabla).getByText('Pérez, Ana').closest('tr')
    const celdas = within(fila).getAllByRole('cell').map((td) => td.textContent)
    const valor = (columna) => celdas[encabezados.indexOf(columna)]

    expect(valor('Con cero')).toBe('1')
    expect(valor('Sin entregar')).toBe('2')
    expect(valor('Por calificar')).toBe('3')
    expect(valor('Notas registradas')).toBe('12')
    expect(valor('Frente al curso')).toBe('1,3 bajo el promedio')
    expect(valor('Últimas notas')).toBe('↓ bajando 1,8')
  })

  it('muestra «no disponible» y no 0 cuando la finalización viene en noDisponibles o faltan las alertas', async () => {
    obtenerIndicadores.mockResolvedValue(indicadores({ finalizacion: false }))
    contarAlertasActivas.mockResolvedValue(null)
    renderPanel()

    const finalizacion = await screen.findByRole('region', { name: 'Actividades completadas' })
    expect(within(finalizacion).getByText('no disponible')).toBeInTheDocument()
    expect(within(finalizacion).queryByText('0 %')).not.toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'Requiere atención' })).getByText('no disponible')).toBeInTheDocument()
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

describe('Lenguaje docente del panel', () => {
  it('«Frente al curso» dice cuánto está sobre o bajo el promedio del curso', () => {
    expect(frenteAlCurso(5.92, 7.42)).toBe('1,5 bajo el promedio')
    expect(frenteAlCurso(7.85, 7.42)).toBe('0,4 sobre el promedio')
    expect(frenteAlCurso(7.42, 7.42)).toBe('en el promedio')
    expect(frenteAlCurso(null, 7.42)).toBe('no disponible')
  })

  it('«Últimas notas» dice si baja, sube o se mantiene', () => {
    expect(ultimasNotas(-1.8)).toBe('↓ bajando 1,8')
    expect(ultimasNotas(0.2)).toBe('↑ subiendo 0,2')
    expect(ultimasNotas(0.01)).toBe('→ se mantiene')
    expect(ultimasNotas(null)).toBe('aún sin datos')
  })
})
