import { Navigate, Outlet } from 'react-router-dom';
import { isAdmin } from './authStorage';

export default function AdminRoute() {
  return isAdmin() ? <Outlet /> : <Navigate to="/dashboard" replace />;
}
