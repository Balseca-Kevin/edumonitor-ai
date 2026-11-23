import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom'
import Asistente from './Asistente'

vi.mock('../../services/agente', () => ({ preguntarAlAsistente: vi.fn(), listarConversaciones: vi.fn(), obtenerConversacion: vi.fn() }))
vi.mock('../../services/indicadores', () => ({ obtenerIndicadores: vi.fn() }))
vi.mock('../../services/alertas', () => ({ obtenerAlertas: vi.fn() }))
vi.mock('../../services/refuerzos', async (original) => ({
  ...(await original()),
  listarPlanes: vi.fn(),
  obtenerPlan: vi.fn(),
  cambiarPlan: vi.fn(),
  descargarDocumento: vi.fn(),
}))

import { listarConversaciones, preguntarAlAsistente } from '../../services/agente'
import { obtenerIndicadores } from '../../services/indicadores'
import { obtenerAlertas } from '../../services/alertas'
import { cambiarPlan, descargarDocumento, listarPlanes, obtenerPlan } from '../../services/refuerzos'

beforeAll(() => {
  globalThis.ResizeObserver ??= class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
})

// Forma del plan de GET /api/cursos/{id}/refuerzos/{rid} (docs/contratos-api.md, «Plan de refuerzo»).
const base = {
  id: 9, cursoId: 1, creado: '2026-11-23T10:00:00-05:00', actividad: { id: 103, nombre: 'Tarea 3' },
  temas: [ { id: 30, nombre: 'Historias de usuario' } ], estudiantes: [ { id: 8, nombre: 'Diego', apellido: 'Díaz' } ],
  conversacionId: null, texto: null, aprobadoEn: null, resultados: [],
}
const enviado = { ...base, estado: 'ENVIADO' }
const borrador = { ...base, estado: 'BORRADOR', conversacionId: 41, texto: 'Diego Díaz: repasa la redacción de historias de usuario.' }
const aprobado = { ...borrador, estado: 'APROBADO', aprobadoEn: '2026-11-23T11:00:00-05:00' }

function renderAsistente(ruta = '/cursos/1/asistente?refuerzo=9') {
  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <Routes>
        <Route path="/cursos/:id" element={<Outlet context={{ curso: { id: 1, nombreCorto: 'AGIL-4B' }, version: 0, periodo: null }} />}>
          <Route path="asistente" element={<Asistente />} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  sessionStorage.setItem('edumonitor_usuario', JSON.stringify({ id: 2, usuario: 'doc01', nombre: 'Docente Uno', rol: 'DOCENTE' }))
  listarConversaciones.mockResolvedValue([])
  obtenerIndicadores.mockResolvedValue({ unidades: [], estudiantes: [] })
  obtenerAlertas.mockResolvedValue([])
  listarPlanes.mockResolvedValue([enviado])
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  sessionStorage.clear()
})

describe('TA-15.A4 · El plan de refuerzo como foco del asistente', () => {
  it('un plan recién enviado se redacta solo, en su conversación, y el borrador queda editable', async () => {
    obtenerPlan.mockResolvedValueOnce(enviado).mockResolvedValueOnce(borrador)
    preguntarAlAsistente.mockResolvedValue({
      conversacionId: 41, refuerzoId: 9, respuesta: borrador.texto, herramientas: [ { nombre: 'obtener_plan_refuerzo', parametros: { cursoId: 1, refuerzoId: 9 }, estado: 'OK', duracionMs: 90 } ],
      modelo: 'qwen2.5:7b', duracionMs: 9000,
    })
    renderAsistente()

    await waitFor(() => expect(preguntarAlAsistente).toHaveBeenCalledWith('1', '', { refuerzoId: 9 }))
    expect(preguntarAlAsistente).toHaveBeenCalledTimes(1)
    const plan = await screen.findByRole('region', { name: 'Plan de refuerzo en el asistente' })
    expect(await within(plan).findByRole('textbox')).toHaveValue(borrador.texto)
    expect(within(plan).getByText('Borrador por aprobar')).toBeInTheDocument()
    expect(screen.getByText(/Sabe: .*plan de refuerzo de Tarea 3/)).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: 'Planes de refuerzo' })).toHaveTextContent('Refuerzo de Tarea 3: Historias de usuario')
  })

  it('el docente edita y aprueba el borrador; aprobado se descarga en PDF y DOCX', async () => {
    obtenerPlan.mockResolvedValue(borrador)
    cambiarPlan.mockResolvedValue({ ...aprobado, texto: 'Diego: repasa historias.' })
    descargarDocumento.mockResolvedValue({ blob: new Blob(['%PDF']), nombre: 'plan-refuerzo-AGIL-4B-9.pdf' })
    URL.createObjectURL = vi.fn(() => 'blob:plan')
    URL.revokeObjectURL = vi.fn()
    renderAsistente()

    const plan = await screen.findByRole('region', { name: 'Plan de refuerzo en el asistente' })
    expect(preguntarAlAsistente).not.toHaveBeenCalled() // ya tenía borrador
    fireEvent.change(within(plan).getByRole('textbox'), { target: { value: 'Diego: repasa historias.' } })
    fireEvent.click(within(plan).getByRole('button', { name: 'Aprobar borrador' }))

    await waitFor(() => expect(cambiarPlan).toHaveBeenCalledWith('1', 9, { texto: 'Diego: repasa historias.', aprobar: true }))
    const aprobada = await screen.findByRole('region', { name: 'Plan de refuerzo en el asistente' })
    expect(await within(aprobada).findByText('Diego: repasa historias.')).toBeInTheDocument()
    fireEvent.click(within(aprobada).getByRole('button', { name: 'Descargar PDF' }))
    await waitFor(() => expect(descargarDocumento).toHaveBeenCalledWith('1', 9, 'pdf'))
    fireEvent.click(within(aprobada).getByRole('button', { name: 'Descargar DOCX' }))
    await waitFor(() => expect(descargarDocumento).toHaveBeenCalledWith('1', 9, 'docx'))
    expect(URL.createObjectURL).toHaveBeenCalled()
  })

  it('si el asistente no responde lo dice y deja pedir el borrador otra vez', async () => {
    obtenerPlan.mockResolvedValue(enviado)
    preguntarAlAsistente.mockRejectedValue({ response: { status: 503, data: { title: 'Asistente no disponible' } } })
    renderAsistente()

    expect(await screen.findByText(/no está disponible/i)).toBeInTheDocument()
    const plan = screen.getByRole('region', { name: 'Plan de refuerzo en el asistente' })
    expect(within(plan).getByRole('button', { name: 'Redactar el borrador' })).toBeInTheDocument()
  })
})
