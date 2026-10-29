
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import Login from './pages/Login'
import Cursos from './pages/Cursos'
import Layout from './components/Layout'
import './App.css'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />

        <Route element={<Layout />}>
          <Route path="/cursos" element={<Cursos />} />
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
