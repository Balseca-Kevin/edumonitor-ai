import { afterEach, describe, expect, it, vi } from 'vitest'
import api from './api'
import { preguntarAlAsistente } from './agente'

afterEach(() => vi.restoreAllMocks())

describe('C7 · Tramo de la pantalla en la pregunta al asistente', () => {
  it('manda el tramo elegido en el foco para que el texto y las tarjetas calculen con las mismas actividades', async () => {
    const post = vi.spyOn(api, 'post').mockResolvedValue({ data: {} })
    await preguntarAlAsistente('1', '¿Quiénes van bajo 7?', { tramo: 'u2' }, null)
    expect(post).toHaveBeenCalledWith('/api/agente/chat', { cursoId: 1, mensaje: '¿Quiénes van bajo 7?', foco: { tramo: 'u2' } })
  })

  it('con «todo el ciclo» no manda tramo, igual que las rutas sin filtro', async () => {
    const post = vi.spyOn(api, 'post').mockResolvedValue({ data: {} })
    await preguntarAlAsistente('1', 'Hola', { estudianteId: 5, tramo: 'todo' }, 44)
    expect(post).toHaveBeenCalledWith('/api/agente/chat', { cursoId: 1, mensaje: 'Hola', foco: { estudianteId: 5 }, conversacionId: 44 })
  })
})
