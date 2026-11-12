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
            Trang chủ
          </NavLink>

          <NavLink className="navbar__link" to="/daily-tasks">
            Nhiệm vụ hàng ngày
          </NavLink>

          <NavLink className="navbar__link" to="/leaderboard">
            Bảng xếp hạng
          </NavLink>

          <NavLink className="navbar__link" to="/tests">
            Bài học & Test
          </NavLink>
          {isAdmin() && (
            <NavLink className="navbar__link" to="/admin/lessons">
              Quản lý nội dung
            </NavLink>
          )}
          {isAdmin() && (
            <NavLink className="navbar__link" to="/admin/users">
              Quản lý người dùng
            </NavLink>
          )}
          {isAdmin() && (
            <NavLink className="navbar__link" to="/admin/system">
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
