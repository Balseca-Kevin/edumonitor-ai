
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import Login from './pages/Login'
import Cursos from './pages/Cursos'
import ConfiguracionMoodle from './pages/ConfiguracionMoodle'
import Layout from './components/Layout'
import CursoMarco from './pages/curso/CursoMarco'
import Asistente from './pages/curso/Asistente'
import Alertas from './pages/curso/Alertas'
import Libro from './pages/curso/Libro'
import Mapa from './pages/curso/Mapa'
import PlanRefuerzo from './pages/curso/PlanRefuerzo'
import FichaEstudiante from './pages/curso/FichaEstudiante'
import Actividad from './pages/curso/Actividad'
import Panel from './pages/curso/Panel'
import EstadoSistema from './pages/admin/EstadoSistema'
import Auditoria from './pages/admin/Auditoria'
import CalidadAsistente from './pages/admin/CalidadAsistente'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />

        <Route element={<Layout />}>
          <Route path="/cursos" element={<Cursos />} />
          <Route path="/admin/moodle" element={<ConfiguracionMoodle />} />
          <Route path="/admin/estado" element={<EstadoSistema />} />
          <Route path="/admin/auditoria" element={<Auditoria />} />
          <Route path="/admin/calidad-asistente" element={<CalidadAsistente />} />
          <Route path="/cursos/:id" element={<CursoMarco />}>
            <Route index element={<Navigate to="libro" replace />} />
            <Route path="libro" element={<Libro />} />
            <Route path="panel" element={<Panel />} />
            <Route path="mapa" element={<Mapa />} />
            <Route path="alertas" element={<Alertas />} />
            <Route path="refuerzos" element={<PlanRefuerzo />} />
            <Route path="estudiantes/:eid" element={<FichaEstudiante />} />
            <Route path="actividades/:aid" element={<Actividad />} />
            <Route path="asistente" element={<Asistente />} />
          </Route>
        </Route>

        <Route
          path="*"
          element={
            <Navigate
              to={
                sessionStorage.getItem('edumonitor_token')
                  ? '/cursos'
                  : '/login'
              }
              replace
            />
          }
        />
      </Routes>
    </BrowserRouter>
  )
}
