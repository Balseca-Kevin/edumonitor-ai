import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import CursoMarco from './CursoMarco'
import Asistente from './Asistente'
import { datoUsado, estudiantesMencionados, grupoDeFecha, motivo } from '../../components/lenguaje'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'

// Vitest no procesa el CSS: se lee el archivo para comprobar las reglas de alto y desplazamiento.
const estilos = readFileSync(resolve(process.cwd(), 'src/styles/app.css'), 'utf8').replaceAll('\r\n', '\n')

vi.mock('../../services/agente', () => ({ preguntarAlAsistente: vi.fn(), listarConversaciones: vi.fn(), obtenerConversacion: vi.fn() }))
vi.mock('../../services/cursos', () => ({ obtenerCurso: vi.fn() }))
vi.mock('../../services/indicadores', () => ({ obtenerIndicadores: vi.fn(), contarAlertasActivas: vi.fn() }))

import { listarConversaciones, obtenerConversacion, preguntarAlAsistente } from '../../services/agente'
import { obtenerCurso } from '../../services/cursos'
import { obtenerIndicadores } from '../../services/indicadores'

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
  {
    id: 5,
    nombre: 'Karina',
    apellido: 'Lara',
    promedioPonderado: 5.29,
    notas: { calificadas: 12, ceros: 0, sinEntrega: 4, pendientes: 0 },
    tendencia: -0.2,
  },
  {
    id: 6,
    nombre: 'Elena',
    apellido: 'Espinoza',
    promedioPonderado: 8.61,
    notas: { calificadas: 12, ceros: 1, sinEntrega: 4, pendientes: 0 },
    tendencia: 0.1,
  },
]

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

function renderAsistente() {
  return render(
    <MemoryRouter initialEntries={['/cursos/1/asistente']}>
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
  listarConversaciones.mockResolvedValue([])
  obtenerIndicadores.mockResolvedValue({ estudiantes })
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  sessionStorage.clear()
})

describe('Asistente como chat', () => {
  it('da la bienvenida con sugerencias y responde con la tarjeta del estudiante nombrado', async () => {
    obtenerCurso.mockResolvedValue(curso)
    preguntarAlAsistente.mockResolvedValue(respuestaChat)
    renderAsistente()

    expect(await screen.findByRole('heading', { name: 'Hola, Docente Uno. ¿Qué quieres saber de AGIL-4B?' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /¿Qué estudiantes requieren atención\?/ })).toBeInTheDocument()

    await preguntar('¿Cómo le va a Karina Lara?')

    expect(await screen.findByText('Karina Lara tiene un promedio de 5,29 y requiere atención.')).toBeInTheDocument()
    expect(preguntarAlAsistente).toHaveBeenCalledWith('1', '¿Cómo le va a Karina Lara?')
    const tarjetas = screen.getByLabelText('Estudiantes mencionados')
    expect(within(tarjetas).getByText('Karina Lara')).toBeInTheDocument()
    expect(within(tarjetas).getByText('5,29')).toBeInTheDocument()
    expect(within(tarjetas).getByText('4 sin entregar')).toBeInTheDocument()
    expect(within(tarjetas).queryByText('Elena Espinoza')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Abrir en el panel' })).toHaveAttribute('href', '/cursos/1/panel')

    fireEvent.click(screen.getByRole('button', { name: 'Datos usados' }))
    const datos = screen.getByRole('list', { name: 'Datos usados' })
    expect(datos).toHaveTextContent('Notas de Karina Lara · 62 ms')
    expect(datos).toHaveTextContent('Sincronización del 09/11/2026')
    expect(datos).not.toHaveTextContent('obtener_calificaciones')
    expect(screen.getByRole('navigation', { name: 'Historial de preguntas' })).toHaveTextContent('¿Cómo le va a Karina Lara?')
  })

  it('mientras consulta muestra «Consultando…» y deshabilita el envío', async () => {
    obtenerCurso.mockResolvedValue(curso)
    let responder
    preguntarAlAsistente.mockReturnValue(new Promise((r) => (responder = r)))
    renderAsistente()

    await preguntar('¿Qué estudiantes requieren atención?')

    expect(await screen.findByRole('status')).toHaveTextContent('Consultando…')
    expect(screen.getByRole('button', { name: 'Enviar pregunta' })).toBeDisabled()
    responder(respuestaChat)
    await waitFor(() => expect(screen.queryByRole('status')).not.toBeInTheDocument())
  })

  it('con 503 muestra «El asistente no está disponible»', async () => {
    obtenerCurso.mockResolvedValue(curso)
    preguntarAlAsistente.mockRejectedValue(errorHttp(503, { title: 'Asistente no disponible', detail: 'El asistente no respondió en 60 segundos.' }))
    renderAsistente()

    await preguntar('¿Qué estudiantes requieren atención?')

    const aviso = await screen.findByRole('alert')
    expect(aviso).toHaveTextContent('El asistente no está disponible')
    expect(aviso).toHaveTextContent('no respondió en 60 segundos')
  })

  it('el historial sale de la base agrupado por fecha y abre una conversación guardada', async () => {
    obtenerCurso.mockResolvedValue(curso)
    listarConversaciones.mockResolvedValue([
      { id: 48, fecha: new Date().toISOString(), pregunta: '¿Qué estudiantes requieren atención?', modelo: 'qwen2.5:7b', duracionMs: 23100, herramientas: 1 },
      { id: 12, fecha: '2026-01-05T10:00:00-05:00', pregunta: '¿Qué cursos tengo asignados?', modelo: 'qwen2.5:7b', duracionMs: 5000, herramientas: 1 },
    ])
    obtenerConversacion.mockResolvedValue({
      id: 12,
      cursoId: 1,
      fecha: '2026-01-05T10:00:00-05:00',
      pregunta: '¿Qué cursos tengo asignados?',
      respuesta: 'Tienes asignado el curso AGIL-4B.',
      modelo: 'qwen2.5:7b',
      duracionMs: 5000,
      herramientas: [{ nombre: 'listar_cursos_autorizados', parametros: {}, estado: 'OK', duracionMs: 41 }],
    })
    renderAsistente()

    const historial = await screen.findByRole('navigation', { name: 'Historial de preguntas' })
    await within(historial).findByText('Hoy')
    expect(within(historial).getByText('Antes')).toBeInTheDocument()
    expect(listarConversaciones).toHaveBeenCalledWith('1')

    fireEvent.click(within(historial).getByRole('button', { name: '¿Qué cursos tengo asignados?' }))
    expect(await screen.findByText('Tienes asignado el curso AGIL-4B.')).toBeInTheDocument()
    expect(obtenerConversacion).toHaveBeenCalledWith(12)

    fireEvent.click(screen.getByRole('button', { name: /Nueva conversación/ }))
    expect(screen.queryByText('Tienes asignado el curso AGIL-4B.')).not.toBeInTheDocument()
  })

  it('con 60 conversaciones en el historial, el cuadro de la pregunta sigue en pantalla', async () => {
    obtenerCurso.mockResolvedValue(curso)
    listarConversaciones.mockResolvedValue(
      Array.from({ length: 60 }, (_, i) => ({
        id: 100 + i,
        fecha: new Date(Date.now() - i * 6 * 60 * 60 * 1000).toISOString(),
        pregunta: `Pregunta guardada número ${i + 1}`,
        modelo: 'qwen2.5:7b',
        duracionMs: 10000,
        herramientas: 1,
      })),
    )
    const { container } = renderAsistente()

    const historial = await screen.findByRole('navigation', { name: 'Historial de preguntas' })
    await within(historial).findByText('Pregunta guardada número 60')
    expect(within(historial).getAllByRole('button')).toHaveLength(60)

    // El historial se desplaza dentro de su propia caja; «Nueva conversación», la navegación y el docente quedan fuera de ella.
    expect(historial).toHaveClass('chat-historial')
    const lateral = historial.closest('.chat-lateral')
    expect(historial).not.toContainElement(screen.getByRole('button', { name: /Nueva conversación/ }))
    expect(historial).not.toContainElement(within(lateral).getByRole('link', { name: 'Panel' }))
    expect(historial).not.toContainElement(lateral.querySelector('.chat-lateral-pie'))

    // El cuadro de la pregunta no está en el historial ni en los mensajes: está en el pie fijo, después de los mensajes.
    const campo = screen.getByLabelText('Tu pregunta')
    const pie = campo.closest('.chat-pie')
    const principal = container.querySelector('.chat-main')
    expect(pie.parentElement).toBe(principal)
    expect(principal.lastElementChild).toBe(pie)
    expect(principal.querySelector('.chat-cuerpo')).not.toContainElement(campo)

    // La pantalla mide el alto de la ventana y la página no se desplaza: solo el historial y los mensajes (styles/app.css).
    const regla = (selector) => {
      const inicio = estilos.indexOf(`\n${selector} {`)
      return inicio < 0 ? '' : estilos.slice(inicio, estilos.indexOf('}', inicio))
    }
    expect(regla('.chat')).toMatch(/height: 100dvh/)
    expect(regla('.chat')).toMatch(/overflow: hidden/)
    expect(regla('.chat-historial')).toMatch(/overflow-y: auto/)
    expect(regla('.chat-historial')).toMatch(/min-height: 0/)
    expect(regla('.chat-cuerpo')).toMatch(/overflow-y: auto/)
    expect(regla('.chat-cuerpo')).toMatch(/min-height: 0/)
    expect(regla('.chat-pie')).toMatch(/flex-shrink: 0/)
  })

  it('un curso ajeno muestra «Acceso no permitido» en pantalla completa', async () => {
    obtenerCurso.mockRejectedValue(errorHttp(403, { detail: 'El curso 2 no está asignado a este docente.' }))
    renderAsistente()

    expect(await screen.findByRole('heading', { name: 'Acceso no permitido' })).toBeInTheDocument()
    expect(screen.getByText('El curso 2 no está asignado a este docente.')).toBeInTheDocument()
    expect(screen.queryByLabelText('Tu pregunta')).not.toBeInTheDocument()
  })

  it('una sesión vencida muestra «Tu sesión venció»', async () => {
    obtenerCurso.mockRejectedValue(errorHttp(401))
    renderAsistente()

    expect(await screen.findByRole('heading', { name: 'Tu sesión venció' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Iniciar sesión' })).toBeInTheDocument()
  })
})

describe('Lenguaje del asistente', () => {
  it('«Datos usados» dice qué se consultó, sin nombres de funciones', () => {
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

  it('el motivo usa los conteos reales y el historial se agrupa por fecha', () => {
    expect(motivo(estudiantes[1])).toBe('1 nota en cero · 4 sin entregar')
    const ahora = new Date('2026-11-12T08:00:00-05:00')
    expect(grupoDeFecha('2026-11-12T07:50:00-05:00', ahora)).toBe('Hoy')
    expect(grupoDeFecha('2026-11-10T07:50:00-05:00', ahora)).toBe('Esta semana')
    expect(grupoDeFecha('2026-10-01T07:50:00-05:00', ahora)).toBe('Antes')
  })
})
