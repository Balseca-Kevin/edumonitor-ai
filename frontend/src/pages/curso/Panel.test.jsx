import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import Panel from './Panel'

vi.mock('../../services/indicadores', () => ({ obtenerIndicadores: vi.fn() }))
vi.mock('../../services/alertas', () => ({ obtenerAlertas: vi.fn() }))
vi.mock('../../services/libro', async (original) => ({ ...(await original()), obtenerLibro: vi.fn() }))
vi.mock('../../services/notaNecesaria', () => ({
  // Forma de GET /api/cursos/{id}/estudiantes/{eid}/nota-necesaria (docs/contratos-api.md, «Nota necesaria»).
  obtenerNotaNecesaria: vi.fn().mockResolvedValue({
    estado: 'NECESITA', notaNecesaria: 7.67, maximoPosible: 7.7, pendientes: 2, promedioActual: 6.6,
    categorias: [ { categoria: 'Exámenes', peso: 0.4, calificadas: 1, promedio: 6, pendientes: [ { actividadId: 117, nombre: 'Examen Parcial 2', fechaLimite: '2026-11-25T18:59:00-05:00', estado: 'AUN_NO_VENCE' } ] } ],
  }),
}))

import { obtenerIndicadores } from '../../services/indicadores'
import { obtenerAlertas } from '../../services/alertas'
import { obtenerLibro } from '../../services/libro'

// Recharts usa ResizeObserver, que jsdom no trae.
beforeAll(() => {
  globalThis.ResizeObserver ??= class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
})

const unidades = [
  { numero: 1, nombre: 'Principios fundamentales de las metodologías ágiles', parcial: 1, actividades: 4, calificadas: 4 },
  { numero: 2, nombre: 'Filosofía y enfoques ágiles de la actualidad', parcial: 1, actividades: 4, calificadas: 4 },
]

function estudiante(id, nombre, apellido, promedio, notas, extra = {}) {
  return {
    id,
    nombre,
    apellido,
    promedioPonderado: promedio,
    parcial: false,
    z: null,
    grupo: null,
    notas: { calificadas: 0, ceros: 0, sinEntrega: 0, pendientes: 0, aunNoVence: 0, ...notas },
    tasaFinalizacion: null,
    tendencia: null,
    categorias: [],
    ...extra,
  }
}

// Respuesta con la forma de GET /api/cursos/{id}/indicadores (docs/contratos-api.md, con «Aún no vence y desglose por categoría»).
function indicadores() {
  return {
    cursoId: 1,
    escala: 10,
    aprobacion: 7.0,
    tramo: 'todo',
    sincronizacion: { id: 12, fecha: '2026-11-21T07:10:00-05:00', estado: 'COMPLETADA' },
    unidades,
    curso: { estudiantes: 3, actividadesEvaluadas: 16, promedio: 7.42, mediana: 7.6, desviacion: 1.35, tasaFinalizacion: 0.81, tendencia: -0.4 },
    distribucion: Array.from({ length: 10 }, (_, i) => ({ desde: i, hasta: i + 1, cantidad: { 5: 1, 6: 1, 8: 1 }[i] ?? 0 })),
    grupos: { alto: 1, medio: 1, bajo: 1 },
    estudiantes: [
      estudiante(5, 'Ana', 'Pérez', 6.1, { calificadas: 12, ceros: 1, sinEntrega: 2 }, {
        tendencia: -1.8,
        categorias: [
          { categoria: 'Exámenes', peso: 0.4, promedio: 5.4, notas: 1 },
          { categoria: 'Tareas', peso: 0.3, promedio: 6.8, notas: 9 },
          { categoria: 'APE', peso: 0.3, promedio: null, notas: 0 },
        ],
      }),
      estudiante(6, 'Luis', 'Mora', 8.75, { calificadas: 14 }),
      estudiante(7, 'Elena', 'Espinoza', 5.2, { calificadas: 10, sinEntrega: 1 }),
    ],
    noDisponibles: [],
  }
}

const alertaAna = { id: 31, regla: 'R4', titulo: 'Requiere atención', estudiante: { id: 5, nombre: 'Ana', apellido: 'Pérez' }, estado: 'ACTIVA', evidencia: {} }

function Ubicacion() {
  const { pathname, search } = useLocation()
  return <p data-testid="ruta">{`${pathname}${search}`}</p>
}

function renderPanel(ruta = '/cursos/1/panel') {
  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <Routes>
        <Route path="/cursos/:id/panel" element={<Panel />} />
        <Route path="*" element={<Ubicacion />} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  obtenerIndicadores.mockResolvedValue(indicadores())
  obtenerAlertas.mockResolvedValue([alertaAna, { ...alertaAna, id: 32, regla: 'R2' }])
})

afterEach(() => cleanup())

describe('R.4 · Panel del curso', () => {
  it('muestra los 4 indicadores con las cifras de la API y «Requieren atención» cuenta estudiantes', async () => {
    renderPanel()

    const cifras = await screen.findByRole('region', { name: 'Cifras del curso' })
    await waitFor(() => expect(within(cifras).getByText('7,42')).toBeInTheDocument())
    expect(within(cifras).getByText('7,60')).toBeInTheDocument()
    expect(within(cifras).getByText('en Todo el ciclo')).toBeInTheDocument()
    expect(within(cifras).getByText('36')).toBeInTheDocument() // 12 + 14 + 10 notas calificadas
    expect(within(cifras).getByRole('button', { name: /Requieren atención\s*1/ })).toBeInTheDocument() // Ana, con dos alertas, cuenta una vez
    expect(obtenerIndicadores).toHaveBeenCalledWith('1', 'todo')
    expect(obtenerAlertas).toHaveBeenCalledWith('1', 'ACTIVA', 'todo')
  })

  it('«Requieren atención» lleva a las alertas del mismo tramo', async () => {
    renderPanel('/cursos/1/panel?tramo=u2')

    fireEvent.click(await screen.findByRole('button', { name: /Requieren atención/ }))

    expect(screen.getByTestId('ruta')).toHaveTextContent('/cursos/1/alertas?tramo=u2')
    expect(obtenerIndicadores).toHaveBeenCalledWith('1', 'u2')
  })

  it('la tabla dice quién requiere atención y quién está al día, y el histograma la filtra por tramo de 1 punto', async () => {
    renderPanel()

    const tabla = await screen.findByRole('table', { name: 'Estudiantes' })
    const filaAna = within(tabla).getByRole('row', { name: /Pérez, Ana/ })
    expect(within(filaAna).getByText('Requiere atención')).toBeInTheDocument()
    expect(within(within(tabla).getByRole('row', { name: /Mora, Luis/ })).getByText('Al día')).toBeInTheDocument()
    expect(within(filaAna).getByText('6,10')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: '1 estudiantes con promedio de 8 a 9' }))

    expect(within(screen.getByRole('table', { name: 'Estudiantes' })).getAllByRole('row')).toHaveLength(2) // encabezado + Luis
    expect(screen.getByText('1 con promedio de 8 a 9')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Quitar filtro' }))
    expect(within(screen.getByRole('table', { name: 'Estudiantes' })).getAllByRole('row')).toHaveLength(4)
  })

  it('un clic en la fila abre la ficha con el desglose 40/30/30, la evolución y lo que no entregó', async () => {
    obtenerLibro.mockResolvedValue({
      parciales: [{ numero: 1, unidades: [{ numero: 1, nombre: 'U1', actividades: [
        { id: 101, nombre: 'Tarea 1', categoria: 'Tareas', fechaLimite: '2026-09-08T23:59:00-05:00' },
        { id: 102, nombre: 'Tarea 5', categoria: 'Tareas', fechaLimite: '2026-10-14T23:59:00-05:00' },
        { id: 103, nombre: 'Tarea 9', categoria: 'Tareas', fechaLimite: '2026-11-28T23:59:00-05:00' },
      ] }] }],
      estudiantes: [{ id: 5, nombre: 'Ana', apellido: 'Pérez', notas: [
        { actividadId: 101, nota: 6.25, estado: 'CALIFICADO' },
        { actividadId: 102, nota: null, estado: 'SIN_ENTREGA' },
        { actividadId: 103, nota: null, estado: 'AUN_NO_VENCE' },
      ] }],
    })
    renderPanel()

    fireEvent.click(await screen.findByRole('row', { name: /Pérez, Ana/ }))

    const ficha = await screen.findByRole('dialog', { name: 'Ficha de Pérez, Ana' })
    expect(within(ficha).getByText('Requiere atención')).toBeInTheDocument()
    expect(within(ficha).getByText('promedio ponderado en Todo el ciclo')).toBeInTheDocument()
    expect(within(ficha).getByText('Exámenes · 40 %')).toBeInTheDocument()
    expect(within(ficha).getByText('5,40')).toBeInTheDocument()
    expect(within(ficha).getByText('sin notas')).toBeInTheDocument() // APE sin notas: no es 0
    // TA-10.A1 · «Para llegar a 7,0» con la nota que calcula el backend y la leyenda obligatoria.
    const paraLlegar = within(ficha).getByRole('region', { name: 'Para llegar a 7,0' })
    expect(await within(paraLlegar).findByText('Necesita 7,67 en cada una de sus 2 actividades pendientes. Con 10 en todas llegaría a 7,70.')).toBeInTheDocument()
    expect(within(paraLlegar).getByText('cálculo con las notas actuales')).toBeInTheDocument()
    expect(within(paraLlegar).getByText(/Examen Parcial 2 · Exámenes · vence 25\/11\/2026/)).toBeInTheDocument()
    expect(within(ficha).queryByText(/nota final/i)).toBeNull()
    expect(await within(ficha).findByRole('img', { name: 'Tarea 1: 6,25' })).toBeInTheDocument()
    expect(within(ficha).getByRole('img', { name: 'Tarea 5: sin entrega' })).toBeInTheDocument()
    expect(within(ficha).getByRole('img', { name: 'Tarea 9: aún no vence' })).toBeInTheDocument()
    const sinEntregar = within(ficha).getByRole('region', { name: 'Sin entregar' })
    expect(within(sinEntregar).getByText('Tarea 5')).toBeInTheDocument()
    expect(within(sinEntregar).queryByText('Tarea 9')).toBeNull() // aún no vence: no es una falta
    expect(obtenerLibro).toHaveBeenCalledWith('1', { tramo: 'todo' })

    fireEvent.click(within(ficha).getByRole('button', { name: 'Preguntar al asistente por este estudiante' }))
    expect(screen.getByTestId('ruta')).toHaveTextContent('/cursos/1/asistente?estudiante=5')
  })

  it('«Armar plan de refuerzo» lleva el estudiante y el tramo, y Escape cierra la ficha', async () => {
    obtenerLibro.mockResolvedValue({ parciales: [], estudiantes: [] })
    renderPanel('/cursos/1/panel?tramo=p1')

    fireEvent.click(await screen.findByRole('row', { name: /Mora, Luis/ }))
    expect(await screen.findByRole('dialog', { name: 'Ficha de Mora, Luis' })).toBeInTheDocument()
    fireEvent.keyDown(window, { key: 'Escape' })
    expect(screen.queryByRole('dialog')).toBeNull()

    fireEvent.click(screen.getByRole('row', { name: /Mora, Luis/ }))
    fireEvent.click(await screen.findByRole('button', { name: 'Armar plan de refuerzo' }))
    expect(screen.getByTestId('ruta')).toHaveTextContent('/cursos/1/refuerzos?estudiante=6&tramo=p1')
  })

  it('sin alertas disponibles dice «no disponible», nunca 0, y un promedio null tampoco es 0', async () => {
    const datos = indicadores()
    datos.curso.mediana = null
    obtenerIndicadores.mockResolvedValue(datos)
    obtenerAlertas.mockRejectedValue(new Error('caída'))
    renderPanel()

    const cifras = await screen.findByRole('region', { name: 'Cifras del curso' })
    await waitFor(() => expect(within(cifras).getAllByText('no disponible')).toHaveLength(2))
    expect(within(cifras).getByText('Las alertas no están disponibles.')).toBeInTheDocument()
  })

  it('HU-16 · «Informe del curso» abre el asistente con la pregunta del resumen ya escrita', async () => {
    renderPanel()

    fireEvent.click(await screen.findByRole('button', { name: 'Informe del curso' }))
    expect(screen.getByTestId('ruta')).toHaveTextContent('/cursos/1/asistente?pregunta=Resume+el+estado+del+curso')
  })

  it('un curso sin sincronizar lo dice con un enlace a Mis cursos', async () => {
    obtenerIndicadores.mockRejectedValue({ response: { status: 409, data: { title: 'Curso sin sincronizar', detail: 'El curso todavía no tiene datos de Moodle.' } } })
    renderPanel()

    expect(await screen.findByText('Curso sin sincronizar')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Ir a Mis cursos' })).toHaveAttribute('href', '/cursos')
  })
})
