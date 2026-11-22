import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import CursoMarco from './CursoMarco'
import Asistente from './Asistente'
import { datoUsado, estudiantesMencionados, motivo, separarRespuesta } from '../../components/lenguaje'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'

// Vitest no procesa el CSS: se lee el archivo para comprobar las reglas de alto y desplazamiento.
const estilos = readFileSync(resolve(process.cwd(), 'src/styles/app.css'), 'utf8').replaceAll('\r\n', '\n')

vi.mock('../../services/agente', () => ({ preguntarAlAsistente: vi.fn(), listarConversaciones: vi.fn(), obtenerConversacion: vi.fn() }))
vi.mock('../../services/cursos', () => ({ obtenerCurso: vi.fn(), sincronizarCurso: vi.fn() }))
vi.mock('../../services/indicadores', () => ({ obtenerIndicadores: vi.fn(), contarEstudiantesConAlertas: vi.fn() }))
vi.mock('../../services/alertas', () => ({ obtenerAlertas: vi.fn() }))
vi.mock('../../services/libro', async (original) => ({ ...(await original()), obtenerLibro: vi.fn() }))
vi.mock('../../components/TiraPeriodo', () => ({ default: () => null }))

import { listarConversaciones, obtenerConversacion, preguntarAlAsistente } from '../../services/agente'
import { obtenerCurso } from '../../services/cursos'
import { contarEstudiantesConAlertas, obtenerIndicadores } from '../../services/indicadores'
import { obtenerAlertas } from '../../services/alertas'
import { obtenerLibro } from '../../services/libro'

beforeAll(() => {
  globalThis.ResizeObserver ??= class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
})

const curso = {
  id: 1,
  moodleId: 2,
  nombreCorto: 'AGIL-4B',
  nombre: 'Metodologías Ágiles (lab)',
  periodo: '2026-2',
  ultimaSincronizacion: { id: 12, fecha: '2026-11-09T07:10:00-05:00', estado: 'COMPLETADA' },
}

// Estudiantes de GET /api/cursos/{id}/indicadores (docs/contratos-api.md).
const estudiantes = [
  { id: 5, nombre: 'Karina', apellido: 'Lara', promedioPonderado: 5.29, notas: { calificadas: 12, ceros: 0, sinEntrega: 4, pendientes: 0 }, tendencia: -0.2, categorias: [] },
  { id: 6, nombre: 'Elena', apellido: 'Espinoza', promedioPonderado: 8.61, notas: { calificadas: 12, ceros: 1, sinEntrega: 4, pendientes: 0 }, tendencia: 0.1, categorias: [] },
]
const unidades = [{ numero: 3, nombre: 'Principales metodologías ágiles del mercado', parcial: 2, actividades: 4, calificadas: 4 }]

// Respuesta del contrato de POST /api/agente/chat.
const respuestaChat = {
  conversacionId: 44,
  respuesta: 'Karina Lara tiene un promedio de 5,29 y requiere atención.',
  herramientas: [{ nombre: 'obtener_calificaciones', parametros: { cursoId: 1, nombre: 'Karina Lara' }, estado: 'OK', duracionMs: 62 }],
  modelo: 'qwen2.5:7b',
  duracionMs: 10000,
}

function errorHttp(status, data = {}) {
  return Object.assign(new Error(`HTTP ${status}`), { response: { status, data } })
}

function renderAsistente(ruta = '/cursos/1/asistente') {
  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <Routes>
        <Route path="/cursos/:id" element={<CursoMarco />}>
          <Route path="asistente" element={<Asistente />} />
        </Route>
        <Route path="/cursos" element={<p>Mis cursos</p>} />
      </Routes>
    </MemoryRouter>,
  )
}

async function preguntar(texto) {
  fireEvent.change(await screen.findByLabelText('Tu pregunta'), { target: { value: texto } })
  fireEvent.click(screen.getByRole('button', { name: 'Enviar pregunta' }))
}

beforeEach(() => {
  sessionStorage.setItem('edumonitor_usuario', JSON.stringify({ id: 2, usuario: 'doc01', nombre: 'Docente Uno', rol: 'DOCENTE' }))
  obtenerCurso.mockResolvedValue(curso)
  listarConversaciones.mockResolvedValue([])
  obtenerIndicadores.mockResolvedValue({ unidades, estudiantes })
  obtenerAlertas.mockResolvedValue([{ id: 31, regla: 'R4', estudiante: { id: 5 } }])
  contarEstudiantesConAlertas.mockResolvedValue(1)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  sessionStorage.clear()
})

describe('R.6 · Asistente dentro del curso', () => {
  it('va dentro de la estructura del curso, con el contexto visible y la bienvenida', async () => {
    renderAsistente('/cursos/1/asistente?tramo=u3')

    expect(await screen.findByRole('heading', { name: 'Hola, Docente Uno. ¿Qué quieres saber de AGIL-4B?' })).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: 'Curso AGIL-4B' })).toBeInTheDocument() // barra lateral oscura del curso
    expect(await screen.findByText('Sabe: Unidad 3 · Principales metodologías ágiles del mercado')).toBeInTheDocument()
    expect(obtenerIndicadores).toHaveBeenCalledWith('1', 'u3')
    expect(screen.queryByText(/E-0\d\d|Nada se envía/)).toBeNull() // las reglas del sistema no se escriben en pantalla
  })

  it('responde con la tarjeta del estudiante nombrado y «Cómo llegó a esta respuesta» cerrado, sin nombres de funciones', async () => {
    preguntarAlAsistente.mockResolvedValue(respuestaChat)
    renderAsistente()

    await preguntar('¿Cómo le va a Karina Lara?')

    expect(await screen.findByText('Karina Lara tiene un promedio de 5,29 y requiere atención.')).toBeInTheDocument()
    expect(preguntarAlAsistente).toHaveBeenCalledWith('1', '¿Cómo le va a Karina Lara?')
    const tarjetas = screen.getByLabelText('Estudiantes mencionados')
    expect(within(tarjetas).getByText('Lara, Karina')).toBeInTheDocument()
    expect(within(tarjetas).getByText('5,29')).toBeInTheDocument()
    expect(within(tarjetas).getByText('4 sin entregar')).toBeInTheDocument()
    expect(within(tarjetas).queryByText(/Espinoza/)).toBeNull()

    const como = screen.getByText('Cómo llegó a esta respuesta').closest('details')
    expect(como).not.toHaveAttribute('open')
    expect(como).toHaveTextContent('Respondió: modelo local · qwen2.5:7b')
    expect(como).toHaveTextContent('Notas de Karina Lara · 62 ms · curso 1 · nombre Karina Lara')
    expect(como).toHaveTextContent('Tiempo total: 10,0 s')
    expect(como).toHaveTextContent('Datos de la sincronización del 09/11/2026')
    expect(como).not.toHaveTextContent('obtener_calificaciones')
    expect(screen.getByRole('navigation', { name: 'Conversaciones' })).toHaveTextContent('¿Cómo le va a Karina Lara?')
  })

  it('separa Hechos, Inferencia y Recomendación cuando el asistente los escribe', async () => {
    preguntarAlAsistente.mockResolvedValue({
      ...respuestaChat,
      respuesta: 'Hechos: Karina Lara tiene 5,29.\nInferencia: le cuestan las tareas.\nRecomendación: revisar con ella la Tarea 5.',
    })
    renderAsistente()

    await preguntar('¿Cómo va Karina?')

    expect(await screen.findByText('Karina Lara tiene 5,29.')).toBeInTheDocument()
    expect(screen.getByText('le cuestan las tareas.')).toBeInTheDocument()
    expect(screen.getByText('Recomendación').nextSibling).toHaveTextContent('revisar con ella la Tarea 5.')
    expect(separarRespuesta('Sin títulos')).toBeNull()
  })

  it('«Abrir ficha» abre la ficha lateral del estudiante con su estado', async () => {
    preguntarAlAsistente.mockResolvedValue(respuestaChat)
    obtenerLibro.mockResolvedValue({ parciales: [], estudiantes: [] })
    renderAsistente()
    await preguntar('¿Cómo le va a Karina Lara?')

    fireEvent.click(await screen.findByRole('button', { name: 'Abrir ficha' }))

    const ficha = await screen.findByRole('dialog', { name: 'Ficha de Lara, Karina' })
    expect(within(ficha).getByText('Requiere atención')).toBeInTheDocument()
  })

  it('desde la ficha llega con el estudiante en el contexto y la pregunta escrita', async () => {
    renderAsistente('/cursos/1/asistente?estudiante=6')

    expect(await screen.findByText(/Sabe: Todo el ciclo · Elena Espinoza/)).toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText('Tu pregunta')).toHaveValue('¿Cómo va Elena Espinoza? ¿En qué actividades le fue mal?'))
    expect(preguntarAlAsistente).not.toHaveBeenCalled() // el docente la revisa y la envía
  })

  it('mientras consulta muestra «Consultando…» y deshabilita el envío', async () => {
    let responder
    preguntarAlAsistente.mockReturnValue(new Promise((r) => (responder = r)))
    renderAsistente()

    await preguntar('¿Qué estudiantes requieren atención?')

    expect(await screen.findByText('Consultando…')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Enviar pregunta' })).toBeDisabled()
    responder(respuestaChat)
    await waitFor(() => expect(screen.queryByText('Consultando…')).not.toBeInTheDocument())
  })

  it('con 503 muestra «El asistente no está disponible»', async () => {
    preguntarAlAsistente.mockRejectedValue(errorHttp(503, { title: 'Asistente no disponible', detail: 'El asistente no respondió en 60 segundos.' }))
    renderAsistente()

    await preguntar('¿Qué estudiantes requieren atención?')

    const aviso = await screen.findByRole('alert')
    expect(aviso).toHaveTextContent('El asistente no está disponible')
    expect(aviso).toHaveTextContent('no respondió en 60 segundos')
  })

  it('el historial sale de la base con su fecha, abre una conversación guardada y «Nueva conversación» la limpia', async () => {
    listarConversaciones.mockResolvedValue([
      { id: 48, fecha: '2026-11-21T10:00:00-05:00', pregunta: '¿Qué estudiantes requieren atención?', modelo: 'qwen2.5:7b', duracionMs: 23100, herramientas: 1 },
      { id: 12, fecha: '2026-11-05T10:00:00-05:00', pregunta: '¿Qué cursos tengo asignados?', modelo: 'qwen2.5:7b', duracionMs: 5000, herramientas: 1 },
    ])
    obtenerConversacion.mockResolvedValue({
      id: 12,
      cursoId: 1,
      fecha: '2026-11-05T10:00:00-05:00',
      pregunta: '¿Qué cursos tengo asignados?',
      respuesta: 'Tienes asignado el curso AGIL-4B.',
      modelo: 'qwen2.5:7b',
      duracionMs: 5000,
      herramientas: [{ nombre: 'listar_cursos_autorizados', parametros: {}, estado: 'OK', duracionMs: 41 }],
    })
    renderAsistente()

    const historial = await screen.findByRole('navigation', { name: 'Conversaciones' })
    expect(await within(historial).findByText(/05\/11\/2026/)).toBeInTheDocument()
    expect(listarConversaciones).toHaveBeenCalledWith('1')

    fireEvent.click(within(historial).getByRole('button', { name: /¿Qué cursos tengo asignados\?/ }))
    expect(await screen.findByText('Tienes asignado el curso AGIL-4B.')).toBeInTheDocument()
    expect(obtenerConversacion).toHaveBeenCalledWith(12)
    expect(within(historial).getByRole('button', { name: /¿Qué cursos tengo asignados\?/ })).toHaveAttribute('aria-current', 'true')

    fireEvent.click(screen.getByRole('button', { name: 'Nueva conversación' }))
    expect(screen.queryByText('Tienes asignado el curso AGIL-4B.')).not.toBeInTheDocument()
  })

  it('con 60 conversaciones solo el historial y los mensajes se desplazan; el cuadro de la pregunta sigue en el pie', async () => {
    listarConversaciones.mockResolvedValue(
      Array.from({ length: 60 }, (_, i) => ({ id: 100 + i, fecha: '2026-11-20T10:00:00-05:00', pregunta: `Pregunta guardada número ${i + 1}`, modelo: 'qwen2.5:7b', duracionMs: 10000, herramientas: 1 })),
    )
    renderAsistente()

    const historial = await screen.findByRole('navigation', { name: 'Conversaciones' })
    await within(historial).findByText('Pregunta guardada número 60')
    expect(within(historial).getAllByRole('button')).toHaveLength(60)
    expect(historial).not.toContainElement(screen.getByRole('button', { name: 'Nueva conversación' }))

    const campo = screen.getByLabelText('Tu pregunta')
    const chat = campo.closest('.asistente-chat')
    expect(chat.lastElementChild).toBe(campo.closest('.chat-pie'))
    expect(chat.querySelector('.chat-cuerpo')).not.toContainElement(campo)

    const regla = (selector) => {
      const inicio = estilos.indexOf(`\n${selector} {`)
      return inicio < 0 ? '' : estilos.slice(inicio, estilos.indexOf('}', inicio))
    }
    expect(regla('.asistente-chat')).toMatch(/height: 78vh/)
    expect(regla('.asistente-historial nav')).toMatch(/overflow-y: auto/)
    expect(regla('.chat-cuerpo')).toMatch(/overflow-y: auto/)
    expect(regla('.chat-pie')).toMatch(/flex-shrink: 0/)
  })

  it('un curso ajeno muestra «Acceso no permitido» en pantalla completa', async () => {
    obtenerCurso.mockRejectedValue(errorHttp(403, { detail: 'El curso 2 no está asignado a este docente.' }))
    renderAsistente()

    expect(await screen.findByRole('heading', { name: 'Acceso no permitido' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Tu pregunta')).not.toBeInTheDocument()
  })

  it('una sesión vencida muestra «Tu sesión venció»', async () => {
    obtenerCurso.mockRejectedValue(errorHttp(401))
    renderAsistente()

    expect(await screen.findByRole('heading', { name: 'Tu sesión venció' })).toBeInTheDocument()
  })
})

describe('Lenguaje del asistente', () => {
  it('los datos usados dicen qué se consultó, sin nombres de funciones', () => {
    expect(datoUsado({ nombre: 'obtener_calificaciones', parametros: { cursoId: 1 }, estado: 'OK', duracionMs: 43 }, [])).toBe(
      'Calificaciones del curso · 43 ms',
    )
    expect(
      datoUsado({ nombre: 'obtener_calificaciones', parametros: { cursoId: 1, estudianteId: 6 }, estado: 'OK', duracionMs: 9 }, estudiantes),
    ).toBe('Notas de Elena Espinoza · 9 ms')
    expect(datoUsado({ nombre: 'identificar_alertas', parametros: { cursoId: 1 }, estado: 'ERROR', duracionMs: 5000 }, [])).toBe(
      'Alertas del curso · 5000 ms · no se pudo consultar',
    )
  })

  it('encuentra a los estudiantes nombrados sin importar tildes ni mayúsculas, en el orden del texto', () => {
    const texto = 'Primero ELENA ESPINOZA y después Karina Lára.'
    expect(estudiantesMencionados(texto, estudiantes).map((e) => e.nombre)).toEqual(['Elena', 'Karina'])
  })

  it('el motivo usa los conteos reales', () => {
    expect(motivo(estudiantes[1])).toBe('1 nota en cero · 4 sin entregar')
  })
})
