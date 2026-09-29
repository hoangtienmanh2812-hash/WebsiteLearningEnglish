import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginForm from './features/auth/LoginForm';
import RegisterForm from './features/auth/RegisterForm';
import ProtectedRoute from './features/auth/ProtectedRoute';
import DashboardPage from './features/dashboard/DashboardPage';
import MainLayout from './layouts/MainLayout';
import QuizPage from './features/quiz/QuizPage';
import './index.css';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/login" element={<LoginForm />} />
        <Route path="/register" element={<RegisterForm />} />
        
        <Route element={<ProtectedRoute />}>
          {/* Khu vực giao diện chính có Sidebar */}
          <Route element={<MainLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
          </Route>
          
          {/* Đường dẫn tới trang làm bài kiểm tra */}
          <Route path="/quiz/:id" element={<QuizPage />} />
        </Route>
        
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;