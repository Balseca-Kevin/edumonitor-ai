import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import Auditoria from './Auditoria'

vi.mock('../../services/auditoria', () => ({ obtenerAuditoria: vi.fn() }))

import { obtenerAuditoria } from '../../services/auditoria'

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

// Forma de GET /api/auditoria (HU-17).
const registro = {
  total: 3,
  acciones: ['ACCESO_DENEGADO', 'CONSULTAR_ASISTENTE', 'SINCRONIZAR'],
  registros: [
    { id: 3, fecha: '2026-11-23T10:02:00-05:00', usuario: { id: 3, usuario: 'doc02', nombre: 'Docente Dos' }, accion: 'ACCESO_DENEGADO', entidad: '/api/cursos/1/libro', cursoId: null, detalle: 'El curso 1 no está asignado a este docente.', resultado: '403' },
    { id: 2, fecha: '2026-11-23T10:01:00-05:00', usuario: { id: 2, usuario: 'doc01', nombre: 'Docente Uno' }, accion: 'CONSULTAR_ASISTENTE', entidad: '/api/agente/chat', cursoId: 1, detalle: 'Respondió: modelo local (respaldo) · qwen2.5:7b · 5400 ms · herramientas: obtener_calificaciones', resultado: '200' },
    { id: 1, fecha: '2026-11-23T10:00:00-05:00', usuario: null, accion: 'INICIAR_SESION', entidad: '/api/auth/login', cursoId: null, detalle: 'usuario doc01', resultado: '401' },
  ],
}

function renderAuditoria() {
  return render(
    <MemoryRouter>
      <Auditoria />
    </MemoryRouter>,
  )
}

describe('HU-17 · Auditoría', () => {
  it('muestra quién hizo qué, el modelo que respondió y los accesos denegados', async () => {
    obtenerAuditoria.mockResolvedValue(registro)
    renderAuditoria()

    const tabla = await screen.findByRole('table', { name: 'Registro de auditoría' })
    const filas = within(tabla).getAllByRole('row')
    expect(filas).toHaveLength(4)
    expect(within(filas[1]).getByText('Acceso no permitido')).toBeInTheDocument()
    expect(within(filas[1]).getByText(/Denegado · 403/)).toBeInTheDocument()
    expect(within(filas[2]).getByText(/Respondió: modelo local \(respaldo\)/)).toBeInTheDocument()
    expect(within(filas[3]).getByText('Sin sesión')).toBeInTheDocument()
  })

  it('filtra por acción y usuario en la API', async () => {
    obtenerAuditoria.mockResolvedValue(registro)
    renderAuditoria()
    await screen.findByRole('table', { name: 'Registro de auditoría' })

    fireEvent.change(screen.getByLabelText('Acción'), { target: { value: 'SINCRONIZAR' } })
    fireEvent.change(screen.getByLabelText('Usuario'), { target: { value: 'doc01' } })
    fireEvent.click(screen.getByRole('button', { name: 'Filtrar' }))

    await waitFor(() => expect(obtenerAuditoria).toHaveBeenLastCalledWith({ accion: 'SINCRONIZAR', usuario: 'doc01' }))
  })

  it('un docente ve «Acceso no permitido»', async () => {
    obtenerAuditoria.mockRejectedValue(Object.assign(new Error('HTTP 403'), { response: { status: 403, data: {} } }))
    renderAuditoria()

    expect(await screen.findByRole('heading', { name: 'Acceso no permitido' })).toBeInTheDocument()
  })
})
