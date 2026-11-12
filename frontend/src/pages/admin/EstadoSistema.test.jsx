import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import EstadoSistema from './EstadoSistema'

vi.mock('../../services/estado', () => ({ obtenerEstadoSistema: vi.fn() }))

import { obtenerEstadoSistema } from '../../services/estado'

// Respuesta con la forma de GET /api/admin/estado (docs/contratos-api.md).
const conAgenteCaido = {
  generado: '2026-11-11T10:00:00-05:00',
  servicios: [
    { nombre: 'api', estado: 'OK', detalle: 'versión 0.2.0' },
    { nombre: 'sqlserver', estado: 'OK', detalle: 'EduMonitorDB' },
    { nombre: 'moodle', estado: 'OK', detalle: 'Moodle-Lab-UTA 4.5' },
    { nombre: 'agente', estado: 'NO_DISPONIBLE', detalle: 'No respondió en 3 s' },
    { nombre: 'modelo', estado: 'NO_DISPONIBLE', detalle: 'Sin dato: el agente no respondió' },
  ],
}

function renderEstado() {
  return render(
    <MemoryRouter>
      <EstadoSistema />
    </MemoryRouter>,
  )
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('Estado del sistema', () => {
  it('muestra un servicio por fila con su estado y detalle, y el caído como no disponible', async () => {
    obtenerEstadoSistema.mockResolvedValue(conAgenteCaido)
    renderEstado()

    const tabla = await screen.findByRole('table', { name: 'Servicios' })
    expect(within(tabla).getAllByRole('row')).toHaveLength(6)
    const fila = (texto) => within(tabla).getByText(texto).closest('tr')
    expect(within(fila('Aula virtual (Moodle)')).getByText('Disponible')).toBeInTheDocument()
    expect(within(fila('Aula virtual (Moodle)')).getByText('Moodle-Lab-UTA 4.5')).toBeInTheDocument()
    expect(within(fila('Agente de IA')).getByText('No disponible')).toBeInTheDocument()
    expect(within(fila('Agente de IA')).getByText('No respondió en 3 s')).toBeInTheDocument()
  })

  it('«Actualizar» vuelve a consultar', async () => {
    obtenerEstadoSistema.mockResolvedValue(conAgenteCaido)
    renderEstado()
    await screen.findByRole('table', { name: 'Servicios' })

    fireEvent.click(screen.getByRole('button', { name: 'Actualizar' }))

    await waitFor(() => expect(obtenerEstadoSistema).toHaveBeenCalledTimes(2))
  })

  it('un docente ve «Acceso no permitido»', async () => {
    obtenerEstadoSistema.mockRejectedValue(Object.assign(new Error('HTTP 403'), { response: { status: 403, data: {} } }))
    renderEstado()

    expect(await screen.findByRole('heading', { name: 'Acceso no permitido' })).toBeInTheDocument()
  })
})
