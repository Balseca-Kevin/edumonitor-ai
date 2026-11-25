import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom'
import Mapa from './Mapa'

vi.mock('../../services/tematicas', () => ({ obtenerMapeo: vi.fn(), obtenerTematicas: vi.fn(), crearTema: vi.fn(), asociarTemas: vi.fn() }))

import { asociarTemas, crearTema, obtenerMapeo, obtenerTematicas } from '../../services/tematicas'

// Formas de GET /api/cursos/{id}/mapeo y GET /api/cursos/{id}/tematicas (docs/contratos-api.md, «Mapeo temático»).
const mapeo = {
  cursoId: 1,
  unidades: [
    { id: 7, numero: 1, nombre: 'Principios fundamentales de las metodologías ágiles', parcial: 1, temas: [ { id: 21, nombre: '1.3 Manifiesto Ágil' }, { id: 30, nombre: 'Historias de usuario' } ] },
    { id: 9, numero: 3, nombre: 'Principales metodologías ágiles del mercado', parcial: 2, temas: [ { id: 40, nombre: '3.2 Scrum' } ] },
  ],
  actividades: [
    {
      id: 101, nombre: 'Tarea 1', categoria: 'Tareas', unidad: 1, fechaLimite: '2026-09-08T23:59:00-05:00', temas: [30],
      criterios: [
        { id: 5, orden: 1, descripcion: 'Redacción de historias de usuario', puntajeMaximo: 2.5, temaId: 30 },
        { id: 6, orden: 2, descripcion: 'Formato y entrega', puntajeMaximo: 2.5, temaId: null },
      ],
    },
    { id: 120, nombre: 'Taller 1', categoria: 'APE', unidad: 3, fechaLimite: null, temas: [], criterios: [] },
  ],
}
const tematicas = {
  cursoId: 1,
  actividadId: null,
  temas: [
    {
      id: 30, nombre: 'Historias de usuario', unidad: { numero: 1, nombre: 'U1' },
      evaluaciones: [ { actividadId: 101, actividad: 'Tarea 1', fuente: 'CRITERIOS', criterios: ['Redacción de historias de usuario'] } ],
      estudiantesConNota: 2, promedio: 6.0, domina: 0.5,
      bajo6: [ { id: 8, nombre: 'Diego', apellido: 'Díaz', nota: 4 } ],
      estudiantes: [
        { id: 8, nombre: 'Diego', apellido: 'Díaz', nota: 4, notas: [ { actividadId: 101, nota: 4 } ] },
        { id: 5, nombre: 'Ana', apellido: 'Acosta', nota: 8, notas: [ { actividadId: 101, nota: 8 } ] },
      ],
      proximas: [ { actividadId: 118, nombre: 'Tarea 9', fechaLimite: '2026-11-28T23:59:00-05:00' } ],
    },
  ],
  sinTemas: [ { actividadId: 120, nombre: 'Taller 1' } ],
}

function renderMapa() {
  return render(
    <MemoryRouter initialEntries={['/cursos/1/mapa']}>
      <Routes>
        <Route path="/cursos/:id" element={<Outlet context={{ version: 0 }} />}>
          <Route path="mapa" element={<Mapa />} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  obtenerMapeo.mockResolvedValue(mapeo)
  obtenerTematicas.mockResolvedValue(tematicas)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('HU-08 · Mapa del curso', () => {
  it('muestra los parciales, las unidades con sus temas y las actividades con los temas que evalúan', async () => {
    renderMapa()

    const u1 = await screen.findByRole('article', { name: 'U1 · Principios fundamentales de las metodologías ágiles' })
    expect(screen.getByText('PARCIAL 1')).toBeInTheDocument()
    expect(screen.getByText('PARCIAL 2')).toBeInTheDocument()
    expect(within(u1).getByRole('checkbox', { name: 'Historias de usuario' })).toBeChecked()
    expect(within(u1).getByRole('checkbox', { name: '1.3 Manifiesto Ágil' })).not.toBeChecked()
    expect(within(u1).getByRole('combobox', { name: 'Tema del criterio Redacción de historias de usuario' })).toHaveValue('30')
    expect(within(u1).getByRole('combobox', { name: 'Tema del criterio Formato y entrega' })).toHaveValue('')
    expect(screen.getByText('1 actividad con notas no tiene temas')).toBeInTheDocument()
  })

  it('el desempeño del tema se abre para ver de qué notas sale y dónde se vuelve a evaluar', async () => {
    renderMapa()

    const cifra = await screen.findByRole('button', { name: 'Domina 50 % · promedio 6,00 · 1 bajo 6,0' })
    fireEvent.click(cifra)

    const tabla = screen.getByRole('table', { name: 'Notas en Historias de usuario' })
    expect(within(tabla).getByRole('rowheader', { name: 'Díaz Diego' }).closest('tr')).toHaveTextContent('Tarea 1: 4,00')
    expect(screen.getByText(/Tarea 1 \(criterio: Redacción de historias de usuario\)/)).toBeInTheDocument()
    expect(screen.getByText(/Se vuelve a evaluar en: Tarea 9/)).toBeInTheDocument()
    expect(screen.getAllByText('Sin actividades asociadas')).toHaveLength(2) // 1.3 Manifiesto Ágil y 3.2 Scrum
  })

  it('guardar los temas de una actividad y de sus criterios llama a la API y recarga el mapa', async () => {
    asociarTemas.mockResolvedValue({ ...mapeo.actividades[0], temas: [21, 30] })
    renderMapa()

    const u1 = await screen.findByRole('article', { name: /U1/ })
    expect(within(u1).queryByRole('button', { name: 'Guardar' })).toBeNull()
    fireEvent.click(within(u1).getByRole('checkbox', { name: '1.3 Manifiesto Ágil' }))
    fireEvent.change(within(u1).getByRole('combobox', { name: 'Tema del criterio Formato y entrega' }), { target: { value: '21' } })
    fireEvent.click(within(u1).getByRole('button', { name: 'Guardar' }))

    await waitFor(() => expect(asociarTemas).toHaveBeenCalledWith('1', 101, [30, 21], [ { id: 5, temaId: 30 }, { id: 6, temaId: 21 } ]))
    await waitFor(() => expect(obtenerMapeo).toHaveBeenCalledTimes(2))
  })

  it('agrega un tema a la unidad y muestra el error de la API si ya existe', async () => {
    crearTema.mockRejectedValueOnce({ response: { status: 400, data: { title: 'Datos inválidos', detail: 'La unidad ya tiene el tema «Historias de usuario».' } } })
    crearTema.mockResolvedValueOnce({ id: 31, nombre: 'Estimación' })
    renderMapa()

    const u1 = await screen.findByRole('article', { name: /U1/ })
    const campo = within(u1).getByLabelText('Nuevo tema de la unidad 1')
    fireEvent.change(campo, { target: { value: 'historias de usuario' } })
    fireEvent.click(within(u1).getByRole('button', { name: 'Agregar' }))
    expect(await within(u1).findByRole('alert')).toHaveTextContent('La unidad ya tiene el tema')

    fireEvent.change(campo, { target: { value: 'Estimación' } })
    fireEvent.click(within(u1).getByRole('button', { name: 'Agregar' }))
    await waitFor(() => expect(crearTema).toHaveBeenLastCalledWith('1', 7, 'Estimación'))
    await waitFor(() => expect(obtenerMapeo).toHaveBeenCalledTimes(2))
  })

  it('un curso sin sincronizar lo dice', async () => {
    obtenerMapeo.mockRejectedValue({ response: { status: 409, data: { title: 'Curso sin sincronizar' } } })
    renderMapa()
    expect(await screen.findByText('Curso sin sincronizar')).toBeInTheDocument()
  })
})
