import { afterEach, describe, expect, it } from 'vitest'
import { cleanup, render, screen, within } from '@testing-library/react'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import CabeceraCurso from './CabeceraCurso'

// Vitest no procesa el CSS: se lee el archivo para comprobar que las animaciones quedan quietas con prefers-reduced-motion.
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

describe('TA-22.A5 · Cabecera del curso con la línea de tiempo', () => {
  it('muestra el código, el período, los estudiantes, el nombre y la semana actual sobre el total', () => {
    render(<CabeceraCurso curso={curso} periodo={periodo} estudiantes={3} />)

    expect(screen.getByText(/AGIL-4B · Período 2026-2 · 3 estudiantes/)).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'Metodologías Ágiles (lab)' })).toBeInTheDocument()
    expect(screen.getByLabelText('Semana 6 de 8')).toHaveTextContent('6 / 8')
    expect(screen.getByText('U2 · Filosofía y enfoques ágiles de la actualidad', { selector: '.cabecera-unidad' })).toBeInTheDocument()
  })

  it('dibuja las unidades, una semana por número con «HOY n» y un marcador por actividad con fecha, con su estado', () => {
    render(<CabeceraCurso curso={curso} periodo={periodo} estudiantes={3} />)
    const linea = screen.getByRole('group', { name: 'Línea de tiempo del período: semana 6 de 8' })

    expect(within(linea).getByText('PARCIAL 1')).toBeInTheDocument()
    expect(within(linea).getByText('PARCIAL 2')).toBeInTheDocument()
    expect(within(linea).getByText('U2 · Filosofía y enfoques ágiles de la actualidad')).toHaveClass('lt-unidad--actual')
    expect(within(linea).getByText('U1 · Principios fundamentales de las metodologías ágiles')).toHaveClass('lt-unidad--pasada')
    expect(within(linea).getByText('HOY 6')).toHaveClass('lt-semana--hoy')
    expect(linea.querySelectorAll('.lt-semana')).toHaveLength(8)

    // Sin fecha límite no hay marcador: 5 de las 6 actividades.
    const marcadores = within(linea).getAllByRole('img')
    expect(marcadores).toHaveLength(5)
    expect(within(linea).getByRole('img', { name: 'Tarea 1: vencida, todos entregaron' })).toHaveClass('marcador--completa')
    expect(within(linea).getByRole('img', { name: 'Tarea 2: vencida, faltan 1 entregas' })).toHaveClass('marcador--faltan')
    expect(within(linea).getByRole('img', { name: 'Examen Parcial 1: vencida, todos entregaron' })).toHaveClass('marcador--rombo')
    expect(within(linea).getByRole('img', { name: 'APE 3: vence esta semana, 1 de 3 entregaron' })).toHaveClass('marcador--esta-semana')
    expect(within(linea).getByRole('img', { name: 'Tarea 9: próxima' })).toHaveClass('marcador--proxima')
    expect(within(linea).getByText('EX P1')).toHaveClass('lt-sigla--completa')
    expect(within(linea).queryByText('L')).toBeNull()
  })

  it('los tres bloques dicen las vencidas con lo que falta, lo que vence esta semana y la siguiente con sus temas', () => {
    render(<CabeceraCurso curso={curso} periodo={periodo} estudiantes={3} />)

    const vencidas = screen.getByRole('region', { name: 'Vencidas' })
    expect(within(vencidas).getByText('3 actividades')).toBeInTheDocument()
    expect(within(vencidas).getByText('1 entrega falta: Tarea 2 (1)')).toBeInTheDocument()
    const estaSemana = screen.getByRole('region', { name: 'Vence esta semana' })
    expect(within(estaSemana).getByText('APE 3')).toBeInTheDocument()
    expect(within(estaSemana).getByText(/1 de 3 entregaron · vence 25\/11\/2026/)).toBeInTheDocument()
    const siguiente = screen.getByRole('region', { name: 'Siguiente' })
    expect(within(siguiente).getByText('Tarea 9')).toBeInTheDocument()
    expect(within(siguiente).getByText(/Semana 7 · vence 02\/12\/2026.* · Temas: Scrum, Kanban/)).toBeInTheDocument()
    expect(screen.getByLabelText('Leyenda de la línea de tiempo')).toHaveTextContent('Examen')
  })

  it('sin nada que venza ni evaluaciones pendientes lo dice con palabras', () => {
    const terminado = { ...periodo, vencidas: { actividades: 6, entregasQueFaltan: 0, conFaltas: [] }, venceEstaSemana: [], siguiente: null }
    render(<CabeceraCurso curso={curso} periodo={terminado} estudiantes={3} />)

    expect(screen.getByText('Todos entregaron las vencidas.')).toBeInTheDocument()
    expect(screen.getByText('Nada vence esta semana')).toBeInTheDocument()
    expect(screen.getByText('No quedan evaluaciones')).toBeInTheDocument()
  })

  it('sin fecha de inicio no inventa semanas: dice «no disponible» y conserva los bloques', () => {
    const sinInicio = { ...periodo, semana: null, estado: null, noDisponibles: ['inicio'] }
    render(<CabeceraCurso curso={curso} periodo={sinInicio} estudiantes={3} />)

    expect(screen.getByText('no disponible')).toBeInTheDocument()
    expect(screen.queryByRole('group', { name: /Línea de tiempo/ })).toBeNull()
    expect(screen.getByText(/La fecha de inicio del curso no está disponible/)).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'Vencidas' })).toBeInTheDocument()
  })

  it('sin período (curso sin sincronizar) lo dice y no muestra ninguna semana', () => {
    render(<CabeceraCurso curso={{ ...curso, ultimaSincronizacion: null }} periodo={null} estudiantes={null} />)

    expect(screen.getByText(/El estado del período aparece cuando el curso se sincroniza/)).toBeInTheDocument()
    expect(screen.queryByText(/sincronizado/)).toBeNull()
    expect(screen.queryByText(/estudiantes/)).toBeNull()
  })

  it('las animaciones de la línea de tiempo existen y prefers-reduced-motion las deja quietas', () => {
    expect(estilos).toMatch(/\.marcador--esta-semana \{[^}]*animation: anillo-vivo/)
    expect(estilos).toMatch(/\.lt-relleno \{[^}]*animation: trazar/)
    expect(estilos).toMatch(/\.vivo-punto \{[^}]*animation: respirar/)
    expect(estilos).toMatch(/@media \(prefers-reduced-motion: reduce\) \{\n {2}\*, \*::before, \*::after \{ animation-duration: 0ms !important;[^}]*animation-iteration-count: 1 !important/)
  })
})
