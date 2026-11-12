import { Outlet } from 'react-router-dom';
import Navbar from '../components/common/Navbar';
import ChatbotWidget from '../components/common/ChatbotWidget';
import './mainLayout.css';

export default function MainLayout() {
  return (
    <div className="main-layout">
      <Navbar />
      <main className="main-layout__content">
        <Outlet />
      </main>
      <ChatbotWidget />
    </div>
  );
}
