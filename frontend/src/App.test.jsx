import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from '@testing-library/react'

import App from './App'

vi.mock('./services/auth', () => ({
  login: vi.fn(),

  guardarSesion: vi.fn((sesion) => {
    sessionStorage.setItem('edumonitor_token', sesion.token)
    sessionStorage.setItem(
      'edumonitor_usuario',
      JSON.stringify(sesion.usuario),
    )
  }),

  cerrarSesion: vi.fn(() => {
    sessionStorage.removeItem('edumonitor_token')
    sessionStorage.removeItem('edumonitor_usuario')
  }),
}))

vi.mock('./services/cursos', () => ({
  obtenerCursos: vi.fn(),
  sincronizarCurso: vi.fn(),
  obtenerHistorialSincronizaciones: vi.fn(),
}))

import { login } from './services/auth'
import { obtenerCursos } from './services/cursos'

beforeEach(() => {
  sessionStorage.clear()
  window.history.pushState({}, '', '/login')
  vi.clearAllMocks()
})

afterEach(() => {
  cleanup()
  sessionStorage.clear()
  window.history.pushState({}, '', '/')
})

describe('TA-06.5 - Navegación de EduMonitor', () => {
  it('redirige al login cuando no existe una sesión', async () => {
    window.history.pushState({}, '', '/cursos')

    render(<App />)

    expect(
      await screen.findByRole('button', {
        name: 'Iniciar sesión',
      }),
    ).toBeInTheDocument()

    expect(window.location.pathname).toBe('/login')
  })

  it('permite iniciar sesión y acceder a Mis cursos', async () => {
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

    obtenerCursos.mockResolvedValue([
      {
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
      },
    ])

    render(<App />)

    fireEvent.change(screen.getByLabelText('Usuario'), {
      target: { value: 'doc01' },
    })

    fireEvent.change(screen.getByLabelText('Contraseña'), {
      target: { value: 'valor-de-prueba' },
    })

    fireEvent.click(
      screen.getByRole('button', {
        name: 'Iniciar sesión',
      }),
    )

    expect(
      await screen.findByRole('heading', {
        name: 'Mis cursos',
      }),
    ).toBeInTheDocument()

    expect(
      await screen.findByText('AGIL-4B'),
    ).toBeInTheDocument()

    await waitFor(() => {
      expect(window.location.pathname).toBe('/cursos')
    })

    expect(
      sessionStorage.getItem('edumonitor_token'),
    ).toBe('token-prueba')
  })

  it('permite acceder a cursos cuando existe una sesión', async () => {
    sessionStorage.setItem(
      'edumonitor_token',
      'token-prueba',
    )

    obtenerCursos.mockResolvedValue([])

    window.history.pushState({}, '', '/cursos')

    render(<App />)

    expect(
      await screen.findByRole('heading', {
        name: 'Mis cursos',
      }),
    ).toBeInTheDocument()

    expect(window.location.pathname).toBe('/cursos')
  })
})
