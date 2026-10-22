
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import Cursos from './Cursos'

vi.mock('../services/cursos', () => ({
  obtenerCursos: vi.fn(),
}))

vi.mock('../services/auth', () => ({
  cerrarSesion: vi.fn(),
}))

import { obtenerCursos } from '../services/cursos'
import { cerrarSesion } from '../services/auth'

const cursoPrueba = {
  id: 1,
  moodleId: 3,
  nombreCorto: 'AGIL-4B',
  nombre: 'Metodologías Ágiles (lab)',
  periodo: '2026-2',
  ultimaSincronizacion: {
    id: 5,
    fecha: '2026-10-22T09:15:00-05:00',
    estado: 'COMPLETADA',
  },
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  sessionStorage.clear()
})

function renderCursos() {
  return render(
    <MemoryRouter>
      <Cursos />
    </MemoryRouter>,
  )
}

describe('Mis cursos', () => {
  it('muestra los cursos recibidos del servicio', async () => {
    obtenerCursos.mockResolvedValue([cursoPrueba])

    renderCursos()

    expect(await screen.findByText('Metodologías Ágiles (lab)')).toBeInTheDocument()
    expect(screen.getByText('AGIL-4B')).toBeInTheDocument()
    expect(screen.getByText('COMPLETADA')).toBeInTheDocument()
    expect(screen.getByText('1 curso')).toBeInTheDocument()
  })

  it('muestra un mensaje cuando no hay cursos', async () => {
    obtenerCursos.mockResolvedValue([])

    renderCursos()

    expect(await screen.findByText('Aún no tienes cursos')).toBeInTheDocument()
  })

  it('muestra un mensaje si falla la carga', async () => {
    obtenerCursos.mockRejectedValue(new Error('Error de conexión'))

    renderCursos()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'No fue posible cargar los cursos',
    )
  })

  it('cierra la sesión cuando se pulsa el botón correspondiente', async () => {
    obtenerCursos.mockResolvedValue([])

    renderCursos()

    await screen.findByText('Aún no tienes cursos')

    fireEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }))

    expect(cerrarSesion).toHaveBeenCalledOnce()
    await waitFor(() => {
      expect(window.location.pathname).not.toBe('/cursos')
    })
  })
})
