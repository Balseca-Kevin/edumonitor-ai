import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import Cursos from './Cursos'

vi.mock('../services/cursos', () => ({
  obtenerCursos: vi.fn(),
  sincronizarCurso: vi.fn(),
  obtenerHistorialSincronizaciones: vi.fn(),
}))
vi.mock('../services/indicadores', () => ({ obtenerIndicadores: vi.fn(), contarEstudiantesConAlertas: vi.fn() }))
vi.mock('../services/auth', () => ({ cerrarSesion: vi.fn() }))
vi.mock('../services/periodo', () => ({ obtenerPeriodo: vi.fn() }))

import { obtenerCursos, obtenerHistorialSincronizaciones, sincronizarCurso } from '../services/cursos'
import { contarEstudiantesConAlertas, obtenerIndicadores } from '../services/indicadores'
import { cerrarSesion } from '../services/auth'
import { obtenerPeriodo } from '../services/periodo'

// Formas de GET /api/cursos y de GET /api/cursos/{id}/indicadores (docs/contratos-api.md).
const agil = {
  id: 1,
  moodleId: 2,
  nombreCorto: 'AGIL-4B',
  nombre: 'Metodologías Ágiles (lab)',
  periodo: '2026-2',
  ultimaSincronizacion: { id: 5, fecha: '2026-11-21T09:15:00-05:00', estado: 'COMPLETADA' },
}
const mcs = { id: 3, moodleId: 4, nombreCorto: 'MCS-4B', nombre: 'Manejo y Configuración del Software (lab)', periodo: '2026-2', ultimaSincronizacion: null }

const indicadores = {
  curso: { promedio: 7.42 },
  estudiantes: [{ notas: { calificadas: 12 } }, { notas: { calificadas: 10 } }],
}

// Forma de GET /api/cursos/{id}/periodo (docs/contratos-api.md, «Estado del período»).
const periodo = {
  cursoId: 1,
  semana: { actual: 12, total: 15 },
  estado: 'EN_CURSO',
  unidadActual: { numero: 4, nombre: 'Herramientas ágiles del mercado', parcial: 2 },
  unidades: [],
  actividades: [],
  vencidas: { actividades: 0, entregasQueFaltan: 0, conFaltas: [] },
  venceEstaSemana: [{ id: 117, nombre: 'Examen Parcial 2', fechaLimite: '2026-11-25T18:59:00-05:00', entregaron: 0, matriculados: 32 }],
  siguiente: null,
  noDisponibles: [],
}

function Ubicacion() {
  const { pathname, state } = useLocation()
  return (
    <>
      <p data-testid="ruta">{pathname}</p>
      <p data-testid="estado">{JSON.stringify(state)}</p>
    </>
  )
}

function renderCursos() {
  return render(
    <MemoryRouter initialEntries={['/cursos']}>
      <Routes>
        <Route path="/cursos" element={<Cursos />} />
        <Route path="*" element={<Ubicacion />} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  obtenerIndicadores.mockResolvedValue(indicadores)
  contarEstudiantesConAlertas.mockResolvedValue(4)
  obtenerHistorialSincronizaciones.mockResolvedValue([])
  obtenerPeriodo.mockResolvedValue(periodo)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  sessionStorage.clear()
})

describe('R.3 · Mis cursos en tarjetas', () => {
  it('cada tarjeta muestra el curso, quienes requieren atención, el promedio y las entregas calificadas reales', async () => {
    obtenerCursos.mockResolvedValue([agil])
    renderCursos()

    const tarjeta = await screen.findByRole('article', { name: 'AGIL-4B · Metodologías Ágiles (lab)' })
    expect(await within(tarjeta).findByText('4 requieren atención')).toBeInTheDocument()
    expect(within(tarjeta).getByText('7,42')).toBeInTheDocument()
    expect(within(tarjeta).getByText('22')).toBeInTheDocument() // 12 + 10 notas calificadas
    expect(within(tarjeta).getByText(/Última sincronización: 21\/11\/2026.*COMPLETADA/)).toBeInTheDocument()
    expect(within(tarjeta).getByRole('button', { name: 'Abrir curso' })).toBeInTheDocument()
    expect(within(tarjeta).getByRole('button', { name: 'Sincronizar y abrir' })).toBeInTheDocument()
    expect(obtenerIndicadores).toHaveBeenCalledWith(1)
  })

  it('TA-22.A5 · la tarjeta muestra la semana, la unidad actual y lo que vence esta semana del backend', async () => {
    obtenerCursos.mockResolvedValue([agil])
    renderCursos()

    const tarjeta = await screen.findByRole('article', { name: /AGIL-4B/ })
    expect(await within(tarjeta).findByText('Semana 12 de 15')).toBeInTheDocument()
    expect(within(tarjeta).getByText('U4 · Herramientas ágiles del mercado')).toBeInTheDocument()
    expect(within(tarjeta).getByRole('progressbar', { name: 'Avance del período' })).toHaveAttribute('aria-valuenow', '12')
    expect(within(tarjeta).getByText('Vence esta semana: Examen Parcial 2')).toBeInTheDocument()
    expect(obtenerPeriodo).toHaveBeenCalledWith(1)
  })

  it('TA-22.A5 · sin nada que venza lo dice y, si el período falla, la tarjeta sigue sin la línea', async () => {
    obtenerCursos.mockResolvedValue([agil])
    obtenerPeriodo.mockResolvedValueOnce({ ...periodo, venceEstaSemana: [] })
    const { unmount } = renderCursos()
    expect(await screen.findByText('Nada vence esta semana')).toBeInTheDocument()
    unmount()

    obtenerPeriodo.mockRejectedValueOnce({ response: { status: 500 } })
    renderCursos()
    expect(await screen.findByText('7,42')).toBeInTheDocument()
    expect(screen.queryByText(/Semana \d+ de/)).toBeNull()
  })

  it('un curso sin sincronizar lo dice y no muestra cifras en 0', async () => {
    obtenerCursos.mockResolvedValue([mcs])
    renderCursos()

    const tarjeta = await screen.findByRole('article', { name: /MCS-4B/ })
    expect(await within(tarjeta).findByText(/todavía no tiene datos del aula virtual/)).toBeInTheDocument()
    expect(within(tarjeta).getByText('Sin sincronizar')).toBeInTheDocument()
    expect(within(tarjeta).queryByText('0')).toBeNull()
    expect(obtenerIndicadores).not.toHaveBeenCalled()
  })

  it('sin alertas no muestra la etiqueta y, si los indicadores fallan, dice «no disponible»', async () => {
    obtenerCursos.mockResolvedValue([agil])
    contarEstudiantesConAlertas.mockResolvedValue(0)
    obtenerIndicadores.mockRejectedValue({ response: { status: 500 } })
    renderCursos()

    const tarjeta = await screen.findByRole('article', { name: /AGIL-4B/ })
    await waitFor(() => expect(within(tarjeta).getAllByText('no disponible')).toHaveLength(2))
    expect(within(tarjeta).queryByText(/requieren atención/)).toBeNull()
  })

  it('«Abrir curso» lleva al panel del curso', async () => {
    obtenerCursos.mockResolvedValue([agil])
    renderCursos()

    fireEvent.click(await screen.findByRole('button', { name: 'Abrir curso' }))
    expect(screen.getByTestId('ruta')).toHaveTextContent('/cursos/1/panel')
  })

  it('«Sincronizar y abrir» sincroniza y abre el curso', async () => {
    obtenerCursos.mockResolvedValue([agil])
    sincronizarCurso.mockResolvedValue({ id: 6, estado: 'COMPLETADA', fin: '2026-11-22T10:00:00-05:00', errores: [] })
    renderCursos()

    fireEvent.click(await screen.findByRole('button', { name: 'Sincronizar y abrir' }))

    expect(sincronizarCurso).toHaveBeenCalledWith(1)
    expect(await screen.findByTestId('ruta')).toHaveTextContent('/cursos/1/panel')
    // TA-21.A2 · El resultado viaja al curso para el aviso con las cifras de la sincronización.
    expect(JSON.parse(screen.getByTestId('estado').textContent).sincronizacion.id).toBe(6)
  })

  it('TA-21.A2 · «Sincronizar y abrir» muestra «Sincronizando curso…» con los 5 pasos, que giran hasta que responde el aula virtual', async () => {
    obtenerCursos.mockResolvedValue([agil])
    let responder
    sincronizarCurso.mockReturnValue(new Promise((listo) => (responder = listo)))
    renderCursos()

    fireEvent.click(await screen.findByRole('button', { name: 'Sincronizar y abrir' }))

    const carga = await screen.findByRole('dialog', { name: 'Sincronizando curso…' })
    expect(within(carga).getByText('AGIL-4B · AULA VIRTUAL')).toBeInTheDocument()
    expect(within(carga).getAllByRole('listitem').map((li) => li.firstChild.nextSibling.firstChild.textContent)).toEqual([
      'Cursos y unidades',
      'Matriculados',
      'Ítems y notas por categoría',
      'Entregas y fechas límite',
      'Finalización de actividades',
    ])
    expect(within(carga).getByRole('progressbar', { name: 'Avance' })).toHaveAttribute('aria-valuenow', '0')

    responder({
      id: 6,
      estado: 'PARCIAL',
      fin: '2026-11-22T10:00:00-05:00',
      errores: [{ funcion: 'core_completion_get_activities_completion_status', mensaje: 'Acceso denegado' }],
    })

    await waitFor(() => expect(within(carga).getByRole('progressbar', { name: 'Avance' })).toHaveAttribute('aria-valuenow', '5'))
    expect(within(carga).getByText('Acceso denegado')).toBeInTheDocument()
    expect(within(carga).getByText('Finalización de actividades').closest('li')).toHaveClass('carga-paso--error')
    expect(within(carga).getByText('Matriculados').closest('li')).toHaveClass('carga-paso--listo')
    expect(await screen.findByTestId('ruta')).toHaveTextContent('/cursos/1/panel')
  })

  it('una sincronización FALLIDA no abre el curso y muestra el historial con sus errores', async () => {
    obtenerCursos.mockResolvedValue([agil])
    const fallida = {
      id: 7,
      estado: 'FALLIDA',
      inicio: '2026-11-22T10:00:00-05:00',
      fin: '2026-11-22T10:00:05-05:00',
      registros: { unidades: 0, actividades: 0, estudiantes: 0, calificaciones: 0 },
      errores: [{ funcion: 'core_course_get_contents', mensaje: 'Moodle no respondió' }],
    }
    sincronizarCurso.mockResolvedValue(fallida)
    obtenerHistorialSincronizaciones.mockResolvedValue([fallida])
    renderCursos()

    fireEvent.click(await screen.findByRole('button', { name: 'Sincronizar y abrir' }))

    expect(await screen.findByText('Sincronización incompleta')).toBeInTheDocument()
    expect(screen.getByRole('table', { name: 'Historial de sincronizaciones de AGIL-4B' })).toBeInTheDocument()
    expect(screen.getAllByText('Moodle no respondió').length).toBeGreaterThan(0)
    expect(screen.queryByTestId('ruta')).toBeNull()
  })

  it('cuando Moodle no responde (502) lo dice en la pantalla', async () => {
    obtenerCursos.mockResolvedValue([agil])
    sincronizarCurso.mockRejectedValue({ response: { status: 502 } })
    renderCursos()

    fireEvent.click(await screen.findByRole('button', { name: 'Sincronizar y abrir' }))

    expect(await screen.findByText(/Moodle no responde; intenta más tarde/)).toBeInTheDocument()
  })

  it('«Ver historial» abre el historial de sincronizaciones en una tabla', async () => {
    obtenerCursos.mockResolvedValue([agil])
    obtenerHistorialSincronizaciones.mockResolvedValue([
      { id: 5, estado: 'COMPLETADA', inicio: '2026-11-21T09:14:00-05:00', fin: '2026-11-21T09:15:00-05:00', registros: { unidades: 4, actividades: 19 }, errores: [] },
    ])
    renderCursos()

    fireEvent.click(await screen.findByRole('button', { name: 'Ver historial' }))

    const tabla = await screen.findByRole('table', { name: 'Historial de sincronizaciones de AGIL-4B' })
    expect(within(tabla).getByText('23')).toBeInTheDocument()
    expect(within(tabla).getByText('Sin errores')).toBeInTheDocument()
  })

  it('sin cursos asignados lo dice y si falla la carga avisa', async () => {
    obtenerCursos.mockResolvedValue([])
    renderCursos()
    expect(await screen.findByText('Aún no tienes cursos')).toBeInTheDocument()
    cleanup()

    obtenerCursos.mockRejectedValue(new Error('caída'))
    renderCursos()
    expect(await screen.findByText('No fue posible cargar los cursos')).toBeInTheDocument()
  })

  it('cierra la sesión desde la barra lateral', async () => {
    obtenerCursos.mockResolvedValue([])
    renderCursos()
    await screen.findByText('Aún no tienes cursos')

    fireEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }))

    expect(cerrarSesion).toHaveBeenCalledOnce()
  })
})
