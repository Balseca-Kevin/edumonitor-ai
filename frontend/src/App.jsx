
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import Login from './pages/Login'
import Cursos from './pages/Cursos'
import ConfiguracionMoodle from './pages/ConfiguracionMoodle'
import Layout from './components/Layout'
import CursoMarco from './pages/curso/CursoMarco'
import Asistente from './pages/curso/Asistente'
import Alertas from './pages/curso/Alertas'
import Panel from './pages/curso/Panel'
import './App.css'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />

        <Route element={<Layout />}>
          <Route path="/cursos" element={<Cursos />} />
          <Route path="/admin/moodle" element={<ConfiguracionMoodle />} />
          <Route path="/cursos/:id" element={<CursoMarco />}>
            <Route index element={<Navigate to="panel" replace />} />
            <Route path="panel" element={<Panel />} />
            <Route path="alertas" element={<Alertas />} />
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
