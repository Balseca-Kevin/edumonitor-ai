import { afterEach, describe, expect, it } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import CabeceraCurso, { LineaTiempo } from './CabeceraCurso'

const estilos = readFileSync(resolve(process.cwd(), 'src/styles/app.css'), 'utf8').replaceAll('\r\n', '\n')

const curso = {
  id: 1,
  nombreCorto: 'AGIL-4B',
  nombre: 'Metodologías Ágiles (lab)',
  periodo: '2026-2',
  ultimaSincronizacion: { id: 12, fecha: '2026-11-21T07:10:00-05:00', estado: 'COMPLETADA' },
}

// Forma de GET /api/cursos/{id}/periodo (docs/contratos-api.md, «Estado del período»); los valores son de la prueba.
const periodo = {
  cursoId: 1,
  hoy: '2026-11-22T10:00:00-05:00',
  semana: { actual: 6, total: 8 },
  estado: 'EN_CURSO',
  unidadActual: { numero: 2, nombre: 'Filosofía y enfoques ágiles de la actualidad', parcial: 2 },
  unidades: [
    { numero: 1, nombre: 'Principios fundamentales de las metodologías ágiles', parcial: 1, semanaDesde: 1, semanaHasta: 4 },
    { numero: 2, nombre: 'Filosofía y enfoques ágiles de la actualidad', parcial: 2, semanaDesde: 5, semanaHasta: 8 },
  ],
  actividades: [
    { id: 101, nombre: 'Tarea 1', sigla: 'T1', categoria: 'Tareas', unidad: 1, fechaLimite: '2026-10-08T23:59:00-05:00', semana: 2, estado: 'VENCIDA', matriculados: 3, entregaron: 3, faltan: 0 },
    { id: 102, nombre: 'Tarea 2', sigla: 'T2', categoria: 'Tareas', unidad: 1, fechaLimite: '2026-10-22T23:59:00-05:00', semana: 4, estado: 'VENCIDA', matriculados: 3, entregaron: 2, faltan: 1 },
    { id: 103, nombre: 'Examen Parcial 1', sigla: 'EX P1', categoria: 'Exámenes', unidad: 1, fechaLimite: '2026-10-22T18:59:00-05:00', semana: 4, estado: 'VENCIDA', matriculados: 3, entregaron: 3, faltan: 0 },
    { id: 104, nombre: 'APE 3', sigla: 'APE3', categoria: 'APE', unidad: 2, fechaLimite: '2026-11-25T23:59:00-05:00', semana: 6, estado: 'VENCE_ESTA_SEMANA', matriculados: 3, entregaron: 1, faltan: 0 },
    { id: 105, nombre: 'Tarea 9', sigla: 'T9', categoria: 'Tareas', unidad: 2, fechaLimite: '2026-12-02T23:59:00-05:00', semana: 7, estado: 'PROXIMA', matriculados: 3, entregaron: 0, faltan: 0 },
    { id: 106, nombre: 'Lectura', sigla: 'L', categoria: 'Tareas', unidad: 2, fechaLimite: null, semana: null, estado: 'SIN_FECHA', matriculados: 3, entregaron: 0, faltan: 0 },
  ],
  vencidas: { actividades: 3, entregasQueFaltan: 1, conFaltas: [{ id: 102, nombre: 'Tarea 2', faltan: 1 }] },
  venceEstaSemana: [{ id: 104, nombre: 'APE 3', fechaLimite: '2026-11-25T23:59:00-05:00', entregaron: 1, matriculados: 3 }],
  siguiente: { id: 105, nombre: 'Tarea 9', categoria: 'Tareas', semana: 7, fechaLimite: '2026-12-02T23:59:00-05:00', temas: ['Scrum', 'Kanban'] },
  noDisponibles: [],
}

afterEach(cleanup)

const abrirPanel = async (nombre = 'Ver período') => {
  fireEvent.click(screen.getByRole('button', { name: nombre }))
  return screen.findByRole('dialog', { name: 'Período' })
}

describe('C6 · Barra de contexto del curso', () => {
  it('muestra en una línea el código, el nombre, la semana, la unidad y los chips que aplican, sin título grande', () => {
    render(<CabeceraCurso curso={curso} periodo={periodo} />)
    const barra = screen.getByLabelText('Curso AGIL-4B')

    expect(barra).toHaveClass('contexto-curso')
    expect(within(barra).getByText('AGIL-4B')).toHaveClass('mono')
    expect(within(barra).getByText('Metodologías Ágiles (lab)')).toHaveClass('contexto-nombre')
    expect(within(barra).getByLabelText('Semana 6 de 8')).toHaveTextContent('Semana 6 de 8')
    expect(within(barra).getByText('U2 · Filosofía y enfoques ágiles de la actualidad')).toHaveClass('contexto-unidad')
    expect(within(barra).getByRole('button', { name: 'Vence esta semana: APE 3' })).toBeInTheDocument()
    expect(within(barra).getByRole('button', { name: '3 vencidas' })).toBeInTheDocument()
    expect(within(barra).getByRole('button', { name: 'Ver período' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { level: 1 })).not.toBeInTheDocument()
    // La línea de tiempo no está en la barra: solo en el panel «Período» y en el Panel del curso.
    expect(screen.queryByRole('group', { name: /Línea de tiempo/ })).not.toBeInTheDocument()
  })

  it('sin evaluaciones esta semana ni vencidas no muestra esos chips', () => {
    render(<CabeceraCurso curso={curso} periodo={{ ...periodo, venceEstaSemana: [], vencidas: { actividades: 0, entregasQueFaltan: 0, conFaltas: [] } }} />)
    expect(screen.queryByRole('button', { name: /Vence esta semana/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /vencidas?$/ })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Ver período' })).toBeInTheDocument()
  })

  it('con varias evaluaciones esta semana nombra la primera y cuántas más; con una vencida usa el singular', () => {
    const dos = [...periodo.venceEstaSemana, { id: 107, nombre: 'Tarea 8', fechaLimite: '2026-11-26T23:59:00-05:00', entregaron: 0, matriculados: 3 }]
    render(<CabeceraCurso curso={curso} periodo={{ ...periodo, venceEstaSemana: dos, vencidas: { ...periodo.vencidas, actividades: 1 } }} />)
    expect(screen.getByRole('button', { name: 'Vence esta semana: APE 3 y 1 más' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '1 vencida' })).toBeInTheDocument()
  })

  it('el nombre completo del curso está en un tooltip al enfocarlo', async () => {
    render(<CabeceraCurso curso={curso} periodo={periodo} />)
    fireEvent.focus(screen.getByText('Metodologías Ágiles (lab)', { selector: '.contexto-nombre' }))
    expect(await screen.findByRole('tooltip')).toHaveTextContent('Metodologías Ágiles (lab)')
  })

  it('«Ver período» abre el panel «Período» con la línea de tiempo, los tres bloques y la leyenda; Esc lo cierra', async () => {
    render(<CabeceraCurso curso={curso} periodo={periodo} />)

    const panel = await abrirPanel()
    expect(within(panel).getByRole('group', { name: 'Línea de tiempo del período: semana 6 de 8' })).toBeInTheDocument()
    expect(within(panel).getByRole('region', { name: 'Vencidas' })).toHaveTextContent('3 actividades')
    expect(within(panel).getByRole('region', { name: 'Vencidas' })).toHaveTextContent('1 entrega falta: Tarea 2 (1)')
    expect(within(panel).getByRole('region', { name: 'Vence esta semana' })).toHaveTextContent('1 de 3 entregaron')
    expect(within(panel).getByRole('region', { name: 'Siguiente' })).toHaveTextContent('Temas: Scrum, Kanban')
    expect(within(panel).getByLabelText('Leyenda de la línea de tiempo')).toBeInTheDocument()

    fireEvent.keyDown(panel, { key: 'Escape' })
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Período' })).not.toBeInTheDocument())
  })

  it.each([['Vence esta semana: APE 3'], ['3 vencidas']])('el chip «%s» abre el panel «Período»', async (chip) => {
    render(<CabeceraCurso curso={curso} periodo={periodo} />)
    expect(await abrirPanel(chip)).toBeInTheDocument()
  })

  it('el botón de cerrar también cierra el panel', async () => {
    render(<CabeceraCurso curso={curso} periodo={periodo} />)
    const panel = await abrirPanel()
    fireEvent.click(within(panel).getByRole('button', { name: 'Cerrar el período' }))
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Período' })).not.toBeInTheDocument())
  })

  it('sin sincronizar dice «Sin sincronizar» y no ofrece el panel', () => {
    render(<CabeceraCurso curso={{ ...curso, ultimaSincronizacion: null }} periodo={null} />)
    expect(screen.getByText('Sin sincronizar')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Ver período' })).not.toBeInTheDocument()
    expect(screen.queryByText(/Semana \d/)).not.toBeInTheDocument()
  })

  it('sincronizado pero sin período dice «Período no disponible», nunca una semana inventada', () => {
    render(<CabeceraCurso curso={curso} periodo={null} />)
    expect(screen.getByText('Período no disponible')).toBeInTheDocument()
    expect(screen.queryByText(/Semana \d/)).not.toBeInTheDocument()
  })

  it('con el período terminado lo dice en la barra y en el panel', async () => {
    const terminado = { ...periodo, estado: 'TERMINADO', semana: { actual: 8, total: 8 }, venceEstaSemana: [], siguiente: null }
    render(<CabeceraCurso curso={curso} periodo={terminado} />)
    expect(screen.getByText('Período terminado')).toBeInTheDocument()
    expect(screen.queryByText(/^U2 ·/)).not.toBeInTheDocument()
    const panel = await abrirPanel()
    expect(within(panel).getByText(/el período terminó/)).toBeInTheDocument()
    expect(within(panel).getByRole('region', { name: 'Siguiente' })).toHaveTextContent('No quedan evaluaciones')
    expect(within(panel).getByRole('region', { name: 'Vence esta semana' })).toHaveTextContent('Nada vence esta semana')
  })

  it('sin fecha de inicio dice «Semana no disponible» y el panel queda sin línea de tiempo, con los bloques', async () => {
    render(<CabeceraCurso curso={curso} periodo={{ ...periodo, semana: null, noDisponibles: ['inicio'] }} />)
    expect(screen.getByText('Semana no disponible', { selector: '.contexto-semana' })).toBeInTheDocument()
    const panel = await abrirPanel()
    expect(within(panel).queryByRole('group')).not.toBeInTheDocument()
    expect(within(panel).getByRole('region', { name: 'Vencidas' })).toBeInTheDocument()
  })
})

describe('C6 · Línea de tiempo ligera', () => {
  it('un marcador enfocable por evaluación con fecha, con nombre accesible igual al tooltip; la que no tiene fecha no se dibuja', () => {
    render(<LineaTiempo periodo={periodo} />)
    const linea = screen.getByRole('group', { name: 'Línea de tiempo del período: semana 6 de 8' })

    expect(within(linea).getAllByRole('button')).toHaveLength(5)
    expect(within(linea).getByRole('button', { name: 'Tarea 1 · vence 08/10 · 3 de 3 entregaron' })).toHaveClass('lt2-marcador--completa')
    expect(within(linea).getByRole('button', { name: 'Tarea 2 · vence 22/10 · 2 de 3 entregaron' })).toHaveClass('lt2-marcador--faltan')
    expect(within(linea).getByRole('button', { name: 'Examen Parcial 1 · vence 22/10 · 3 de 3 entregaron' })).toHaveClass('lt2-marcador--rombo')
    expect(within(linea).getByRole('button', { name: 'APE 3 · vence 25/11 · 1 de 3 entregaron' })).toHaveClass('lt2-marcador--esta-semana')
    expect(within(linea).getByRole('button', { name: 'Tarea 9 · vence 02/12 · próxima' })).toHaveClass('lt2-marcador--proxima')
    expect(within(linea).queryByRole('button', { name: /Lectura/ })).not.toBeInTheDocument()
    expect(within(linea).getByRole('img', { name: 'Hoy, semana 6' })).toBeInTheDocument()
    expect(within(linea).getByText('PARCIAL 1')).toBeInTheDocument()
    expect(within(linea).getByText('PARCIAL 2')).toBeInTheDocument()
    expect(linea.querySelector('.lt2-unidad--actual')).toHaveTextContent('U2 · Filosofía y enfoques ágiles de la actualidad')
    // Sin etiquetas fijas bajo los marcadores: la sigla no aparece en la línea.
    expect(within(linea).queryByText('T1')).not.toBeInTheDocument()
  })

  it('al enfocar un marcador con el teclado aparece su tooltip', async () => {
    render(<LineaTiempo periodo={periodo} />)
    const marcador = screen.getByRole('button', { name: 'Tarea 2 · vence 22/10 · 2 de 3 entregaron' })
    marcador.focus()
    expect(marcador).toHaveFocus()
    fireEvent.focus(marcador)
    expect(await screen.findByRole('tooltip')).toHaveTextContent('Tarea 2 · vence 22/10 · 2 de 3 entregaron')
  })

  it('la versión del Panel es compacta: sin parciales a la vista, sin nombres de unidad ni rango de semanas', () => {
    render(<LineaTiempo periodo={periodo} compacta />)
    const linea = screen.getByRole('group', { name: 'Línea de tiempo del período: semana 6 de 8' })
    expect(linea).toHaveClass('lt2--compacta')
    expect(within(linea).queryByText(/Principios fundamentales/)).not.toBeInTheDocument()
    expect(within(linea).queryByText('Semana 1')).not.toBeInTheDocument()
    expect(within(linea).getAllByRole('button')).toHaveLength(5)
  })

  it('las animaciones existen, la barra mide 48 px, la línea del Panel 80 px y prefers-reduced-motion las deja quietas', () => {
    expect(estilos).toMatch(/\.lt2-marcador--esta-semana \{[^}]*animation: anillo-vivo/)
    expect(estilos).toMatch(/\.lt2-relleno \{[^}]*animation: trazar/)
    expect(estilos).toMatch(/\.vivo-punto \{[^}]*animation: respirar/)
    expect(estilos).toMatch(/\.contexto-curso \{[^}]*height: 48px/)
    expect(estilos).toMatch(/\.panel-periodo \{[^}]*max-height: 80px/)
    expect(estilos).toMatch(/@media \(prefers-reduced-motion: reduce\)/)
  })
})
