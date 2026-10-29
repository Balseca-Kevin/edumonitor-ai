import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import ConfiguracionMoodle from './ConfiguracionMoodle'

vi.mock('../services/moodleConfig', () => ({
  obtenerConfiguracion: vi.fn(),
  guardarConfiguracion: vi.fn(),
  probarConexion: vi.fn(),
}))

vi.mock('../services/auth', () => ({
  cerrarSesion: vi.fn(),
}))

import { guardarConfiguracion, obtenerConfiguracion, probarConexion } from '../services/moodleConfig'

const configuracion = {
  url: 'http://localhost:8080',
  servicio: 'edumonitor_ws',
  tokenEnmascarado: '••••3f9a',
  actualizado: '2026-10-21T22:10:00-05:00',
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

function renderPagina() {
  return render(
    <MemoryRouter>
      <ConfiguracionMoodle />
    </MemoryRouter>,
  )
}

describe('Configuración de Moodle', () => {
  it('muestra la URL y el token enmascarado, nunca completo', async () => {
    obtenerConfiguracion.mockResolvedValue(configuracion)

    renderPagina()

    expect(await screen.findByDisplayValue('http://localhost:8080')).toBeInTheDocument()
    const token = screen.getByLabelText(/token del servicio web/i)
    expect(token).toHaveAttribute('placeholder', '••••3f9a')
    expect(token).toHaveAttribute('type', 'password')
  })

  it('prueba la conexión y lista cada función con su estado', async () => {
    obtenerConfiguracion.mockResolvedValue(configuracion)
    probarConexion.mockResolvedValue({
      conectado: true,
      sitio: 'Moodle-Lab-UTA',
      version: '4.5',
      usuarioTecnico: 'edumonitor_ws_user',
      funciones: [
        { nombre: 'core_webservice_get_site_info', disponible: true },
        { nombre: 'core_completion_get_activities_completion_status', disponible: false },
      ],
    })

    renderPagina()
    fireEvent.click(await screen.findByRole('button', { name: /probar conexión/i }))

    expect(await screen.findByText(/conexión correcta/i)).toHaveTextContent('Moodle 4.5')
    expect(screen.getByText('core_webservice_get_site_info')).toBeInTheDocument()
    expect(screen.getByText('No disponible')).toBeInTheDocument()
    expect(screen.getByText(/no bloquean la sincronización/i)).toBeInTheDocument()
  })

  it('informa si Moodle no responde', async () => {
    obtenerConfiguracion.mockResolvedValue(configuracion)
    probarConexion.mockResolvedValue({ conectado: false, funciones: [], error: 'Moodle no respondió en 30 s.' })

    renderPagina()
    fireEvent.click(await screen.findByRole('button', { name: /probar conexión/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Moodle no respondió en 30 s.')
  })

  it('guarda la URL y el token y limpia el campo del token', async () => {
    obtenerConfiguracion.mockResolvedValue(configuracion)
    guardarConfiguracion.mockResolvedValue()

    renderPagina()
    const token = await screen.findByLabelText(/token del servicio web/i)
    await screen.findByDisplayValue('http://localhost:8080')
    fireEvent.change(token, { target: { value: 'token-nuevo-1234' } })
    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }))

    await waitFor(() => {
      expect(guardarConfiguracion).toHaveBeenCalledWith('http://localhost:8080', 'token-nuevo-1234')
    })
    expect(await screen.findByText('Configuración guardada.')).toBeInTheDocument()
    expect(token).toHaveValue('')
  })

  it('muestra el detalle del ProblemDetails si el servidor rechaza los datos', async () => {
    obtenerConfiguracion.mockResolvedValue(configuracion)
    guardarConfiguracion.mockRejectedValue({
      response: { status: 400, data: { title: 'Datos de entrada inválidos', detail: 'La URL de Moodle debe ser absoluta.' } },
    })

    renderPagina()
    await screen.findByDisplayValue('http://localhost:8080')
    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('La URL de Moodle debe ser absoluta.')
  })
})
