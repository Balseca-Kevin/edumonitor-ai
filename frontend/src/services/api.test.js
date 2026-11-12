
import { afterEach, describe, expect, it, vi } from 'vitest'
import api from './api'

const adaptadorOriginal = api.defaults.adapter

afterEach(() => {
  api.defaults.adapter = adaptadorOriginal
  sessionStorage.clear()
  vi.restoreAllMocks()
})

function simularRespuestaHTTP(estado) {
  api.defaults.adapter = async (config) => {
    throw {
      config,
      response: {
        status: estado,
        data: { title: 'Error simulado' },
      },
    }
  }
}

describe('HU-19 · Interceptor HTTP', () => {
  it('notifica sesión vencida ante un 401 autenticado', async () => {
    sessionStorage.setItem(
      'edumonitor_token',
      'jwt-de-prueba',
    )

    simularRespuestaHTTP(401)

    const evento = vi.spyOn(window, 'dispatchEvent')

    await expect(
      api.get('/api/cursos'),
    ).rejects.toMatchObject({
      response: { status: 401 },
    })

    expect(
      evento.mock.calls.some(
        ([e]) => e.type === 'edumonitor:sesion-vencida',
      ),
    ).toBe(true)
  })

  it('no trata las credenciales incorrectas como sesión vencida', async () => {
    sessionStorage.setItem(
      'edumonitor_token',
      'jwt-de-prueba',
    )

    simularRespuestaHTTP(401)

    const evento = vi.spyOn(window, 'dispatchEvent')

    await expect(
      api.post('/api/auth/login', {
        usuario: 'doc01',
        contrasena: 'incorrecta',
      }),
    ).rejects.toMatchObject({
      response: { status: 401 },
    })

    expect(
      evento.mock.calls.some(
        ([e]) => e.type === 'edumonitor:sesion-vencida',
      ),
    ).toBe(false)
  })

  it('no convierte un error 403 en sesión vencida', async () => {
    sessionStorage.setItem(
      'edumonitor_token',
      'jwt-de-prueba',
    )

    simularRespuestaHTTP(403)

    const evento = vi.spyOn(window, 'dispatchEvent')

    await expect(
      api.get('/api/cursos/1'),
    ).rejects.toMatchObject({
      response: { status: 403 },
    })

    expect(
      evento.mock.calls.some(
        ([e]) => e.type === 'edumonitor:sesion-vencida',
      ),
    ).toBe(false)
  })

  it('no notifica sesión vencida cuando no existe token', async () => {
    simularRespuestaHTTP(401)

    const evento = vi.spyOn(window, 'dispatchEvent')

    await expect(
      api.get('/api/cursos'),
    ).rejects.toMatchObject({
      response: { status: 401 },
    })

    expect(
      evento.mock.calls.some(
        ([e]) => e.type === 'edumonitor:sesion-vencida',
      ),
    ).toBe(false)
  })
})
