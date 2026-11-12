import { tonoDeEstado } from './formato'

/** Estado con punto de color y texto, nunca solo color (diseno.md). */
export default function Estado({ estado, texto }) {
  return <span className={`s2-estado s2-estado--${tonoDeEstado(estado)}`}>{texto ?? estado}</span>
}
