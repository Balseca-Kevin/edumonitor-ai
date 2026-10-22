
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import Login from './Login'

vi.mock('../services/auth', () => ({
  login: vi.fn(),
  guardarSesion: vi.fn(),
}))

import { login, guardarSesion } from '../services/auth'

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

function renderLogin() {
  return render(
    <MemoryRouter>
      <Login />
    </MemoryRouter>,
  )
}

describe('Login', () => {
  it('muestra los campos de usuario y contraseña', () => {
    renderLogin()

    expect(screen.getByLabelText(/usuario/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/contraseña/i)).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /iniciar sesión/i }),
    ).toBeInTheDocument()
  })

  it('inicia sesión y guarda los datos cuando las credenciales son correctas', async () => {
    const sesion = {
      token: 'token-prueba',
      usuario: {
        id: 2,
        usuario: 'doc01',
        nombre: 'Docente Uno',
        rol: 'DOCENTE',
      },
    }

    login.mockResolvedValue(sesion)

    renderLogin()

    fireEvent.change(screen.getByLabelText(/usuario/i), {
      target: { value: 'doc01' },
    })
    fireEvent.change(screen.getByLabelText(/contraseña/i), {
      target: { value: 'valor-de-prueba' },
    })
    fireEvent.click(screen.getByRole('button', { name: /iniciar sesión/i }))

    await waitFor(() => {
      expect(login).toHaveBeenCalledWith('doc01', 'valor-de-prueba')
      expect(guardarSesion).toHaveBeenCalledWith(sesion)
    })
  })

  it('muestra un mensaje si el inicio de sesión falla', async () => {
    login.mockRejectedValue(new Error('Credenciales incorrectas'))

    renderLogin()

    fireEvent.change(screen.getByLabelText(/usuario/i), {
      target: { value: 'doc01' },
    })
    fireEvent.change(screen.getByLabelText(/contraseña/i), {
      target: { value: 'incorrecta' },
    })
    fireEvent.click(screen.getByRole('button', { name: /iniciar sesión/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Credenciales incorrectas',
    )
  })
})
