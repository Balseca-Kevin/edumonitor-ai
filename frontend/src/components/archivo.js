/** Guarda un archivo que devolvió la API (blob y nombre) como descarga del navegador. */
export function guardarArchivo({ blob, nombre }) {
  const url = URL.createObjectURL(blob)
  const enlace = document.createElement('a')
  enlace.href = url
  enlace.download = nombre
  document.body.appendChild(enlace)
  enlace.click()
  enlace.remove()
  URL.revokeObjectURL(url)
}
