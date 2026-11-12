
import { afterEach, describe, expect, it } from 'vitest'
import {
  act,
  cleanup,
  render,
  screen,
} from '@testing-library/react'
import {
  MemoryRouter,
  Route,
  Routes,
} from 'react-router-dom'
import Layout from './Layout'

afterEach(() => {
  cleanup()
  sessionStorage.clear()
})

function renderLayout(ruta = '/cursos') {
  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <Routes>
        <Route path="/login" element={<p>Página de login</p>} />

        <Route element={<Layout />}>
          <Route path="/cursos" element={<p>Lista de cursos</p>} />
          <Route path="/admin/moodle" element={<p>Configuración administrativa</p>} />
          <Route path="/admin/estado" element={<p>Estado administrativo</p>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

function establecerSesion(rol) {
  sessionStorage.setItem('edumonitor_token', 'jwt-de-prueba')
  sessionStorage.setItem(
    'edumonitor_usuario',
    JSON.stringify({ nombre: 'Usuario de prueba', rol }),
  )
}

describe('HU-19 · Control de acceso', () => {
  it('redirige al login cuando no existe token', () => {
    renderLayout('/cursos')

    expect(screen.getByText('Página de login')).toBeInTheDocument()
  })

  it('bloquea una ruta ADMIN para DOCENTE con pantalla 403', () => {
    establecerSesion('DOCENTE')
    renderLayout('/admin/moodle')

    expect(
      screen.getByRole('heading', { name: 'Acceso no permitido' }),
    ).toBeInTheDocument()

    expect(
      screen.queryByText('Configuración administrativa'),
    ).not.toBeInTheDocument()
  })

  it('permite acceder a una ruta ADMIN con rol ADMIN', () => {
    establecerSesion('ADMIN')
    renderLayout('/admin/moodle')

    expect(
      screen.getByText('Configuración administrativa'),
    ).toBeInTheDocument()
  })

  it('muestra la pantalla 401 cuando se notifica sesión vencida', () => {
    establecerSesion('DOCENTE')
    renderLayout('/cursos')

    expect(screen.getByText('Lista de cursos')).toBeInTheDocument()

    act(() => {
      window.dispatchEvent(
        new Event('edumonitor:sesion-vencida'),
      )
    })

    expect(
      screen.getByRole('heading', { name: 'Tu sesión venció' }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', { name: 'Iniciar sesión' }),
    ).toBeInTheDocument()
  })
})
