import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import CursoMarco from './CursoMarco'
import Asistente from './Asistente'

vi.mock('../../services/agente', () => ({ preguntarAlAsistente: vi.fn() }))
vi.mock('../../services/cursos', () => ({ obtenerCurso: vi.fn() }))

import { preguntarAlAsistente } from '../../services/agente'
import { obtenerCurso } from '../../services/cursos'

const curso = {
  id: 1,
  moodleId: 2,
  nombreCorto: 'AGIL-4B',
  nombre: 'Metodologías Ágiles (lab)',
  periodo: '2026-2',
  ultimaSincronizacion: { id: 12, fecha: '2026-11-09T07:10:00-05:00', estado: 'COMPLETADA' },
}

// Respuesta del contrato de POST /api/agente/chat (docs/contratos-api.md).
const respuestaChat = {
  conversacionId: 44,
  respuesta: 'Hechos: 4 estudiantes tienen alertas activas.',
  herramientas: [{ nombre: 'obtener_calificaciones', parametros: { cursoId: 1 }, estado: 'OK', duracionMs: 95 }],
  modelo: 'qwen2.5:7b',
  duracionMs: 5400,
}

function errorHttp(status, data = {}) {
  return Object.assign(new Error(`HTTP ${status}`), { response: { status, data } })
}

function renderAsistente() {
  return render(
    <MemoryRouter initialEntries={['/cursos/1/asistente']}>
      <Routes>
        <Route path="/cursos/:id" element={<CursoMarco />}>
          <Route path="asistente" element={<Asistente />} />
        </Route>
        <Route path="/cursos" element={<p>Mis cursos</p>} />
      </Routes>
    </MemoryRouter>,
  )
}

async function preguntar(texto) {
  fireEvent.change(await screen.findByLabelText('Tu pregunta'), { target: { value: texto } })
  fireEvent.click(screen.getByRole('button', { name: 'Preguntar' }))
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('Asistente', () => {
  it('muestra la respuesta y la tabla de herramientas usadas con estado y duración', async () => {
    obtenerCurso.mockResolvedValue(curso)
    preguntarAlAsistente.mockResolvedValue(respuestaChat)
    renderAsistente()

    await preguntar('¿Qué estudiantes requieren atención?')

    expect(await screen.findByText('Hechos: 4 estudiantes tienen alertas activas.')).toBeInTheDocument()
    expect(preguntarAlAsistente).toHaveBeenCalledWith('1', '¿Qué estudiantes requieren atención?')
    const tabla = screen.getByRole('table', { name: 'Herramientas usadas' })
    expect(within(tabla).getByText('obtener_calificaciones')).toBeInTheDocument()
    expect(within(tabla).getByText('cursoId=1')).toBeInTheDocument()
    expect(within(tabla).getByText('OK')).toBeInTheDocument()
    expect(within(tabla).getByText('95')).toBeInTheDocument()
    expect(screen.getByText(/qwen2\.5:7b/)).toBeInTheDocument()
  })

  it('mientras consulta muestra «Consultando…» y deshabilita el botón', async () => {
    obtenerCurso.mockResolvedValue(curso)
    let responder
    preguntarAlAsistente.mockReturnValue(new Promise((resolve) => { responder = resolve }))
    renderAsistente()

    await preguntar('¿Qué cursos tengo asignados?')

    expect(await screen.findByRole('status')).toHaveTextContent('Consultando…')
    expect(screen.getByRole('button', { name: 'Consultando…' })).toBeDisabled()
    responder(respuestaChat)
    await waitFor(() => expect(screen.queryByRole('status')).not.toBeInTheDocument())
  })

  it('con 503 muestra «El asistente no está disponible»', async () => {
    obtenerCurso.mockResolvedValue(curso)
    preguntarAlAsistente.mockRejectedValue(errorHttp(503, { title: 'Asistente no disponible', detail: 'El asistente no respondió en 60 segundos.' }))
    renderAsistente()

    await preguntar('¿Qué estudiantes requieren atención?')

    const aviso = await screen.findByRole('alert')
    expect(aviso).toHaveTextContent('El asistente no está disponible')
    expect(aviso).toHaveTextContent('no respondió en 60 segundos')
  })

  it('un curso ajeno muestra «Acceso no permitido» en pantalla completa', async () => {
    obtenerCurso.mockRejectedValue(errorHttp(403, { detail: 'El curso 2 no está asignado a este docente.' }))
    renderAsistente()

    expect(await screen.findByRole('heading', { name: 'Acceso no permitido' })).toBeInTheDocument()
    expect(screen.getByText('El curso 2 no está asignado a este docente.')).toBeInTheDocument()
    expect(screen.queryByLabelText('Tu pregunta')).not.toBeInTheDocument()
  })

  it('una sesión vencida muestra «Tu sesión venció»', async () => {
    obtenerCurso.mockRejectedValue(errorHttp(401))
    renderAsistente()

    expect(await screen.findByRole('heading', { name: 'Tu sesión venció' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Iniciar sesión' })).toBeInTheDocument()
  })
})
