import { tonoDeEstado } from './formato'

/** Estado con punto de color y texto, nunca solo color (diseño D). */
export default function Estado({ estado, texto }) {
  return <span className={`estado estado--${tonoDeEstado(estado)}`}>{texto ?? estado}</span>
}
