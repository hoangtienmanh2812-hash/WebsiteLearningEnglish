import { NavLink, useNavigate } from 'react-router-dom';
import { clearAuthSession, getAuthUser } from '../../features/auth/authStorage';
import './navbar.css';

const pendingNavigation = ['Bài học', 'Từ vựng', 'Bài tập', 'Chatbot'];

export default function Navbar() {
  const navigate = useNavigate();
  const user = getAuthUser();
  const displayName = user?.fullName.trim() || user?.email || 'Người học';
  const initial = displayName.charAt(0).toUpperCase();

  const handleLogout = () => {
    clearAuthSession();
    navigate('/login', { replace: true });
  };

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
          {pendingNavigation.map((label) => (
            <button
              className="navbar__link navbar__link--pending"
              key={label}
              type="button"
              disabled
              title={`${label} sẽ sớm được bổ sung`}
            >
              {label}
            </button>
          ))}
        </nav>

        <details className="user-menu">
          <summary className="user-menu__trigger">
            <span className="user-menu__avatar" aria-hidden="true">{initial}</span>
            <span className="user-menu__name">{displayName}</span>
            <svg viewBox="0 0 20 20" aria-hidden="true">
              <path d="m6 8 4 4 4-4" />
            </svg>
          </summary>
          <div className="user-menu__panel">
            <p className="user-menu__identity">
              <strong>{displayName}</strong>
              {user?.email && <span>{user.email}</span>}
            </p>
            <button className="user-menu__logout" type="button" onClick={handleLogout}>
              <svg viewBox="0 0 20 20" aria-hidden="true">
                <path d="M8 4H4.7A1.7 1.7 0 0 0 3 5.7v8.6A1.7 1.7 0 0 0 4.7 16H8m4-4H3m6-3 3 3-3 3" />
              </svg>
              Đăng xuất
            </button>
          </div>
        </details>
      </div>
    </header>
  );
}
