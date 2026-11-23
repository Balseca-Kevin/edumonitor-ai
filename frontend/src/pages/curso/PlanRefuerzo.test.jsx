import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Outlet, Route, Routes, useLocation } from 'react-router-dom'
import PlanRefuerzo from './PlanRefuerzo'

vi.mock('../../services/tematicas', () => ({ obtenerMapeo: vi.fn(), obtenerTematicas: vi.fn() }))
vi.mock('../../services/refuerzos', async (original) => ({ ...(await original()), crearPlan: vi.fn(), listarPlanes: vi.fn() }))
vi.mock('../../services/notaNecesaria', () => ({ obtenerNotaNecesaria: vi.fn() }))

import { obtenerMapeo, obtenerTematicas } from '../../services/tematicas'
import { crearPlan, listarPlanes } from '../../services/refuerzos'
import { obtenerNotaNecesaria } from '../../services/notaNecesaria'

// Formas de GET /api/cursos/{id}/mapeo, /tematicas, /refuerzos y /nota-necesaria (docs/contratos-api.md).
const mapeo = {
  cursoId: 1,
  unidades: [],
  actividades: [
    { id: 103, nombre: 'Tarea 3', categoria: 'Tareas', unidad: 1, fechaLimite: null, temas: [30, 31], criterios: [] },
    { id: 104, nombre: 'Tarea 4', categoria: 'Tareas', unidad: 1, fechaLimite: null, temas: [], criterios: [] },
  ],
}
const tema = (id, nombre, domina, estudiantes, proximas = []) => ({
  id, nombre, unidad: { numero: 1, nombre: 'U1' }, evaluaciones: [], estudiantesConNota: estudiantes.length, promedio: 6, domina,
  bajo6: estudiantes.filter((e) => e.nota < 6), estudiantes, proximas,
})
const tematicas = {
  cursoId: 1,
  actividadId: 103,
  temas: [
    tema(30, 'Historias de usuario', 0.4, [
      { id: 8, nombre: 'Diego', apellido: 'Díaz', nota: 4, notas: [] },
      { id: 5, nombre: 'Ana', apellido: 'Acosta', nota: 9, notas: [] },
      { id: 12, nombre: 'Luis', apellido: 'Mora', nota: 5.5, notas: [] },
    ], [ { actividadId: 118, nombre: 'Tarea 9', fechaLimite: '2026-11-28T23:59:00-05:00' } ]),
    tema(31, 'Estimación', 0.9, [ { id: 5, nombre: 'Ana', apellido: 'Acosta', nota: 10, notas: [] } ]),
  ],
  sinTemas: [],
}
const planAprobado = {
  id: 3, cursoId: 1, creado: '2026-11-20T10:00:00-05:00', estado: 'APROBADO',
  actividad: { id: 103, nombre: 'Tarea 3' }, temas: [ { id: 30, nombre: 'Historias de usuario' } ],
  estudiantes: [ { id: 8, nombre: 'Diego', apellido: 'Díaz' } ], conversacionId: 40, texto: 'Diego: repasa…', aprobadoEn: '2026-11-20T11:00:00-05:00',
  resultados: [ { estudianteId: 8, estudiante: 'Diego Díaz', temaId: 30, tema: 'Historias de usuario', antes: 5.2, ahora: 7.1, estado: 'SUBIO' } ],
}

function Ubicacion() {
  const { pathname, search } = useLocation()
  return <p data-testid="ruta">{`${pathname}${search}`}</p>
}

function renderPlan(ruta = '/cursos/1/refuerzos') {
  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <Routes>
        <Route path="/cursos/:id" element={<Outlet context={{ version: 0 }} />}>
          <Route path="refuerzos" element={<PlanRefuerzo />} />
          <Route path="asistente" element={<Ubicacion />} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  obtenerMapeo.mockResolvedValue(mapeo)
  obtenerTematicas.mockResolvedValue(tematicas)
  listarPlanes.mockResolvedValue([planAprobado])
  obtenerNotaNecesaria.mockImplementation((_, eid) =>
    Promise.resolve(eid === 8 ? { estado: 'NECESITA', notaNecesaria: 7.67, maximoPosible: 7.7 } : { estado: 'NO_ALCANZA', maximoPosible: 6.2 }),
  )
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('TA-15.A3 · Plan de refuerzo', () => {
  it('muestra las temáticas de la actividad con lo que domina el curso y marca las que menos del 70 % domina', async () => {
    renderPlan()

    expect(await screen.findByRole('combobox')).toHaveValue('103') // solo las actividades con temas
    expect(screen.queryByRole('option', { name: 'Tarea 4' })).toBeNull()
    const historias = await screen.findByRole('checkbox', { name: /^Historias de usuario/ })
    expect(historias).toBeChecked()
    expect(screen.getByRole('checkbox', { name: /^Estimación/ })).not.toBeChecked()
    expect(screen.getByText(/domina 40 % del curso · 2 bajo 6,0: Diego Díaz \(4,00\), Luis Mora \(5,50\)/)).toBeInTheDocument()
    expect(obtenerTematicas).toHaveBeenCalledWith('1', 103)
  })

  it('propone a quienes quedaron bajo 6,0 con su nota necesaria y dónde se vuelve a evaluar', async () => {
    renderPlan()

    const diego = await screen.findByRole('checkbox', { name: /^Diego Díaz/ })
    expect(diego).toBeChecked()
    expect(screen.queryByRole('checkbox', { name: /^Ana Acosta/ })).toBeNull()
    expect(await screen.findByText(/Historias de usuario: 4,00 · necesita 7,67 en lo pendiente para llegar a 7,0/)).toBeInTheDocument()
    expect(await screen.findByText(/ya no alcanza 7,0 \(máximo 6,20\)/)).toBeInTheDocument()
    expect(screen.getByText(/Tarea 9 · vence 28\/11\/2026/)).toBeInTheDocument()
    expect(screen.getByText('La nota necesaria es un cálculo con las notas actuales.')).toBeInTheDocument()
  })

  it('«Enviar el plan al asistente» guarda el plan con los marcados y abre el asistente con el plan', async () => {
    crearPlan.mockResolvedValue({ ...planAprobado, id: 9, estado: 'ENVIADO' })
    renderPlan('/cursos/1/refuerzos?estudiante=5')

    fireEvent.click(await screen.findByRole('checkbox', { name: /^Luis Mora/ })) // lo saca del plan
    expect(screen.getByRole('checkbox', { name: /^Ana Acosta/ })).toBeChecked() // llegó desde su ficha
    fireEvent.click(screen.getByRole('button', { name: 'Enviar el plan al asistente' }))

    await waitFor(() => expect(crearPlan).toHaveBeenCalledWith('1', { actividadId: 103, temas: [30], estudiantes: [5, 8] }))
    expect(await screen.findByTestId('ruta')).toHaveTextContent('/cursos/1/asistente?refuerzo=9')
  })

  it('el historial muestra el estado y lo que pasó con el tema, sin decir que el refuerzo lo causó', async () => {
    renderPlan()

    const historial = (await screen.findByText('Historial de refuerzos')).parentElement
    expect(within(historial).getByText('Aprobado')).toBeInTheDocument()
    expect(within(historial).getByText('Diego Díaz: subió de 5,20 a 7,10 en Historias de usuario después del refuerzo')).toBeInTheDocument()
    expect(within(historial).getByRole('link', { name: 'Ver el plan y descargarlo' })).toHaveAttribute('href', '/cursos/1/asistente?refuerzo=3')
  })

  it('sin temas asociados lleva al Mapa del curso', async () => {
    obtenerMapeo.mockResolvedValue({ ...mapeo, actividades: [mapeo.actividades[1]] })
    renderPlan()

    expect(await screen.findByRole('link', { name: 'Asócialos en el Mapa del curso' })).toHaveAttribute('href', '/cursos/1/mapa')
  })
})
