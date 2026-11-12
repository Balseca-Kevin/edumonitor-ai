
import { Navigate, Outlet } from 'react-router-dom'

export default function Layout() {
  const token = sessionStorage.getItem('edumonitor_token')

  if (!token) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
