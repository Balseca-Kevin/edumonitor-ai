import {
  afterEach,
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
  within,
} from '@testing-library/react'

import { MemoryRouter } from 'react-router-dom'
import Login from './Login'

vi.mock('../services/auth', () => ({
  login: vi.fn(),
  guardarSesion: vi.fn(),
}))

import {
  login,
  guardarSesion,
} from '../services/auth'

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

function completarFormulario(usuario, contrasena) {
  fireEvent.change(screen.getByLabelText(/usuario/i), {
    target: { value: usuario },
  })

  fireEvent.change(screen.getByLabelText(/contraseña/i), {
    target: { value: contrasena },
  })

  fireEvent.click(
    screen.getByRole('button', {
      name: /ingresar/i,
    }),
  )
}

describe('Login', () => {
  it('muestra los campos de usuario y contraseña', () => {
    renderLogin()

    expect(
      screen.getByLabelText(/usuario/i),
    ).toBeInTheDocument()

    expect(
      screen.getByLabelText(/contraseña/i),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: /ingresar/i,
      }),
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

    completarFormulario('doc01', 'valor-de-prueba')

    await waitFor(() => {
      expect(login).toHaveBeenCalledWith(
        'doc01',
        'valor-de-prueba',
      )

      expect(guardarSesion).toHaveBeenCalledWith(sesion)
    })
  })

  it('muestra el título de ProblemDetails cuando las credenciales son incorrectas', async () => {
    login.mockRejectedValue(
      Object.assign(
        new Error('Request failed with status code 401'),
        {
          response: {
            status: 401,
            data: {
              title: 'Credenciales incorrectas',
            },
          },
        },
      ),
    )

    renderLogin()

    completarFormulario('doc01', 'incorrecta')

    expect(
      await screen.findByRole('alert'),
    ).toHaveTextContent('Credenciales incorrectas')
  })

  it('muestra un mensaje útil cuando el servidor no responde', async () => {
    login.mockRejectedValue(
      new Error('Network Error'),
    )

    renderLogin()

    completarFormulario('doc01', 'valor-de-prueba')

    expect(
      await screen.findByRole('alert'),
    ).toHaveTextContent(
      'No se pudo conectar con el servidor. Intenta de nuevo.',
    )
  })

  it('muestra un mensaje genérico ante errores HTTP sin título', async () => {
    login.mockRejectedValue(
      Object.assign(
        new Error('Request failed with status code 500'),
        {
          response: {
            status: 500,
            data: {},
          },
        },
      ),
    )

    renderLogin()

    completarFormulario('doc01', 'valor-de-prueba')

    expect(
      await screen.findByRole('alert'),
    ).toHaveTextContent('No se pudo iniciar sesión.')
  })
})
describe('R.7 · Inicio de sesión del Sprint 3', () => {
  it('muestra el sello en línea, el subtexto con el asistente y el período, y las tres etiquetas', () => {
    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>,
    )

    expect(screen.getByText('Aula virtual · UTA · FISEI · Software')).toBeInTheDocument()
    expect(screen.getByText(/un asistente que sabe en qué punto del período estás/)).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Ingresar' })).toBeInTheDocument()
    expect(screen.getByText('Con tu usuario docente de EduMonitor.')).toBeInTheDocument()
    const etiquetas = screen.getByRole('list', { name: 'Qué ofrece EduMonitor' })
    expect(within(etiquetas).getAllByRole('listitem').map((li) => li.textContent)).toEqual([
      'Notas tal como en Moodle',
      'Alertas con evidencia',
      'Asistente con respaldo local',
    ])
    expect(screen.queryByText(/Modelo de IA local/)).toBeNull() // las reglas del sistema no se escriben en pantalla
  })
})
