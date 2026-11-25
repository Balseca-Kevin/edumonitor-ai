import { afterEach, describe, expect, it, vi } from 'vitest'
import { act, cleanup, render, screen, within } from '@testing-library/react'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import AvisoSincronizacion from './AvisoSincronizacion'
import PantallaCarga from './PantallaCarga'
import { CIERRE_AVISO_MS, pasosDeSincronizacion } from './sincronizacion'

// Vitest no procesa el CSS: se lee el archivo para comprobar las animaciones del aviso y de la carga.
const estilos = readFileSync(resolve(process.cwd(), 'src/styles/app.css'), 'utf8').replaceAll('\r\n', '\n')

// Formas de POST /api/cursos/{id}/sync y de GET /api/cursos/{id}/sync/{sid}/cambios (docs/contratos-api.md).
const completada = { id: 13, estado: 'COMPLETADA', registros: { unidades: 4, actividades: 19, estudiantes: 32, calificaciones: 608 }, errores: [] }
const cambios = { sincronizacionId: 13, anteriorId: 12, conteos: { total: 3, notasNuevas: 3, notasCambiadas: 0, entregasNuevas: 0, otros: 0 }, celdas: [], noDisponibles: [] }

afterEach(() => {
  cleanup()
  vi.useRealTimers()
})

describe('TA-21.A2 · Pasos de la sincronización', () => {
  it('sin respuesta todos giran; con ella cada paso queda listo o con el error de su función', () => {
    expect(pasosDeSincronizacion(null).map((p) => p.estado)).toEqual(['girando', 'girando', 'girando', 'girando', 'girando'])

    const parcial = pasosDeSincronizacion({
      estado: 'PARCIAL',
      errores: [{ funcion: 'mod_assign_get_submissions', mensaje: 'No responde' }],
    })
    expect(parcial.map((p) => p.estado)).toEqual(['listo', 'listo', 'listo', 'error', 'listo'])
    expect(parcial[3]).toEqual({ texto: 'Entregas y fechas límite', estado: 'error', detalle: 'No responde' })

    expect(pasosDeSincronizacion({ estado: 'FALLIDA', errores: [] }).every((p) => p.estado === 'error')).toBe(true)
  })

  it('la pantalla de carga muestra el avance con los pasos terminados', () => {
    render(<PantallaCarga titulo="Sincronizando curso…" codigo="AGIL-4B · AULA VIRTUAL" pasos={pasosDeSincronizacion(completada)} />)

    const carga = screen.getByRole('dialog', { name: 'Sincronizando curso…' })
    expect(carga).toHaveAttribute('aria-busy', 'false')
    expect(within(carga).getByRole('progressbar', { name: 'Avance' })).toHaveAttribute('aria-valuenow', '5')
    expect(within(carga).getAllByText(/· terminado/)).toHaveLength(5)
  })
})

describe('TA-21.A2 · Aviso de sincronización', () => {
  it('se cierra solo a los 8 s', () => {
    vi.useFakeTimers()
    const cerrar = vi.fn()
    render(<AvisoSincronizacion codigo="AGIL-4B" sincronizacion={completada} cambios={cambios} atencion={4} onCerrar={cerrar} />)

    act(() => vi.advanceTimersByTime(CIERRE_AVISO_MS - 1))
    expect(cerrar).not.toHaveBeenCalled()
    act(() => vi.advanceTimersByTime(1))
    expect(cerrar).toHaveBeenCalledTimes(1)
  })

  it('en la primera sincronización dice lo que se importó, no cambios', () => {
    render(
      <AvisoSincronizacion codigo="AGIL-4B" sincronizacion={completada} cambios={{ ...cambios, anteriorId: null, conteos: { ...cambios.conteos, total: 0, notasNuevas: 0 } }} atencion={4} onCerrar={() => {}} />,
    )

    expect(screen.getByText('AGIL-4B ya tiene los datos del aula virtual')).toBeInTheDocument()
    expect(screen.getByText('notas importadas').previousSibling).toHaveTextContent('608')
    expect(screen.getByText('estudiantes').previousSibling).toHaveTextContent('32')
  })

  it('una sincronización anterior al registro de cambios dice «no disponible»', () => {
    render(
      <AvisoSincronizacion codigo="AGIL-4B" sincronizacion={completada} cambios={{ ...cambios, noDisponibles: ['cambios'] }} atencion={null} onCerrar={() => {}} />,
    )

    expect(screen.getByText('notas nuevas').previousSibling).toHaveTextContent('no disponible')
    expect(screen.getByText('requieren atención').previousSibling).toHaveTextContent('no disponible')
    expect(screen.queryByText('Sin cambios desde la sincronización anterior')).toBeNull()
  })

  it('una sincronización FALLIDA lo dice sin cifras', () => {
    render(
      <AvisoSincronizacion
        codigo="AGIL-4B"
        sincronizacion={{ id: 15, estado: 'FALLIDA', errores: [{ funcion: 'core_course_get_contents', mensaje: 'x' }] }}
        cambios={null}
        atencion={null}
        onCerrar={() => {}}
      />,
    )

    expect(screen.getByText('LA SINCRONIZACIÓN FALLÓ')).toBeInTheDocument()
    expect(screen.queryByText('notas nuevas')).toBeNull()
    expect(screen.getByText('Falló: core_course_get_contents')).toBeInTheDocument()
  })

  it('«Ver cambios en el libro» solo aparece cuando la pantalla del libro lo ofrece y hay cambios', () => {
    const ver = vi.fn()
    const { rerender } = render(<AvisoSincronizacion codigo="AGIL-4B" sincronizacion={completada} cambios={cambios} atencion={4} onCerrar={() => {}} />)
    expect(screen.queryByRole('button', { name: 'Ver cambios en el libro' })).toBeNull()

    rerender(<AvisoSincronizacion codigo="AGIL-4B" sincronizacion={completada} cambios={cambios} atencion={4} onCerrar={() => {}} onVerCambios={ver} />)
    screen.getByRole('button', { name: 'Ver cambios en el libro' }).click()
    expect(ver).toHaveBeenCalled()
  })

  it('las animaciones del aviso y de la carga existen y quedan quietas con prefers-reduced-motion', () => {
    expect(estilos).toMatch(/\.aviso-sync \{[^}]*animation: baja/)
    expect(estilos).toMatch(/\.aviso-sync-brillo::after \{[^}]*animation: barre/)
    expect(estilos).toMatch(/\.carga-giro \{[^}]*animation: gira/)
    expect(estilos).toMatch(/\.carga-caja \{[^}]*animation: pop/)
    expect(estilos).toMatch(/@media \(prefers-reduced-motion: reduce\) \{\n {2}\*, \*::before, \*::after \{ animation-duration: 0ms !important;/)
  })
})
