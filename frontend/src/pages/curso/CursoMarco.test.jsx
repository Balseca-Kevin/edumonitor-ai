import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import CursoMarco from './CursoMarco'
import { useTramo } from '../../components/tramo'

vi.mock('../../services/cursos', () => ({ obtenerCurso: vi.fn(), sincronizarCurso: vi.fn() }))
vi.mock('../../services/indicadores', () => ({ obtenerIndicadores: vi.fn(), contarEstudiantesConAlertas: vi.fn() }))
vi.mock('../../services/periodo', () => ({ obtenerPeriodo: vi.fn() }))

import { obtenerCurso, sincronizarCurso } from '../../services/cursos'
import { contarEstudiantesConAlertas, obtenerIndicadores } from '../../services/indicadores'
import { obtenerPeriodo } from '../../services/periodo'

// Formas de GET /api/cursos/{id} y de las unidades de GET /api/cursos/{id}/indicadores (docs/contratos-api.md).
const curso = {
  id: 1,
  moodleId: 2,
  nombreCorto: 'AGIL-4B',
  nombre: 'Metodologías Ágiles (lab)',
  periodo: '2026-2',
  ultimaSincronizacion: { id: 12, fecha: '2026-11-21T07:10:00-05:00', estado: 'COMPLETADA' },
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
  venceEstaSemana: [],
  siguiente: null,
  noDisponibles: [],
}
const unidades = [
  { numero: 2, nombre: 'Filosofía y enfoques ágiles de la actualidad', parcial: 1, actividades: 4, calificadas: 4 },
  { numero: 1, nombre: 'Principios fundamentales de las metodologías ágiles', parcial: 1, actividades: 4, calificadas: 4 },
  { numero: 3, nombre: 'Principales metodologías ágiles del mercado', parcial: 2, actividades: 4, calificadas: 4 },
  { numero: 4, nombre: 'Herramientas ágiles del mercado', parcial: 2, actividades: 7, calificadas: 3 },
]

function PantallaDePrueba() {
  const tramo = useTramo()
  const { search } = useLocation()
  return <p data-testid="pantalla">tramo {tramo} · búsqueda «{search}»</p>
}

function renderCurso(ruta = '/cursos/1/panel') {
  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <Routes>
        <Route path="/cursos/:id" element={<CursoMarco />}>
          <Route path="panel" element={<PantallaDePrueba />} />
          <Route path="alertas" element={<PantallaDePrueba />} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  sessionStorage.setItem('edumonitor_usuario', JSON.stringify({ id: 2, usuario: 'doc01', nombre: 'Docente Uno', rol: 'DOCENTE' }))
  obtenerCurso.mockResolvedValue(curso)
  obtenerIndicadores.mockResolvedValue({ unidades })
  contarEstudiantesConAlertas.mockResolvedValue(3)
  obtenerPeriodo.mockResolvedValue(periodo)
})

afterEach(() => {
  cleanup()
  sessionStorage.clear()
})

describe('R.2 · Barra lateral oscura y barra del curso', () => {
  it('muestra la navegación del curso con el contador de estudiantes que requieren atención', async () => {
    renderCurso()

    const nav = await screen.findByRole('navigation', { name: 'Curso AGIL-4B' })
    expect(within(nav).getAllByRole('link').map((a) => a.textContent)).toEqual(['Panel del curso', 'Alertas3', 'Asistente'])
    expect(within(nav).getByLabelText('3 estudiantes requieren atención')).toBeInTheDocument()
    expect(within(nav).getByRole('link', { name: 'Panel del curso' })).toHaveClass('activo')
    const lateral = screen.getByRole('complementary', { name: 'Navegación' })
    expect(within(lateral).getByText('AGIL-4B · 2026-2')).toBeInTheDocument()
    expect(within(lateral).getByText('doc01 · Docente')).toBeInTheDocument()
  })

  it('sin dato de alertas no muestra el contador, nunca un 0', async () => {
    contarEstudiantesConAlertas.mockResolvedValue(null)
    renderCurso()

    const nav = await screen.findByRole('navigation', { name: 'Curso AGIL-4B' })
    expect(within(nav).getByRole('link', { name: 'Alertas' })).toBeInTheDocument()
    expect(within(nav).queryByLabelText(/requieren atención/)).toBeNull()
  })

  it('el control segmentado sale de las unidades reales y el tramo elegido llega a la pantalla y a la navegación', async () => {
    renderCurso()

    const grupo = await screen.findByRole('group', { name: 'Filtrar por parcial o unidad' })
    await waitFor(() => expect(within(grupo).getAllByRole('button')).toHaveLength(7))
    expect(within(grupo).getAllByRole('button').map((b) => b.textContent)).toEqual(['Todo el ciclo', 'Parcial 1', 'Parcial 2', 'U1', 'U2', 'U3', 'U4'])
    expect(within(grupo).getByRole('button', { name: 'Todo el ciclo' })).toHaveAttribute('aria-pressed', 'true')
    expect(within(grupo).getByRole('button', { name: 'U3' })).toHaveAttribute('title', 'Unidad 3 · Principales metodologías ágiles del mercado')

    fireEvent.click(within(grupo).getByRole('button', { name: 'U2' }))

    expect(screen.getByTestId('pantalla')).toHaveTextContent('tramo u2 · búsqueda «?tramo=u2»')
    expect(within(grupo).getByRole('button', { name: 'U2' })).toHaveAttribute('aria-pressed', 'true')
    const nav = screen.getByRole('navigation', { name: 'Curso AGIL-4B' })
    expect(within(nav).getByRole('link', { name: /Alertas/ })).toHaveAttribute('href', '/cursos/1/alertas?tramo=u2')

    fireEvent.click(within(grupo).getByRole('button', { name: 'Todo el ciclo' }))
    expect(screen.getByTestId('pantalla')).toHaveTextContent('tramo todo · búsqueda «»')
  })

  it('un curso sin sincronizar solo ofrece «Todo el ciclo»', async () => {
    obtenerIndicadores.mockRejectedValue({ response: { status: 409 } })
    renderCurso()

    const grupo = await screen.findByRole('group', { name: 'Filtrar por parcial o unidad' })
    await waitFor(() => expect(contarEstudiantesConAlertas).toHaveBeenCalled())
    expect(within(grupo).getAllByRole('button').map((b) => b.textContent)).toEqual(['Todo el ciclo'])
  })

  it('«Sincronizar» llama a la sincronización, avisa el resultado y vuelve a leer el curso', async () => {
    sincronizarCurso.mockResolvedValue({ id: 13, estado: 'PARCIAL', errores: [{ funcion: 'core_completion_get_activities_completion_status', mensaje: 'x' }] })
    renderCurso()

    expect(await screen.findByText(/Datos de la última sincronización · 21\/11\/2026/)).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Sincronizar' }))

    expect(sincronizarCurso).toHaveBeenCalledWith('1')
    expect(await screen.findByText('Sincronización parcial: falló core_completion_get_activities_completion_status')).toBeInTheDocument()
    await waitFor(() => expect(obtenerCurso).toHaveBeenCalledTimes(2))
  })

  it('un tramo inválido en la URL se trata como «Todo el ciclo»', async () => {
    renderCurso('/cursos/1/panel?tramo=semana')
    expect(await screen.findByTestId('pantalla')).toHaveTextContent('tramo todo')
  })
})

describe('TA-22.A5 · Cabecera del curso en el marco', () => {
  it('muestra la semana del período que da el backend y los estudiantes del curso', async () => {
    obtenerIndicadores.mockResolvedValue({ unidades, curso: { estudiantes: 32 } })
    renderCurso()

    expect(await screen.findByLabelText('Semana 12 de 15')).toBeInTheDocument()
    expect(obtenerPeriodo).toHaveBeenCalledWith('1')
    expect(screen.getByText('U4 · Herramientas ágiles del mercado', { selector: '.cabecera-unidad' })).toBeInTheDocument()
  })

  it('si el período no responde, la cabecera lo dice y el resto del curso sigue', async () => {
    obtenerPeriodo.mockRejectedValue({ response: { status: 409 } })
    renderCurso()

    expect(await screen.findByText(/El estado del período aparece cuando el curso se sincroniza/)).toBeInTheDocument()
    expect(screen.getByTestId('pantalla')).toBeInTheDocument()
  })
})
