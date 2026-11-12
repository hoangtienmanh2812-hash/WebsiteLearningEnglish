import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { clearAuthSession, getAuthUser, updateAuthUser } from '../auth/authStorage';
import {
  changeAccountPassword,
  getAccountApiError,
  getAccountProfile,
  updateAccountProfile,
  type AccountProfile,
} from './accountApi';
import './account.css';

export default function AccountPage() {
  const navigate = useNavigate();
  const storedUser = getAuthUser();
  const [profile, setProfile] = useState<AccountProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [fullName, setFullName] = useState(storedUser?.fullName ?? '');
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [profileError, setProfileError] = useState('');
  const [profileNotice, setProfileNotice] = useState('');
  const [passwordError, setPasswordError] = useState('');
  const [passwordNotice, setPasswordNotice] = useState('');
  const displayName = profile?.fullName ?? storedUser?.fullName ?? 'Người học';
  const initial = displayName.trim().charAt(0).toUpperCase() || 'E';

  useEffect(() => {
    let active = true;
    getAccountProfile()
      .then((result) => {
        if (active) {
          setProfile(result);
          setFullName(result.fullName);
        }
      })
      .catch((error: unknown) => {
        if (active) setProfileError(getAccountApiError(error));
      })
      .finally(() => {
        if (active) setIsLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const saveProfile = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setProfileError('');
    setProfileNotice('');
    const normalizedName = fullName.trim();
    if (!normalizedName || normalizedName.length > 100) {
      setProfileError('Tên hiển thị phải từ 1 đến 100 ký tự.');
      return;
    }

    setIsSavingProfile(true);
    try {
      await updateAccountProfile(normalizedName);
      updateAuthUser({ fullName: normalizedName });
      setProfile((previous) => previous ? { ...previous, fullName: normalizedName } : previous);
      setProfileNotice('Thông tin cá nhân đã được cập nhật.');
    } catch (error: unknown) {
      setProfileError(getAccountApiError(error));
    } finally {
      setIsSavingProfile(false);
    }
  };

  const savePassword = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPasswordError('');
    setPasswordNotice('');
    if (newPassword.length < 8) {
      setPasswordError('Mật khẩu mới cần có ít nhất 8 ký tự.');
      return;
    }
    if (new TextEncoder().encode(newPassword).length > 72) {
      setPasswordError('Mật khẩu mới không được vượt quá 72 byte UTF-8.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setPasswordError('Mật khẩu xác nhận chưa khớp.');
      return;
    }

    setIsChangingPassword(true);
    try {
      await changeAccountPassword(currentPassword, newPassword);
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setPasswordNotice('Mật khẩu đã được thay đổi thành công.');
    } catch (error: unknown) {
      setPasswordError(getAccountApiError(error));
    } finally {
      setIsChangingPassword(false);
    }
  };

  const logout = () => {
    clearAuthSession();
    navigate('/login', { replace: true });
  };

  return (
    <section className="account-page" aria-labelledby="account-title">
      <header className="account-page__header">
        <div>
          <p className="account-page__eyebrow">KHÔNG GIAN CỦA BẠN</p>
          <h1 id="account-title">Quản lý tài khoản</h1>
          <p>Xem tiến độ học tập và cập nhật thông tin bảo mật của bạn.</p>
        </div>
        <button className="account-page__logout" type="button" onClick={logout}>
          Đăng xuất
        </button>
      </header>

      {isLoading ? (
        <div className="account-page__loading" role="status">Đang tải thông tin tài khoản...</div>
      ) : (
        <>
          {profileError && !profile && (
            <p className="account-page__message account-page__message--error" role="alert">{profileError}</p>
          )}
          <div className="account-page__layout">
            <aside className="account-page__identity-card">
              <div className="account-page__avatar" aria-hidden="true">{initial}</div>
              <h2>{displayName}</h2>
              <p>{profile?.email ?? storedUser?.email}</p>
              <span className="account-page__member-since">
                Thành viên từ{' '}
                {profile?.createdAt
                  ? new Date(profile.createdAt).toLocaleDateString('vi-VN', { year: 'numeric', month: 'long' })
                  : '—'}
              </span>
              <div className="account-page__identity-note">
                Email đang được dùng để đăng nhập và chưa thể thay đổi tại đây.
              </div>
            </aside>

            <div className="account-page__main">
              <section className="account-page__stats" aria-label="Thống kê học tập">
                <article className="account-page__stat">
                  <span className="account-page__stat-icon" aria-hidden="true">✦</span>
                  <div><strong>{profile?.exp ?? 0}</strong><span>Điểm kinh nghiệm</span></div>
                </article>
                <article className="account-page__stat">
                  <span className="account-page__stat-icon" aria-hidden="true">⌁</span>
                  <div><strong>{profile?.streakCount ?? 0} ngày</strong><span>Chuỗi học liên tiếp</span></div>
                </article>
                <article className="account-page__stat">
                  <span className="account-page__stat-icon" aria-hidden="true">✓</span>
                  <div>
                    <strong>{profile?.completedLessons ?? 0}/{profile?.totalLessons ?? 0}</strong>
                    <span>Bài học hoàn thành</span>
                  </div>
                </article>
              </section>

              <section className="account-page__card" aria-labelledby="account-profile-heading">
                <div className="account-page__section-heading">
                  <span className="account-page__section-icon" aria-hidden="true">◎</span>
                  <div>
                    <h2 id="account-profile-heading">Thông tin cá nhân</h2>
                    <p>Cập nhật tên hiển thị trên hồ sơ của bạn.</p>
                  </div>
                </div>
                <form className="account-page__form" onSubmit={(event) => { void saveProfile(event); }}>
                  {profileError && profile && (
                    <p className="account-page__message account-page__message--error" role="alert">{profileError}</p>
                  )}
                  {profileNotice && (
                    <p className="account-page__message account-page__message--success" role="status">{profileNotice}</p>
                  )}
                  <label className="account-page__field">
                    <span>Tên hiển thị</span>
                    <input
                      value={fullName}
                      onChange={(event) => setFullName(event.target.value)}
                      autoComplete="name"
                      maxLength={100}
                      required
                    />
                  </label>
                  <label className="account-page__field">
                    <span>Email</span>
                    <input value={profile?.email ?? storedUser?.email ?? ''} readOnly disabled />
                  </label>
                  <div className="account-page__actions">
                    <button className="account-page__button" type="submit" disabled={isSavingProfile}>
                      {isSavingProfile ? 'Đang lưu...' : 'Lưu thông tin'}
                    </button>
                  </div>
                </form>
              </section>

              <section className="account-page__card" aria-labelledby="account-security-heading">
                <div className="account-page__section-heading">
                  <span className="account-page__section-icon account-page__section-icon--security" aria-hidden="true">⌑</span>
                  <div>
                    <h2 id="account-security-heading">Bảo mật tài khoản</h2>
                    <p>Đổi mật khẩu định kỳ để bảo vệ tài khoản của bạn.</p>
                  </div>
                </div>
                <form className="account-page__form" onSubmit={(event) => { void savePassword(event); }}>
                  {passwordError && (
                    <p className="account-page__message account-page__message--error" role="alert">{passwordError}</p>
                  )}
                  {passwordNotice && (
                    <p className="account-page__message account-page__message--success" role="status">{passwordNotice}</p>
                  )}
                  <label className="account-page__field">
                    <span>Mật khẩu hiện tại</span>
                    <input
                      type="password"
                      value={currentPassword}
                      onChange={(event) => setCurrentPassword(event.target.value)}
                      autoComplete="current-password"
                      required
                    />
                  </label>
                  <div className="account-page__password-fields">
                    <label className="account-page__field">
                      <span>Mật khẩu mới</span>
                      <input
                        type="password"
                        value={newPassword}
                        onChange={(event) => setNewPassword(event.target.value)}
                        autoComplete="new-password"
                        minLength={8}
                        required
                      />
                    </label>
                    <label className="account-page__field">
                      <span>Xác nhận mật khẩu mới</span>
                      <input
                        type="password"
                        value={confirmPassword}
                        onChange={(event) => setConfirmPassword(event.target.value)}
                        autoComplete="new-password"
                        minLength={8}
                        required
                      />
                    </label>
                  </div>
                  <p className="account-page__hint">Mật khẩu cần tối thiểu 8 ký tự.</p>
                  <div className="account-page__actions">
                    <button className="account-page__button" type="submit" disabled={isChangingPassword}>
                      {isChangingPassword ? 'Đang cập nhật...' : 'Đổi mật khẩu'}
                    </button>
                  </div>
                </form>
              </section>
            </div>
          </div>
        </>
      )}
    </section>
  );
}
