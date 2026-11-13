import { NavLink } from 'react-router-dom';
import { getAuthUser, isAdmin } from '../../features/auth/authStorage';
import './navbar.css';

export default function Navbar() {
  const user = getAuthUser();
  const displayName = user?.fullName?.trim() || user?.email || 'Người học';
  const initial = displayName.charAt(0).toUpperCase();

  return (
    <header className="navbar-shell">
      <div className="navbar">
        <NavLink className="navbar__brand" to="/dashboard" aria-label="English Learning - Trang chủ">
          <span className="navbar__brand-mark" aria-hidden="true">E</span>
          <span>English Learning</span>
        </NavLink>

        <nav className="navbar__navigation" aria-label="Điều hướng chính">
          <NavLink className="navbar__link" to="/dashboard" end>
            <svg className="navbar__link-icon" viewBox="0 0 24 24" aria-hidden="true">
              <path d="m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-6v-7h-4v7H4a1 1 0 0 1-1-1V10Z" />
            </svg>
            Trang chủ
          </NavLink>

          <NavLink className="navbar__link" to="/daily-tasks">
            <svg className="navbar__link-icon" viewBox="0 0 24 24" aria-hidden="true">
              <rect x="3" y="5" width="18" height="16" rx="2" />
              <path d="M16 3v4M8 3v4M3 10h18m-13 5 2 2 4-4" />
            </svg>
            Nhiệm vụ hàng ngày
          </NavLink>

          <NavLink className="navbar__link" to="/leaderboard">
            <svg className="navbar__link-icon" viewBox="0 0 24 24" aria-hidden="true">
              <path d="M8 4h8v5a4 4 0 0 1-8 0V4Zm0 2H4v2a4 4 0 0 0 4 4m8-6h4v2a4 4 0 0 1-4 4m-4 1v4m-4 3h8m-7-3h6v3H9v-3Z" />
            </svg>
            Bảng xếp hạng
          </NavLink>

          <NavLink className="navbar__link" to="/tests">
            <svg className="navbar__link-icon" viewBox="0 0 24 24" aria-hidden="true">
              <path d="M5 3h10l4 4v14H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Zm10 0v5h5M8 12h8m-8 4h8" />
            </svg>
            Bài học & Test
          </NavLink>
          {isAdmin() && (
            <NavLink className="navbar__link" to="/admin/lessons">
              <svg className="navbar__link-icon" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M4 4h7a3 3 0 0 1 3 3v14a3 3 0 0 0-3-3H4V4Zm16 0h-3a3 3 0 0 0-3 3v14a3 3 0 0 1 3-3h3V4Z" />
              </svg>
              Quản lý nội dung
            </NavLink>
          )}
          {isAdmin() && (
            <NavLink className="navbar__link" to="/admin/users">
              <svg className="navbar__link-icon" viewBox="0 0 24 24" aria-hidden="true">
                <circle cx="9" cy="8" r="3" />
                <path d="M3 20v-1a6 6 0 0 1 12 0v1H3Zm13-9a3 3 0 1 0 0-6m2 9a5 5 0 0 1 3 4v1h-3" />
              </svg>
              Quản lý người dùng
            </NavLink>
          )}
          {isAdmin() && (
            <NavLink className="navbar__link" to="/admin/system">
              <svg className="navbar__link-icon" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z" />
                <path d="m19.4 15 .1.1 1.4 1.1-1.4 2.4-1.7-.7a8 8 0 0 1-1.7 1l-.3 1.8h-2.8l-.3-1.8a8 8 0 0 1-1.7-1l-1.7.7-1.4-2.4 1.4-1.1a7 7 0 0 1 0-2l-1.4-1.1 1.4-2.4 1.7.7a8 8 0 0 1 1.7-1l.3-1.8h2.8l.3 1.8a8 8 0 0 1 1.7 1l1.7-.7 1.4 2.4-1.4 1.1a7 7 0 0 1-.1 2Z" />
              </svg>
              Quản lý hệ thống
            </NavLink>
          )}
        </nav>

        <NavLink className="user-menu__trigger" to="/account" aria-label="Quản lý tài khoản cá nhân">
            <span className="user-menu__avatar" aria-hidden="true">{initial}</span>
            <span className="user-menu__details">
              <span className="user-menu__name">{displayName}</span>
              <span className="user-menu__caption">Quản lý tài khoản</span>
            </span>
            <svg viewBox="0 0 20 20" aria-hidden="true" width="16" height="16">
              <path d="m6 8 4 4 4-4" stroke="currentColor" strokeWidth="2" fill="none" />
            </svg>
        </NavLink>
      </div>
    </header>
  );
}
