import { useEffect, useState } from 'react'

/** true si el sistema pide menos movimiento o si no hay forma de saberlo (pruebas, navegadores sin matchMedia). */
export function sinMovimiento() {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return true
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches
}

/**
 * Cifra que cuenta desde 0 hasta `valor` una sola vez, en `duracion` ms con salida suave (diseño D: todo el movimiento termina
 * en menos de 3 s). Con prefers-reduced-motion devuelve el valor final desde el principio. Null se devuelve tal cual, para que
 * la pantalla diga «no disponible» y nunca muestre 0.
 */
export function useCuenta(valor, duracion = 1100) {
  const quieto = valor === null || valor === undefined || sinMovimiento()
  const [progreso, setProgreso] = useState(quieto ? 1 : 0)

  useEffect(() => {
    if (quieto) return undefined
    let marco
    const inicio = performance.now()
    const paso = (ahora) => {
      const x = Math.min(1, (ahora - inicio) / duracion)
      setProgreso(1 - Math.pow(1 - x, 3))
      if (x < 1) marco = requestAnimationFrame(paso)
    }
    marco = requestAnimationFrame(paso)
    return () => cancelAnimationFrame(marco)
  }, [quieto, duracion, valor])

  if (valor === null || valor === undefined) return valor
  return quieto ? valor : valor * progreso
}
