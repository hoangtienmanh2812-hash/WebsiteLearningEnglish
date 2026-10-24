import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginForm from './features/auth/LoginForm';
import RegisterForm from './features/auth/RegisterForm';
import ProtectedRoute from './features/auth/ProtectedRoute';
import DashboardPage from './features/dashboard/DashboardPage';
import MainLayout from './layouts/MainLayout';
import QuizPage from './features/quiz/QuizPage';
import TestsPage from './features/test/TestsPage';
import TestPage from './features/test/TestPage'; // 👉 1. Import TestPage vào đây
import AdminRoute from './features/auth/AdminRoute';
import AdminLessonsPage from './features/admin/AdminLessonsPage';
import AdminUsersPage from './features/admin/AdminUsersPage';
import AdminSystemPage from './features/admin/AdminSystemPage';

import { DailyTasks } from './features/tasks/DailyTasks';
import { Leaderboard } from './features/leaderboard/leaderboard';

import './index.css';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/login" element={<LoginForm />} />
        <Route path="/register" element={<RegisterForm />} />
        
        <Route element={<ProtectedRoute />}>
          {/* Các trang hiển thị bên trong khung giao diện chung MainLayout (có Navbar/Sidebar) */}
          <Route element={<MainLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/daily-tasks" element={<DailyTasks />} />
            <Route path="/leaderboard" element={<Leaderboard />} />
            <Route path="/tests" element={<TestsPage />} />
            <Route element={<AdminRoute />}>
              <Route path="/admin/lessons" element={<AdminLessonsPage />} />
              <Route path="/admin/users" element={<AdminUsersPage />} />
              <Route path="/admin/system" element={<AdminSystemPage />} />
            </Route>
          </Route>
          
          {/* Đường dẫn tới trang quiz từ vựng cũ */}
          <Route path="/quiz/:id" element={<QuizPage />} />

          {/* 👉 2. Thêm route trỏ tới giao diện Test tổng hợp mới */}
          <Route path="/test/:id" element={<TestPage />} />
        </Route>
        
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
