
import api from './api'

const USE_MOCK = import.meta.env.VITE_USE_MOCK === 'true'

const cursosSimulados = [
  {
    id: 1,
    moodleId: 3,
    nombreCorto: 'AGIL-4B',
    nombre: 'Metodologías Ágiles (lab)',
    periodo: '2026-2',
    ultimaSincronizacion: {
      id: 5,
      fecha: '2026-10-22T09:15:00-05:00',
      estado: 'COMPLETADA',
    },
  },
]

const historialSimulado = {
  1: [
    {
      id: 5,
      cursoId: 1,
      estado: 'COMPLETADA',
      inicio: '2026-10-22T09:14:00-05:00',
      fin: '2026-10-22T09:15:00-05:00',
      origen: 'MOODLE',
      registros: {
        unidades: 4,
        actividades: 12,
        estudiantes: 30,
        calificaciones: 90,
      },
      errores: [],
    },
  ],
}

export async function obtenerCursos() {
  if (USE_MOCK) {
    return cursosSimulados
  }

  const response = await api.get('/api/cursos')
  return response.data
}

export async function sincronizarCurso(id) {
  if (USE_MOCK) {
    const resultado = {
      id: Date.now(),
      cursoId: Number(id),
      estado: 'COMPLETADA',
      inicio: new Date().toISOString(),
      fin: new Date().toISOString(),
      origen: 'SIMULADO',
      registros: {
        unidades: 4,
        actividades: 12,
        estudiantes: 30,
        calificaciones: 90,
      },
      errores: [],
    }

    const historial = historialSimulado[id] ?? []
    historialSimulado[id] = [resultado, ...historial]

    return resultado
  }

  try {
    const response = await api.post(`/api/cursos/${id}/sync`)
    return response.data
  } catch (error) {
    // La API puede responder 502 con una sincronización FALLIDA
    // incluida en ProblemDetails.
    const resultadoFallido = error.response?.data?.sincronizacion

    if (resultadoFallido) {
      return resultadoFallido
    }

    throw error
  }
}

export async function obtenerHistorialSincronizaciones(id) {
  if (USE_MOCK) {
    return historialSimulado[id] ?? []
  }

  const response = await api.get(`/api/cursos/${id}/sync`)
  return response.data
}
