
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

export async function obtenerCursos() {
  if (USE_MOCK) {
    return cursosSimulados
  }

  const response = await api.get('/api/cursos')
  return response.data
}
