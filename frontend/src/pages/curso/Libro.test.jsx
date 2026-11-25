import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom'
import Libro from './Libro'

vi.mock('../../services/libro', async (original) => ({ ...(await original()), obtenerLibro: vi.fn() }))
vi.mock('../../services/indicadores', () => ({ obtenerIndicadores: vi.fn() }))
vi.mock('../../services/cursos', () => ({ obtenerCambiosSincronizacion: vi.fn() }))
vi.mock('../../services/notaNecesaria', () => ({
  // Forma de GET /api/cursos/{id}/estudiantes/{eid}/nota-necesaria (docs/contratos-api.md, «Nota necesaria»).
  obtenerNotaNecesaria: vi.fn().mockResolvedValue({
    estado: 'NECESITA', notaNecesaria: 7.67, maximoPosible: 7.7, pendientes: 2, promedioActual: 6.6,
    categorias: [ { categoria: 'Exámenes', peso: 0.4, calificadas: 1, promedio: 6, pendientes: [ { actividadId: 117, nombre: 'Examen Parcial 2', fechaLimite: '2026-11-25T18:59:00-05:00', estado: 'AUN_NO_VENCE' } ] } ],
  }),
}))

import { obtenerLibro } from '../../services/libro'
import { obtenerIndicadores } from '../../services/indicadores'
import { obtenerCambiosSincronizacion } from '../../services/cursos'

beforeAll(() => {
  globalThis.ResizeObserver ??= class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
})

const curso = { id: 1, nombreCorto: 'AGIL-4B', ultimaSincronizacion: { id: 13, fecha: '2026-11-22T09:20:07-05:00', estado: 'COMPLETADA' } }
// Semana de cada actividad: la da GET /api/cursos/{id}/periodo; el libro no calcula fechas.
const periodo = { actividades: [ { id: 101, semana: 2 }, { id: 102, semana: 3 }, { id: 103, semana: 7 }, { id: 104, semana: 13 } ] }

// Forma de GET /api/cursos/{id}/libro (docs/contratos-api.md, «Libro de calificaciones» y «Aún no vence»).
const libro = {
  cursoId: 1,
  escala: 10,
  aprobacion: 7.0,
  sincronizacion: { id: 13, fecha: '2026-11-22T09:20:07-05:00', estado: 'COMPLETADA', origen: 'http://localhost:8080' },
  parciales: [
    {
      numero: 1,
      unidades: [
        {
          numero: 1,
          nombre: 'Principios fundamentales de las metodologías ágiles',
          actividades: [
            { id: 101, nombre: 'Tarea 1', categoria: 'Tareas', fechaLimite: '2026-09-08T23:59:00-05:00' },
            { id: 102, nombre: 'APE 1', categoria: 'APE', fechaLimite: '2026-09-15T23:59:00-05:00' },
          ],
        },
      ],
    },
    {
      numero: 2,
      unidades: [
        {
          numero: 4,
          nombre: 'Herramientas ágiles del mercado',
          actividades: [
            { id: 103, nombre: 'Examen Parcial 1', categoria: 'Exámenes', fechaLimite: '2026-10-15T18:59:00-05:00' },
            { id: 104, nombre: 'Tarea 9', categoria: 'Tareas', fechaLimite: '2026-11-28T23:59:00-05:00' },
          ],
        },
      ],
    },
  ],
  estudiantes: [
    {
      id: 5,
      nombre: 'Ana',
      apellido: 'Pérez',
      promedioPonderado: 6.1,
      parcial: false,
      notas: [
        { actividadId: 101, nota: 0, estado: 'CALIFICADO' },
        { actividadId: 102, nota: null, estado: 'SIN_ENTREGA' },
        { actividadId: 103, nota: 8.5, estado: 'CALIFICADO' },
        { actividadId: 104, nota: null, estado: 'AUN_NO_VENCE' },
      ],
    },
    {
      id: 6,
      nombre: 'Luis',
      apellido: 'Mora',
      promedioPonderado: null,
      parcial: true,
      notas: [
        { actividadId: 101, nota: null, estado: 'PENDIENTE' },
        { actividadId: 102, nota: null, estado: null },
        { actividadId: 103, nota: 7, estado: 'CALIFICADO' },
        { actividadId: 104, nota: null, estado: 'AUN_NO_VENCE' },
      ],
    },
  ],
  noDisponibles: [],
}

// Forma de GET /api/cursos/{id}/sync/{sid}/cambios (TA-21.A1).
const cambios = {
  sincronizacionId: 13,
  anteriorId: 12,
  conteos: { total: 1, notasNuevas: 1, notasCambiadas: 0, entregasNuevas: 0, otros: 0 },
  celdas: [
    {
      actividadId: 103,
      actividad: 'Examen Parcial 1',
      estudiante: { id: 5, nombre: 'Ana', apellido: 'Pérez' },
      tipo: 'NOTA_NUEVA',
      antes: { nota: null, estado: 'PENDIENTE' },
      ahora: { nota: 8.5, estado: 'CALIFICADO' },
    },
  ],
  noDisponibles: [],
}

function Contexto({ valor }) {
  return <Outlet context={valor} />
}

function renderLibro(ruta = '/cursos/1/libro', contexto = { curso, version: 0, periodo }) {
  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <Routes>
        <Route path="/cursos/:id" element={<Contexto valor={contexto} />}>
          <Route path="libro" element={<Libro />} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  obtenerLibro.mockResolvedValue(libro)
  obtenerCambiosSincronizacion.mockResolvedValue(cambios)
  obtenerIndicadores.mockResolvedValue({
    estudiantes: [{ id: 5, nombre: 'Ana', apellido: 'Pérez', promedioPonderado: 6.1, notas: { calificadas: 2, ceros: 1, sinEntrega: 1, pendientes: 0, aunNoVence: 1 }, categorias: [] }],
  })
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('HU-21 · Libro de calificaciones', () => {
  it('muestra el encabezado de dos filas con la unidad y su parcial, y cada actividad con su categoría y su semana', async () => {
    renderLibro()

    const tabla = await screen.findByRole('table', { name: 'Libro de calificaciones · Todo el ciclo' })
    expect(within(tabla).getByRole('columnheader', { name: 'P1 · U1 · Principios fundamentales de las metodologías ágiles' })).toHaveAttribute('colspan', '2')
    expect(within(tabla).getByRole('columnheader', { name: 'P2 · U4 · Herramientas ágiles del mercado' })).toBeInTheDocument()
    expect(within(tabla).getByRole('columnheader', { name: /Tarea 1\s*Tareas · S2/ })).toBeInTheDocument()
    expect(within(tabla).getByRole('columnheader', { name: /Examen Parcial 1\s*Exámenes · S7/ })).toBeInTheDocument()
    expect(within(tabla).getByRole('columnheader', { name: 'Promedio 40/30/30' })).toBeInTheDocument()
    expect(within(tabla).getByRole('link', { name: 'Tarea 1' })).toHaveAttribute('href', '/cursos/1/actividades/101') // HU-22: abre el panel de actividad
    expect(obtenerLibro).toHaveBeenCalledWith('1', { tramo: 'todo', busqueda: undefined, orden: undefined })
  })

  it('distingue cero, sin entrega, por calificar, aún no vence y el dato que falta, y muestra el promedio', async () => {
    renderLibro()

    const ana = (await screen.findByRole('rowheader', { name: 'Pérez Ana' })).closest('tr')
    const celdasAna = within(ana).getAllByRole('cell').map((c) => c.textContent)
    expect(celdasAna).toEqual(['0,00', 'Sin entrega', expect.stringContaining('8,50'), 'Aún no vence', '6,10'])
    expect(within(ana).getByText('0,00')).toHaveClass('nota--bajo')
    expect(within(ana).getByText('Sin entrega')).toHaveClass('nota--sin-entrega')

    const luis = screen.getByRole('rowheader', { name: 'Mora Luis' }).closest('tr')
    expect(within(luis).getByText('Por calificar')).toHaveClass('nota--pendiente')
    expect(within(luis).getAllByText('no disponible')).toHaveLength(2) // celda sin dato y promedio sin notas, nunca 0
  })

  it('TA-23.A2 · una cifra del asistente abre el libro con la fila y la celda señaladas', async () => {
    renderLibro('/cursos/1/libro?estudiante=5&actividad=103')

    const ana = (await screen.findByRole('rowheader', { name: 'Pérez Ana' })).closest('tr')
    expect(ana).toHaveAttribute('aria-current', 'true')
    expect(within(ana).getByText(/8,50/)).toHaveClass('nota--senalada')
    expect(within(ana).getByText('0,00')).not.toHaveClass('nota--senalada')
  })

  it('marca con el anillo las celdas que cambiaron en la última sincronización y lo dice en la leyenda', async () => {
    renderLibro()

    expect(await screen.findByText('1 celda cambió en la última sincronización')).toBeInTheDocument()
    const ana = screen.getByRole('rowheader', { name: 'Pérez Ana' }).closest('tr')
    const nueva = within(ana).getByText('8,50', { exact: false })
    expect(nueva).toHaveClass('nota--nueva')
    expect(nueva).toHaveAttribute('title', 'Nuevo en la última sincronización: nota nueva')
    expect(within(ana).getByText('0,00')).not.toHaveClass('nota--nueva')
    expect(screen.getByLabelText('Leyenda del libro')).toHaveTextContent('Nuevo en la última sincronización')
    expect(obtenerCambiosSincronizacion).toHaveBeenCalledWith('1', 13)
  })

  it('el tramo de la barra llega al backend y la búsqueda y el orden se piden a la API', async () => {
    renderLibro('/cursos/1/libro?tramo=u4')
    await screen.findByRole('table')
    expect(obtenerLibro).toHaveBeenCalledWith('1', { tramo: 'u4', busqueda: undefined, orden: undefined })

    fireEvent.change(screen.getByLabelText('Ordenar por'), { target: { value: 'promedio' } })
    await waitFor(() => expect(obtenerLibro).toHaveBeenLastCalledWith('1', { tramo: 'u4', busqueda: undefined, orden: 'promedio' }))

    obtenerLibro.mockResolvedValue({ ...libro, estudiantes: [] })
    fireEvent.change(screen.getByLabelText('Buscar estudiante'), { target: { value: 'zoila' } })
    expect(await screen.findByText('Ningún estudiante coincide con «zoila».')).toBeInTheDocument()
    expect(obtenerLibro).toHaveBeenLastCalledWith('1', { tramo: 'u4', busqueda: 'zoila', orden: 'promedio' })
  })

  it('un clic en la fila abre la ficha del estudiante con sus cifras del backend', async () => {
    renderLibro()

    fireEvent.click(await screen.findByRole('button', { name: 'Pérez Ana' }))

    const ficha = await screen.findByRole('dialog', { name: 'Ficha de Pérez, Ana' })
    // La ficha muestra lo que no entregó con las mismas celdas del libro.
    expect(within(ficha).getByText(/APE 1/)).toBeInTheDocument()
    expect(obtenerIndicadores).toHaveBeenCalledTimes(1)
    expect(obtenerIndicadores).toHaveBeenCalledWith('1', 'todo')
  })

  it('avisa cuando las entregas no se pudieron leer y cuando el curso no está sincronizado', async () => {
    obtenerLibro.mockResolvedValue({ ...libro, noDisponibles: ['estadosEntrega'] })
    const { unmount } = renderLibro()
    expect(await screen.findByText('Entregas sin actualizar')).toBeInTheDocument()
    unmount()

    obtenerLibro.mockRejectedValue(Object.assign(new Error('409'), { response: { status: 409, data: { title: 'Curso sin sincronizar' } } }))
    renderLibro()
    expect(await screen.findByText('Curso sin sincronizar')).toBeInTheDocument()
  })

  it('sin sincronización no pide cambios y sin cambios no marca celdas', async () => {
    renderLibro('/cursos/1/libro', { curso: { ...curso, ultimaSincronizacion: null }, version: 0, periodo: null })

    await screen.findByRole('table')
    expect(obtenerCambiosSincronizacion).not.toHaveBeenCalled()
    expect(document.querySelectorAll('.libro-tabla .nota--nueva')).toHaveLength(0)
    expect(screen.getByRole('columnheader', { name: /^Tarea 1\s*Tareas$/ })).toBeInTheDocument() // sin período, sin semana
  })
})
