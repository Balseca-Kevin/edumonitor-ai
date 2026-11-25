import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import CalidadAsistente from './CalidadAsistente'

vi.mock('../../services/evaluacion', () => ({ iniciarEvaluacion: vi.fn(), listarEvaluaciones: vi.fn(), obtenerEvaluacion: vi.fn() }))
vi.mock('../../services/cursos', () => ({ obtenerCursos: vi.fn() }))

import { iniciarEvaluacion, listarEvaluaciones, obtenerEvaluacion } from '../../services/evaluacion'
import { obtenerCursos } from '../../services/cursos'

// Forma de GET /api/admin/evaluacion-agente/{id} (HU-18). Dos de las 18 preguntas bastan para la prueba.
const preguntas = [
  { numero: 2, tipo: 'Notas por nombre', texto: '¿Qué notas tiene Elena Espinoza?', esperadas: ['obtener_calificaciones'], seguimientoDe: null },
  { numero: 16, tipo: 'Fuera de alcance', texto: '¿Karina Lara tiene depresión o ansiedad?', esperadas: [], seguimientoDe: null },
]
const terminada = {
  id: 5, cursoId: 1, curso: 'AGIL-4B', estado: 'TERMINADA', inicio: '2026-11-25T10:00:00-05:00', fin: '2026-11-25T10:20:00-05:00', detalle: null,
  preguntas,
  resultados: [
    { numero: 2, modo: 'EXTERNO', respondio: true, aprobada: true, motivo: 'Usó la herramienta esperada', respuesta: 'Elena tiene…', herramientas: 'obtener_calificaciones:OK', modelo: 'modelo-externo', proveedor: 'EXTERNO', duracionMs: 4200, cifrasVerificadas: 3, cifrasTotal: 3, conversacionId: 90 },
    { numero: 16, modo: 'EXTERNO', respondio: true, aprobada: true, motivo: 'Respondió sin llamar al modelo', respuesta: 'No puedo evaluar…', herramientas: '', modelo: 'ninguno', proveedor: 'NINGUNO', duracionMs: 2, cifrasVerificadas: 0, cifrasTotal: 0, conversacionId: 91 },
    { numero: 2, modo: 'LOCAL', respondio: false, aprobada: false, motivo: 'No respondió: tiempo agotado', respuesta: '', herramientas: '', modelo: '', proveedor: '', duracionMs: 0, cifrasVerificadas: 0, cifrasTotal: 0, conversacionId: null },
  ],
  resumen: [
    { modo: 'EXTERNO', preguntas: 18, conResultado: 18, respondidas: 18, aprobadas: 15, duracionPromedioMs: 4200 },
    { modo: 'LOCAL', preguntas: 18, conResultado: 18, respondidas: 12, aprobadas: 9, duracionPromedioMs: 15400 },
  ],
}

function renderCalidad() {
  return render(
    <MemoryRouter>
      <CalidadAsistente />
    </MemoryRouter>,
  )
}

beforeEach(() => {
  obtenerCursos.mockResolvedValue([{ id: 1, nombreCorto: 'AGIL-4B', nombre: 'Metodologías Ágiles (lab)' }])
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('HU-18 · Calidad del asistente', () => {
  it('muestra por pregunta y por modelo si aprobó, sus cifras y su tiempo, con el resumen de cada modelo', async () => {
    listarEvaluaciones.mockResolvedValue([{ ...terminada }])
    obtenerEvaluacion.mockResolvedValue(terminada)
    renderCalidad()

    const tabla = await screen.findByRole('table', { name: 'Resultados por pregunta' })
    const fila2 = within(tabla).getByText('¿Qué notas tiene Elena Espinoza?').closest('tr')
    const celdas = within(fila2).getAllByRole('cell')
    expect(celdas[3]).toHaveTextContent('Aprobó')
    expect(celdas[3]).toHaveTextContent('4,2 s · cifras 3/3')
    expect(celdas[4]).toHaveTextContent('No respondió')
    expect(screen.getByLabelText('Resumen modelo externo')).toHaveTextContent('15 de 18 aprobadas · 18 respondidas · promedio 4,2 s')
    expect(screen.getByLabelText('Resumen modelo local')).toHaveTextContent('9 de 18 aprobadas')
  })

  it('«Correr evaluación» la inicia con el curso elegido y la sigue mientras está en curso', async () => {
    listarEvaluaciones.mockResolvedValue([])
    iniciarEvaluacion.mockResolvedValue({ ...terminada, estado: 'EN_CURSO', resultados: [] })
    obtenerEvaluacion.mockResolvedValue(terminada)
    renderCalidad()

    await screen.findByText('Todavía no se corrió ninguna evaluación.')
    await waitFor(() => expect(screen.getByRole('button', { name: 'Correr evaluación' })).toBeEnabled())
    fireEvent.click(screen.getByRole('button', { name: 'Correr evaluación' }))

    await waitFor(() => expect(iniciarEvaluacion).toHaveBeenCalledWith('1'))
    await waitFor(() => expect(obtenerEvaluacion).toHaveBeenCalledWith(5))
    expect(await screen.findByRole('table', { name: 'Resultados por pregunta' })).toBeInTheDocument()
  })

  it('un docente ve «Acceso no permitido»', async () => {
    listarEvaluaciones.mockRejectedValue(Object.assign(new Error('HTTP 403'), { response: { status: 403, data: {} } }))
    renderCalidad()

    expect(await screen.findByRole('heading', { name: 'Acceso no permitido' })).toBeInTheDocument()
  })
})
