
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import Cursos from './Cursos'

vi.mock('../services/cursos', () => ({
  obtenerCursos: vi.fn(),
  sincronizarCurso: vi.fn(),
  obtenerHistorialSincronizaciones: vi.fn(),
}))

vi.mock('../services/auth', () => ({
  cerrarSesion: vi.fn(),
}))

import {
  obtenerCursos,
  sincronizarCurso,
  obtenerHistorialSincronizaciones,
} from '../services/cursos'
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
  it('sincroniza un curso y actualiza su estado', async () => {
  obtenerCursos.mockResolvedValue([cursoPrueba])

  sincronizarCurso.mockResolvedValue({
    id: 10,
    cursoId: 1,
    estado: 'COMPLETADA',
    inicio: '2026-10-28T10:00:00-05:00',
    fin: '2026-10-28T10:01:00-05:00',
    registros: {
      estudiantes: 30,
      calificaciones: 90,
    },
    errores: [],
  })

  obtenerHistorialSincronizaciones.mockResolvedValue([])

  renderCursos()

  await screen.findByText('AGIL-4B')

  fireEvent.click(
    screen.getByRole('button', {
      name: 'Sincronizar curso',
    }),
  )

  await waitFor(() => {
    expect(sincronizarCurso).toHaveBeenCalledWith(1)
  })

  expect(
    await screen.findByText('Resultado de sincronización'),
  ).toBeInTheDocument()
})

it('muestra un mensaje específico cuando Moodle no responde', async () => {
  obtenerCursos.mockResolvedValue([cursoPrueba])

  sincronizarCurso.mockRejectedValue({
    response: {
      status: 502,
      data: {
        title: 'Bad Gateway',
      },
    },
  })

  renderCursos()

  await screen.findByText('AGIL-4B')

  fireEvent.click(
    screen.getByRole('button', {
      name: 'Sincronizar curso',
    }),
  )

  expect(
    await screen.findByRole('alert'),
  ).toHaveTextContent(
    'Moodle no responde; intenta más tarde',
  )
})

it('muestra el historial de sincronizaciones en una tabla', async () => {
  obtenerCursos.mockResolvedValue([cursoPrueba])

  obtenerHistorialSincronizaciones.mockResolvedValue([
    {
      id: 5,
      cursoId: 1,
      estado: 'COMPLETADA',
      fin: '2026-10-22T09:15:00-05:00',
      registros: {
        estudiantes: 30,
        calificaciones: 90,
      },
      errores: [],
    },
  ])

  renderCursos()

  await screen.findByText('AGIL-4B')

  fireEvent.click(
    screen.getByRole('button', {
      name: 'Ver historial',
    }),
  )

  expect(
    await screen.findByRole('table'),
  ).toBeInTheDocument()

  expect(
    screen.getByRole('columnheader', {
      name: 'Fecha',
    }),
  ).toBeInTheDocument()

  expect(
    screen.getByRole('columnheader', {
      name: 'Registros',
    }),
  ).toBeInTheDocument()
})
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
  it('muestra un mensaje cuando el curso no está asignado', async () => {
  obtenerCursos.mockResolvedValue([cursoPrueba])

  sincronizarCurso.mockRejectedValue({
    response: {
      status: 403,
      data: {
        title: 'Forbidden',
      },
    },
  })

  renderCursos()

  await screen.findByText('AGIL-4B')

  fireEvent.click(
    screen.getByRole('button', {
      name: 'Sincronizar curso',
    }),
  )

  expect(
    await screen.findByRole('alert'),
  ).toHaveTextContent(
    'Este curso no está asignado a tu cuenta',
  )
})
it('muestra una advertencia cuando la sincronización es parcial', async () => {
  obtenerCursos.mockResolvedValue([cursoPrueba])

  sincronizarCurso.mockResolvedValue({
    id: 12,
    cursoId: 1,
    estado: 'PARCIAL',
    inicio: '2026-10-28T10:00:00-05:00',
    fin: '2026-10-28T10:01:00-05:00',
    registros: {
      estudiantes: 30,
      calificaciones: 80,
    },
    errores: [
      {
        funcion: 'sincronizarCalificaciones',
        mensaje: 'No se pudieron recuperar algunas notas',
      },
    ],
  })

  obtenerHistorialSincronizaciones.mockResolvedValue([])

  renderCursos()

  await screen.findByText('AGIL-4B')

  fireEvent.click(
    screen.getByRole('button', {
      name: 'Sincronizar curso',
    }),
  )

  expect(
    await screen.findByText('Sincronización incompleta'),
  ).toBeInTheDocument()

  expect(
    screen.getAllByText(/No se pudieron recuperar algunas notas/).length,
  ).toBeGreaterThan(0)
})
it('deshabilita el botón mientras se sincroniza un curso', async () => {
  obtenerCursos.mockResolvedValue([cursoPrueba])

  let resolverSincronizacion

  sincronizarCurso.mockImplementation(
    () =>
      new Promise((resolve) => {
        resolverSincronizacion = resolve
      }),
  )

  renderCursos()

  await screen.findByText('AGIL-4B')

  fireEvent.click(
    screen.getByRole('button', {
      name: 'Sincronizar curso',
    }),
  )

  expect(
    screen.getByRole('button', {
      name: 'Sincronizando...',
    }),
  ).toBeDisabled()

  resolverSincronizacion({
    id: 15,
    cursoId: 1,
    estado: 'COMPLETADA',
    fin: '2026-10-28T10:01:00-05:00',
    registros: {},
    errores: [],
  })

  obtenerHistorialSincronizaciones.mockResolvedValue([])

  await waitFor(() => {
    expect(
      screen.getByRole('button', {
        name: 'Sincronizar curso',
      }),
    ).toBeEnabled()
  })
})

  it('una sincronización PARCIAL no cuenta como completa', async () => {
    obtenerCursos.mockResolvedValue([
      cursoPrueba,
      {
        ...cursoPrueba,
        id: 2,
        nombreCorto: 'BDD-4B',
        nombre: 'Bases de Datos (lab)',
        ultimaSincronizacion: { id: 9, fecha: '2026-10-22T10:00:00-05:00', estado: 'PARCIAL' },
      },
    ])

    renderCursos()
    await screen.findByText('Bases de Datos (lab)')
    fireEvent.change(screen.getByLabelText(/filtrar por sincronización/i), { target: { value: 'sincronizado' } })

    expect(screen.queryByText('Bases de Datos (lab)')).not.toBeInTheDocument()
    expect(screen.getByText(cursoPrueba.nombre)).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText(/filtrar por sincronización/i), { target: { value: 'pendiente' } })
    expect(screen.getByText('Bases de Datos (lab)')).toBeInTheDocument()
  })
})
