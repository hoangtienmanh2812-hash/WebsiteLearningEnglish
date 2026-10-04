import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginForm from './features/auth/LoginForm';
import RegisterForm from './features/auth/RegisterForm';
import ProtectedRoute from './features/auth/ProtectedRoute';
import DashboardPage from './features/dashboard/DashboardPage';
import MainLayout from './layouts/MainLayout';
import QuizPage from './features/quiz/QuizPage';

// 👉 1. Import 2 Component tính năng mới vào đây
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
            
            {/* 👉 2. Thêm 2 đường dẫn mới vào đây */}
            <Route path="/daily-tasks" element={<DailyTasks />} />
            <Route path="/leaderboard" element={<Leaderboard />} />
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