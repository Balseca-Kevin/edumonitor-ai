import { describe, expect, it } from 'vitest'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const leer = (archivo) => readFileSync(fileURLToPath(new URL(archivo, import.meta.url)), 'utf8')
const tokens = leer('./tokens.css')
const app = leer('./app.css')

/** R.1 · Paleta del Sprint 3 (docs/sprint-3/diseno.md, «Color»): cada token con su valor y sin el índigo ni el slate de la propuesta anterior. */
const PALETA = {
  '--oscuro-fondo': '#16181d',
  '--oscuro-superficie': '#1e2027',
  '--oscuro-linea': '#2a2d35',
  '--oscuro-texto': '#e6e7ea',
  '--oscuro-texto-2': '#9aa0ab',
  '--oscuro-texto-3': '#8a8f9c',
  '--oscuro-punto': '#aeb6c6',
  '--oscuro-arena': '#c9a877',
  '--vivo': '#8fd0a0',
  '--papel': '#f4f2ed',
  '--hoja': '#ffffff',
  '--hoja-2': '#faf8f3',
  '--linea': '#e4e0d6',
  '--linea-2': '#d9d4c7',
  '--tinta': '#1a1c22',
  '--tinta-2': '#6b7080',
  '--ia': '#0e6e60',
  '--ia-fondo': '#e3f1ee',
  '--ia-texto': '#0a5248',
  '--grafico': '#5b6170',
  '--arena': '#d2b48a',
  '--bajo-fondo': '#f3e7d3',
  '--bajo-texto': '#7a4e14',
  '--sin-entrega-fondo': '#f6ddd9',
  '--sin-entrega-texto': '#8e2f28',
  '--descenso-fondo': '#e5e7f3',
  '--descenso-texto': '#2e3a8c',
  '--ok-fondo': '#ddebdd',
  '--ok': '#2f6b3a',
  '--acento': '#7b8bd6',
  '--acento-texto': '#3f51b5',
  '--error': '#b3463f',
}

describe('tokens del Sprint 3', () => {
  it.each(Object.entries(PALETA))('%s vale %s', (token, valor) => {
    expect(tokens.replace(/\s+/g, '')).toContain(`${token}:${valor};`)
  })

  it('no quedan el índigo ni el slate de la propuesta anterior', () => {
    expect(`${tokens}${app}`).not.toMatch(/#4f46e5|\bslate\b/i)
  })

  it('app.css no escribe colores literales: todos salen de tokens.css', () => {
    expect(app).not.toMatch(/#[0-9a-f]{3,6}\b/i)
  })

  it('el movimiento queda quieto con prefers-reduced-motion', () => {
    expect(tokens).toMatch(/prefers-reduced-motion: reduce[\s\S]*--t-ficha: 0ms/)
    expect(app).toMatch(/prefers-reduced-motion: reduce/)
  })
})
