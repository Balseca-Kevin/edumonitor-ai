import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import FichaEstudiante from './FichaEstudiante'

vi.mock('../../services/fichas', () => ({ obtenerFicha: vi.fn() }))
vi.mock('../../services/notaNecesaria', () => ({
  obtenerNotaNecesaria: vi.fn().mockResolvedValue({ estado: 'NECESITA', notaNecesaria: 7.67, maximoPosible: 7.7, pendientes: 2, categorias: [] }),
}))

import { obtenerFicha } from '../../services/fichas'

beforeAll(() => {
  globalThis.ResizeObserver ??= class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

// Forma de GET /api/cursos/{id}/estudiantes/{eid} (HU-10, docs/contratos-api.md). Valores ilustrativos de la prueba.
const ficha = {
  cursoId: 1,
  estudiante: { id: 8, nombre: 'Diego', apellido: 'Díaz' },
  aprobacion: 7,
  promedioPonderado: 5.4,
  parcial: false,
  grupo: 'BAJO',
  notas: { calificadas: 2, ceros: 1, sinEntrega: 1, pendientes: 0, aunNoVence: 1 },
  categorias: [{ categoria: 'Tareas', peso: 0.3, promedio: 4, notas: 2 }],
  actividades: [
    { id: 101, nombre: 'Tarea 1', categoria: 'Tareas', unidad: 1, nombreUnidad: 'Unidad 1', fechaLimite: '2026-09-08T23:59:00-05:00', nota: 0, estado: 'CALIFICADO', comentario: null, criterios: [] },
    {
      id: 103, nombre: 'Tarea 3', categoria: 'Tareas', unidad: 1, nombreUnidad: 'Unidad 1', fechaLimite: '2026-09-22T23:59:00-05:00', nota: 8, estado: 'CALIFICADO',
      comentario: '• Revisar los criterios de aceptación', criterios: [{ descripcion: 'Criterios de aceptación', nivel: 'Insuficiente', puntaje: 1, puntajeMaximo: 2.5 }],
    },
    { id: 104, nombre: 'Tarea 4', categoria: 'Tareas', unidad: 2, nombreUnidad: 'Unidad 2', fechaLimite: '2026-10-01T23:59:00-05:00', nota: null, estado: 'SIN_ENTREGA', comentario: null, criterios: [] },
    { id: 118, nombre: 'Tarea 9', categoria: 'Tareas', unidad: 4, nombreUnidad: 'Unidad 4', fechaLimite: '2026-12-01T23:59:00-05:00', nota: null, estado: 'AUN_NO_VENCE', comentario: null, criterios: [] },
  ],
  evolucion: [
    { actividadId: 101, actividad: 'Tarea 1', fechaLimite: '2026-09-08T23:59:00-05:00', nota: 0, promedio: 0 },
    { actividadId: 103, actividad: 'Tarea 3', fechaLimite: '2026-09-22T23:59:00-05:00', nota: 8, promedio: 4 },
  ],
  temasDebiles: [
    { id: 30, nombre: 'Historias de usuario', unidad: 1, nota: 4, promedioCurso: 6.42, notas: [{ actividadId: 103, actividad: 'Tarea 3', nota: 4 }], proximas: [{ actividadId: 118, nombre: 'Tarea 9', fechaLimite: '2026-12-01T23:59:00-05:00' }] },
  ],
  alertas: [{ id: 31, regla: 'R4', evidencia: { promedio: 5.4, notas: 4, umbral: 7 }, generada: '2026-11-20T10:00:00-05:00' }],
  noDisponibles: [],
}

function renderFicha() {
  return render(
    <MemoryRouter initialEntries={['/cursos/1/estudiantes/8']}>
      <Routes>
        <Route path="/cursos/:id/estudiantes/:eid" element={<FichaEstudiante />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('HU-10 · Ficha del estudiante', () => {
  it('muestra las notas como el libro, con cero, sin entrega y aún no vence diferenciados, y cada nota abre su celda', async () => {
    obtenerFicha.mockResolvedValue(ficha)
    renderFicha()

    expect(await screen.findByRole('heading', { name: 'Díaz, Diego' })).toBeInTheDocument()
    expect(obtenerFicha).toHaveBeenCalledWith('1', '8')
    const tabla = screen.getByRole('table', { name: 'Notas por actividad' })
    expect(within(tabla).getByText('0,00')).toHaveClass('nota--bajo')
    expect(within(tabla).getByText('Sin entrega')).toHaveClass('nota--sin-entrega')
    expect(within(tabla).getByText('Aún no vence')).toHaveClass('nota--aun-no-vence')
    expect(within(tabla).getByText('8,00').closest('a')).toHaveAttribute('href', '/cursos/1/libro?estudiante=8&actividad=103')
    expect(within(tabla).getByText('• Revisar los criterios de aceptación')).toBeInTheDocument()
    expect(within(tabla).getByText(/Criterios de aceptación:/)).toBeInTheDocument()
  })

  it('muestra el promedio con enlace al libro, los temas débiles con su origen y las alertas como «Requiere atención»', async () => {
    obtenerFicha.mockResolvedValue(ficha)
    renderFicha()

    const promedio = await screen.findByRole('link', { name: '5,40' })
    expect(promedio).toHaveAttribute('href', '/cursos/1/libro?estudiante=8')
    expect(screen.getAllByText('Requiere atención').length).toBeGreaterThan(0)
    expect(screen.getByText('Historias de usuario')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Tarea 3 (4,00)' })).toHaveAttribute('href', '/cursos/1/libro?estudiante=8&actividad=103')
    expect(screen.getByText('Requiere atención · Promedio bajo')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Analizar con el asistente' })).toBeInTheDocument()
    expect(await screen.findByText('cálculo con las notas actuales')).toBeInTheDocument()
  })

  it('un curso sin sincronizar se ve como aviso, sin cifras en 0', async () => {
    obtenerFicha.mockRejectedValue(Object.assign(new Error('HTTP 409'), { response: { status: 409, data: { title: 'Curso sin sincronizar' } } }))
    renderFicha()

    expect(await screen.findByRole('alert')).toHaveTextContent('Curso sin sincronizar')
  })
})
