import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import Actividad from './Actividad'

vi.mock('../../services/actividades', () => ({ obtenerActividad: vi.fn() }))
vi.mock('../../services/refuerzos', async (original) => ({ ...(await original()), crearPlan: vi.fn() }))

import { obtenerActividad } from '../../services/actividades'
import { crearPlan } from '../../services/refuerzos'

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

// Forma de GET /api/cursos/{id}/actividades/{aid} (HU-22, docs/contratos-api.md). Valores ilustrativos de la prueba.
const panel = {
  cursoId: 1,
  actividad: {
    id: 103, nombre: 'Tarea 3', sigla: 'T3', categoria: 'Tareas', unidad: { numero: 1, nombre: 'Principios', parcial: 1 },
    fechaLimite: '2026-09-22T23:59:00-05:00', estadoPeriodo: 'VENCIDA', semana: 4,
    enunciado: 'Redacta 5 historias de usuario.\n• Con criterios de aceptación',
    archivos: [{ nombre: 'plantilla-historias.docx', tamano: 20480, tipo: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' }],
  },
  resumen: { matriculados: 3, calificadas: 2, ceros: 0, porCalificar: 0, sinEntrega: 1, aunNoVence: 0, sinDato: 0, promedio: 6, aprobados: 1, bajo6: 1 },
  distribucion: Array.from({ length: 10 }, (_, i) => ({ desde: i, hasta: i + 1, cantidad: i === 4 || i === 8 ? 1 : 0 })),
  criterios: [{ id: 5, orden: 1, descripcion: 'Criterios de aceptación', puntajeMaximo: 2.5, tema: 'Historias de usuario', conNivel: 2, domina: 0.5, puntajePromedio: 1.75 }],
  temas: [{ id: 30, nombre: 'Historias de usuario', domina: 0.5, promedio: 6, estudiantesConNota: 2 }],
  requierenAtencion: [
    { id: 8, nombre: 'Diego', apellido: 'Díaz', nota: 4, estado: 'CALIFICADO', alertaActiva: true },
    { id: 9, nombre: 'Luis', apellido: 'Mora', nota: null, estado: 'SIN_ENTREGA', alertaActiva: false },
  ],
  estudiantes: [
    { id: 5, nombre: 'Ana', apellido: 'Acosta', nota: 8, estado: 'CALIFICADO', comentario: null },
    { id: 8, nombre: 'Diego', apellido: 'Díaz', nota: 4, estado: 'CALIFICADO', comentario: '• Faltan criterios de aceptación' },
    { id: 9, nombre: 'Luis', apellido: 'Mora', nota: null, estado: 'SIN_ENTREGA', comentario: null },
  ],
  noDisponibles: [],
}

function renderActividad() {
  return render(
    <MemoryRouter initialEntries={['/cursos/1/actividades/103']}>
      <Routes>
        <Route path="/cursos/:id/actividades/:aid" element={<Actividad />} />
        <Route path="/cursos/:id/asistente" element={<p>Asistente abierto</p>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('HU-22 · Panel de actividad', () => {
  it('muestra el enunciado y los adjuntos de Moodle, las entregas y el dominio por criterio', async () => {
    obtenerActividad.mockResolvedValue(panel)
    renderActividad()

    expect(await screen.findByRole('heading', { name: 'Tarea 3' })).toBeInTheDocument()
    expect(obtenerActividad).toHaveBeenCalledWith('1', '103')
    expect(screen.getByText(/Redacta 5 historias de usuario/)).toBeInTheDocument()
    expect(screen.getByText('plantilla-historias.docx')).toBeInTheDocument()
    expect(screen.getByText('Sin entregar').nextSibling).toHaveTextContent('1')
    expect(screen.getByText(/50 % del curso logra el 70 % del puntaje/)).toBeInTheDocument()
    expect(screen.getByText(/Vencida/)).toBeInTheDocument()
  })

  it('cada nota abre su celda del libro y quien requiere atención abre su ficha', async () => {
    obtenerActividad.mockResolvedValue(panel)
    renderActividad()

    const tabla = await screen.findByRole('table', { name: 'Notas de la actividad' })
    expect(within(tabla).getByText('4,00').closest('a')).toHaveAttribute('href', '/cursos/1/libro?estudiante=8&actividad=103')
    expect(within(tabla).getByText('Sin entrega')).toHaveClass('nota--sin-entrega')
    expect(screen.getByRole('link', { name: /Diego Díaz · 4,00 · requiere atención/ })).toHaveAttribute('href', '/cursos/1/estudiantes/8')
  })

  it('«Redactar retroalimentación» guarda el plan de tipo retroalimentación con quienes quedaron bajo 7,0 y abre el asistente', async () => {
    obtenerActividad.mockResolvedValue(panel)
    crearPlan.mockResolvedValue({ id: 12 })
    renderActividad()

    fireEvent.click(await screen.findByRole('button', { name: 'Redactar retroalimentación (1)' }))

    await waitFor(() => expect(crearPlan).toHaveBeenCalledWith('1', { actividadId: 103, temas: [], estudiantes: [8], tipo: 'RETROALIMENTACION' }))
    expect(await screen.findByText('Asistente abierto')).toBeInTheDocument()
  })

  it('una actividad que no es del curso se ve como error', async () => {
    obtenerActividad.mockRejectedValue(Object.assign(new Error('HTTP 404'), { response: { status: 404, data: { detail: 'La actividad 103 no es del curso 1.' } } }))
    renderActividad()

    expect(await screen.findByRole('alert')).toHaveTextContent('La actividad 103 no es del curso 1.')
  })
})
