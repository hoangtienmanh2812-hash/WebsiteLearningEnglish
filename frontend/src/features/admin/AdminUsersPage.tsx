import { useCallback, useEffect, useState } from 'react';
import { getAuthUser } from '../auth/authStorage';
import {
  getAdminUser,
  getAdminUsers,
  getAdminUsersApiError,
  updateAdminUser,
  type AdminUser,
  type AdminUserDetails,
} from './adminUsersApi';
import './adminUsers.css';

type RoleFilter = 'all' | 'admin' | 'user';

export default function AdminUsersPage() {
  const currentUser = getAuthUser();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [search, setSearch] = useState('');
  const [searchFilter, setSearchFilter] = useState('');
  const [role, setRole] = useState<RoleFilter>('all');
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState('');
  const [notice, setNotice] = useState('');
  const [selectedUser, setSelectedUser] = useState<AdminUserDetails | null>(null);
  const [isLoadingDetails, setIsLoadingDetails] = useState(false);
  const [editingUser, setEditingUser] = useState<AdminUser | null>(null);
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [isAdmin, setIsAdmin] = useState(false);
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [editorError, setEditorError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  const refreshUsers = useCallback(async () => {
    setIsLoading(true);
    setPageError('');
    try {
      setUsers(await getAdminUsers(searchFilter, role));
    } catch (error: unknown) {
      setPageError(getAdminUsersApiError(error));
    } finally {
      setIsLoading(false);
    }
  }, [role, searchFilter]);

  useEffect(() => {
    void refreshUsers();
  }, [refreshUsers]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => setSearchFilter(search), 250);
    return () => window.clearTimeout(timeoutId);
  }, [search]);

  const openDetails = async (user: AdminUser) => {
    setIsLoadingDetails(true);
    setPageError('');
    try {
      setSelectedUser(await getAdminUser(user.id));
    } catch (error: unknown) {
      setPageError(getAdminUsersApiError(error));
    } finally {
      setIsLoadingDetails(false);
    }
  };

  const openEditor = (user: AdminUser) => {
    setEditingUser(user);
    setFullName(user.fullName);
    setEmail(user.email);
    setIsAdmin(user.isAdmin);
    setPassword('');
    setConfirmPassword('');
    setEditorError('');
  };

  const saveUser = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!editingUser) {
      return;
    }

    setEditorError('');
    if (password && password.length < 8) {
      setEditorError('Mật khẩu mới cần có ít nhất 8 ký tự.');
      return;
    }
    if (password && password !== confirmPassword) {
      setEditorError('Mật khẩu xác nhận chưa khớp.');
      return;
    }

    setIsSaving(true);
    try {
      await updateAdminUser(editingUser.id, {
        fullName,
        email,
        isAdmin,
        ...(password ? { password } : {}),
      });
      setEditingUser(null);
      setPassword('');
      setConfirmPassword('');
      setNotice('Đã cập nhật thông tin người dùng.');
      await refreshUsers();
      if (selectedUser?.id === editingUser.id) {
        setSelectedUser(await getAdminUser(editingUser.id));
      }
    } catch (error: unknown) {
      setEditorError(getAdminUsersApiError(error));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <section className="admin-users">
      <header className="admin-users__header">
        <div>
          <p className="admin-users__eyebrow">KHU VỰC QUẢN TRỊ</p>
          <h1>Quản lý người dùng</h1>
          <p>Tra cứu tài khoản, vai trò và tiến trình học tập.</p>
        </div>
        <button className="admin-users__button admin-users__button--secondary" type="button" onClick={() => { void refreshUsers(); }} disabled={isLoading}>
          Tải lại
        </button>
      </header>

      <div className="admin-users__filters">
        <label>
          <span>Tìm theo tên hoặc email</span>
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Nhập tên hoặc email"
          />
        </label>
        <label>
          <span>Lọc vai trò</span>
          <select value={role} onChange={(event) => setRole(event.target.value as RoleFilter)}>
            <option value="all">Tất cả vai trò</option>
            <option value="admin">Admin</option>
            <option value="user">Người dùng</option>
          </select>
        </label>
        <span className="admin-users__count">{isLoading ? 'Đang tải...' : `${users.length} người dùng`}</span>
      </div>

      {pageError && <p className="admin-users__message admin-users__message--error" role="alert">{pageError}</p>}
      {notice && <p className="admin-users__message admin-users__message--success" role="status">{notice}</p>}
      {isLoadingDetails && <p className="admin-users__message">Đang tải chi tiết người dùng...</p>}

      {!isLoading && users.length === 0 && !pageError && (
        <p className="admin-users__empty">Không tìm thấy người dùng phù hợp.</p>
      )}

      {!isLoading && users.length > 0 && (
        <div className="admin-users__table-wrap">
          <table className="admin-users__table">
            <thead>
              <tr>
                <th>Người dùng</th>
                <th>Vai trò</th>
                <th>XP</th>
                <th>Tiến trình</th>
                <th>Streak</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id}>
                  <td>
                    <strong>{user.fullName}</strong>
                    <span>{user.email}</span>
                  </td>
                  <td><span className={`admin-users__role${user.isAdmin ? ' admin-users__role--admin' : ''}`}>{user.isAdmin ? 'Admin' : 'Người dùng'}</span></td>
                  <td>{user.exp.toLocaleString()}</td>
                  <td>{user.completedLessons}/{user.totalLessons} bài</td>
                  <td>{user.streakCount} ngày</td>
                  <td className="admin-users__actions">
                    <button className="admin-users__text-button" type="button" onClick={() => { void openDetails(user); }}>Chi tiết</button>
                    <button className="admin-users__text-button" type="button" onClick={() => openEditor(user)}>Sửa</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {selectedUser && (
        <div className="admin-users__backdrop" role="presentation" onMouseDown={(event) => {
          if (event.target === event.currentTarget) setSelectedUser(null);
        }}>
          <section className="admin-users__dialog" role="dialog" aria-modal="true" aria-labelledby="admin-user-details-title">
            <header className="admin-users__dialog-header">
              <div>
                <p className="admin-users__eyebrow">HỒ SƠ NGƯỜI DÙNG</p>
                <h2 id="admin-user-details-title">{selectedUser.fullName}</h2>
                <p>{selectedUser.email}</p>
              </div>
              <button className="admin-users__close" type="button" onClick={() => setSelectedUser(null)} aria-label="Đóng">×</button>
            </header>
            <div className="admin-users__stats">
              <div><span>Vai trò</span><strong>{selectedUser.isAdmin ? 'Admin' : 'Người dùng'}</strong></div>
              <div><span>XP</span><strong>{selectedUser.exp.toLocaleString()}</strong></div>
              <div><span>Streak</span><strong>{selectedUser.streakCount} ngày</strong></div>
              <div><span>Hoàn thành</span><strong>{selectedUser.completedLessons}/{selectedUser.totalLessons}</strong></div>
              <div><span>Đăng nhập gần nhất</span><strong>{selectedUser.lastLoginDate ? new Date(selectedUser.lastLoginDate).toLocaleDateString() : 'Chưa có'}</strong></div>
            </div>
            <h3 className="admin-users__progress-title">Tiến trình bài học</h3>
            <ol className="admin-users__progress">
              {selectedUser.progress.map((lesson) => (
                <li key={lesson.lessonId} className={lesson.isCompleted ? 'is-complete' : ''}>
                  <span>{lesson.title}</span>
                  <strong>{lesson.isCompleted ? 'Hoàn thành' : 'Chưa hoàn thành'}</strong>
                </li>
              ))}
              {selectedUser.progress.length === 0 && <li>Chưa có bài học trong hệ thống.</li>}
            </ol>
          </section>
        </div>
      )}

      {editingUser && (
        <div className="admin-users__backdrop" role="presentation" onMouseDown={(event) => {
          if (event.target === event.currentTarget && !isSaving) setEditingUser(null);
        }}>
          <section className="admin-users__dialog admin-users__dialog--edit" role="dialog" aria-modal="true" aria-labelledby="admin-user-edit-title">
            <header className="admin-users__dialog-header">
              <div>
                <p className="admin-users__eyebrow">CHỈNH SỬA TÀI KHOẢN</p>
                <h2 id="admin-user-edit-title">Cập nhật người dùng</h2>
              </div>
              <button className="admin-users__close" type="button" onClick={() => setEditingUser(null)} disabled={isSaving} aria-label="Đóng">×</button>
            </header>
            <form className="admin-users__form" onSubmit={(event) => { void saveUser(event); }}>
              {editorError && <p className="admin-users__message admin-users__message--error" role="alert">{editorError}</p>}
              <label>
                <span>Họ tên</span>
                <input value={fullName} onChange={(event) => setFullName(event.target.value)} maxLength={100} required />
              </label>
              <label>
                <span>Email</span>
                <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} maxLength={256} required />
              </label>
              <label>
                <span>Mật khẩu mới</span>
                <input
                  type="password"
                  autoComplete="new-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  minLength={8}
                  maxLength={72}
                  placeholder="Để trống nếu không thay đổi"
                />
                <small>Ít nhất 8 ký tự. Để trống để giữ mật khẩu hiện tại.</small>
              </label>
              {password && (
                <label>
                  <span>Xác nhận mật khẩu mới</span>
                  <input
                    type="password"
                    autoComplete="new-password"
                    value={confirmPassword}
                    onChange={(event) => setConfirmPassword(event.target.value)}
                    minLength={8}
                    maxLength={72}
                    required
                  />
                </label>
              )}
              <label>
                <span>Vai trò</span>
                <select
                  value={isAdmin ? 'admin' : 'user'}
                  onChange={(event) => setIsAdmin(event.target.value === 'admin')}
                  disabled={editingUser.id === currentUser?.id}
                >
                  <option value="user">Người dùng</option>
                  <option value="admin">Admin</option>
                </select>
                {editingUser.id === currentUser?.id && <small>Không thể tự hạ quyền tài khoản đang đăng nhập.</small>}
              </label>
              <footer>
                <button className="admin-users__button admin-users__button--secondary" type="button" onClick={() => setEditingUser(null)} disabled={isSaving}>Hủy</button>
                <button className="admin-users__button" type="submit" disabled={isSaving}>{isSaving ? 'Đang lưu...' : 'Lưu thay đổi'}</button>
              </footer>
            </form>
          </section>
        </div>
      )}
    </section>
  );
}
