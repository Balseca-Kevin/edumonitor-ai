import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import CursoMarco from './CursoMarco'
import { useTramo } from '../../components/tramo'

vi.mock('../../services/cursos', () => ({ obtenerCurso: vi.fn(), sincronizarCurso: vi.fn(), obtenerCambiosSincronizacion: vi.fn() }))
vi.mock('../../services/libro', () => ({ obtenerLibro: vi.fn() }))
vi.mock('../../services/indicadores', () => ({ obtenerIndicadores: vi.fn(), contarEstudiantesConAlertas: vi.fn() }))
vi.mock('../../services/periodo', () => ({ obtenerPeriodo: vi.fn() }))

import { obtenerCambiosSincronizacion, obtenerCurso, sincronizarCurso } from '../../services/cursos'
import { obtenerLibro } from '../../services/libro'
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

function renderCurso(ruta = '/cursos/1/panel', estado) {
  return render(
    <MemoryRouter initialEntries={[estado ? { pathname: ruta.split('?')[0], search: ruta.includes('?') ? `?${ruta.split('?')[1]}` : '', state: estado } : ruta]}>
      <Routes>
        <Route path="/cursos/:id" element={<CursoMarco />}>
          <Route path="panel" element={<PantallaDePrueba />} />
          <Route path="alertas" element={<PantallaDePrueba />} />
          <Route path="libro" element={<PantallaDePrueba />} />
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
  vi.clearAllMocks()
  sessionStorage.clear()
})

describe('R.2 · Barra lateral oscura y barra del curso', () => {
  it('muestra la navegación del curso con el contador de estudiantes que requieren atención', async () => {
    renderCurso()

    const nav = await screen.findByRole('navigation', { name: 'Curso AGIL-4B' })
    expect(within(nav).getAllByRole('link').map((a) => a.textContent)).toEqual(['Libro de calificaciones', 'Panel del curso', 'Alertas3', 'Asistente'])
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

// Forma de GET /api/cursos/{id}/sync/{sid}/cambios (docs/contratos-api.md, «Cambios entre sincronizaciones»).
const cambios = {
  sincronizacionId: 13,
  cursoId: 1,
  anteriorId: 12,
  conteos: { total: 4, notasNuevas: 2, notasCambiadas: 1, entregasNuevas: 1, otros: 0 },
  celdas: [],
  noDisponibles: [],
}

describe('TA-21.A2 · Pantalla de carga y aviso de sincronización en el curso', () => {
  it('«Abrir curso» muestra «Abriendo curso…» y cada paso termina cuando responde su consulta real', async () => {
    let responderLibro
    obtenerLibro.mockReturnValue(new Promise((listo) => (responderLibro = listo)))
    renderCurso('/cursos/1/panel', { abrir: true })

    const carga = screen.getByRole('dialog', { name: 'Abriendo curso…' })
    expect(within(carga).getByText('Con los datos de la última sincronización.')).toBeInTheDocument()
    await waitFor(() => expect(within(carga).getByText('Estado del período').closest('li')).toHaveClass('carga-paso--listo'))
    expect(within(carga).getByText('Indicadores y alertas').closest('li')).toHaveClass('carga-paso--listo')
    expect(within(carga).getByText('Libro de calificaciones').closest('li')).toHaveClass('carga-paso--girando')
    expect(obtenerLibro).toHaveBeenCalledWith('1')

    responderLibro({ parciales: [], estudiantes: [] })
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Abriendo curso…' })).toBeNull())
    expect(screen.getByTestId('pantalla')).toBeInTheDocument()
  })

  it('sin venir de «Abrir curso» no muestra la pantalla de carga ni pide el libro', async () => {
    renderCurso()
    expect(await screen.findByTestId('pantalla')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).toBeNull()
    expect(obtenerLibro).not.toHaveBeenCalled()
  })

  it('al llegar desde «Sincronizar y abrir» baja el aviso con las cifras reales de los cambios', async () => {
    obtenerCambiosSincronizacion.mockResolvedValue(cambios)
    contarEstudiantesConAlertas.mockResolvedValue(5)
    renderCurso('/cursos/1/panel', { sincronizacion: { id: 13, estado: 'COMPLETADA', errores: [] } })

    const aviso = await screen.findByRole('status', { name: 'Resultado de la sincronización' })
    expect(within(aviso).getByText('SINCRONIZACIÓN COMPLETADA')).toBeInTheDocument()
    expect(await within(aviso).findByText('AGIL-4B tiene datos nuevos del aula virtual')).toBeInTheDocument()
    expect(obtenerCambiosSincronizacion).toHaveBeenCalledWith('1', 13)
    expect(within(aviso).getByText('notas nuevas').previousSibling).toHaveTextContent('2')
    expect(within(aviso).getByText('entregas por calificar').previousSibling).toHaveTextContent('1')
    await waitFor(() => expect(within(aviso).getByText('requieren atención').previousSibling).toHaveTextContent('5'))
    expect(within(aviso).getByText('errores').previousSibling).toHaveTextContent('0')
    expect(within(aviso).getByText('Además, 1 nota cambió frente a la sincronización anterior.')).toBeInTheDocument()

    fireEvent.click(within(aviso).getByRole('button', { name: 'Cerrar' }))
    expect(screen.queryByRole('status', { name: 'Resultado de la sincronización' })).toBeNull()
  })

  it('«Ver cambios en el libro» cierra el aviso y abre el libro con el tramo elegido', async () => {
    obtenerCambiosSincronizacion.mockResolvedValue(cambios)
    renderCurso('/cursos/1/panel?tramo=u2', { sincronizacion: { id: 13, estado: 'COMPLETADA', errores: [] } })

    const aviso = await screen.findByRole('status', { name: 'Resultado de la sincronización' })
    fireEvent.click(await within(aviso).findByRole('button', { name: 'Ver cambios en el libro' }))

    expect(screen.queryByRole('status', { name: 'Resultado de la sincronización' })).toBeNull()
    expect(screen.getByTestId('pantalla')).toHaveTextContent('búsqueda «?tramo=u2»')
    expect(within(screen.getByRole('navigation', { name: 'Curso AGIL-4B' })).getByRole('link', { name: 'Libro de calificaciones' })).toHaveClass('activo')
  })

  it('«Sincronizar» en la barra muestra la carga y después el aviso; si fue parcial nombra la función que falló', async () => {
    obtenerCambiosSincronizacion.mockResolvedValue({ ...cambios, conteos: { total: 0, notasNuevas: 0, notasCambiadas: 0, entregasNuevas: 0, otros: 0 } })
    sincronizarCurso.mockResolvedValue({
      id: 14,
      estado: 'PARCIAL',
      errores: [{ funcion: 'core_completion_get_activities_completion_status', mensaje: 'Acceso denegado' }],
    })
    renderCurso()

    fireEvent.click(await screen.findByRole('button', { name: 'Sincronizar' }))
    expect(screen.getByRole('dialog', { name: 'Sincronizando curso…' })).toBeInTheDocument()

    const aviso = await screen.findByRole('status', { name: 'Resultado de la sincronización' }, { timeout: 2000 })
    expect(screen.queryByRole('dialog', { name: 'Sincronizando curso…' })).toBeNull()
    expect(aviso).toHaveClass('aviso-sync--parcial')
    expect(within(aviso).getByText('SINCRONIZACIÓN PARCIAL')).toBeInTheDocument()
    expect(within(aviso).getByText('Falló: core_completion_get_activities_completion_status')).toBeInTheDocument()
    expect(await within(aviso).findByText('Sin cambios desde la sincronización anterior')).toBeInTheDocument()
    expect(within(aviso).getByText('error').previousSibling).toHaveTextContent('1')
  })

  it('si los cambios no se pueden leer, las cifras dicen «no disponible», nunca 0', async () => {
    obtenerCambiosSincronizacion.mockRejectedValue({ response: { status: 500 } })
    renderCurso('/cursos/1/panel', { sincronizacion: { id: 13, estado: 'COMPLETADA', errores: [] } })

    const aviso = await screen.findByRole('status', { name: 'Resultado de la sincronización' })
    await waitFor(() => expect(within(aviso).getByText('notas nuevas').previousSibling).toHaveTextContent('no disponible'))
    expect(within(aviso).getByText('entregas por calificar').previousSibling).toHaveTextContent('no disponible')
  })
})
