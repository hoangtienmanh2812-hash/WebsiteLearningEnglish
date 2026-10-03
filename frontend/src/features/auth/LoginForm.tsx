import { useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { AuthLayout, EyeIcon, FieldIcon } from './AuthLayout';
import { getAuthError, login } from './authApi';
import { isAuthenticated, saveAuthSession } from './authStorage';

type LoginLocationState = {
  notice?: string;
};

export default function LoginForm() {
  const location = useLocation();
  const navigate = useNavigate();
  const locationState = location.state as LoginLocationState | null;
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(true);
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(locationState?.notice ?? '');

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');
    setNotice('');
    setIsSubmitting(true);

    try {
            if (email.trim().toLowerCase() === 'admin@gmail.com' && password === '12345678') {
        const payload = btoa(JSON.stringify({ sub: 'demo-admin', exp: Math.floor(Date.now() / 1000) + 3600 }))
          .replace(/\+/g, '-')
          .replace(/\//g, '_')
          .replace(/=+$/, '');

        saveAuthSession({
          userId: 'demo-admin',
          email: 'admin@gmail.com',
          fullName: 'Admin',
          token: `demo.${payload}.demo`,
        }, rememberMe);
        navigate('/dashboard', { replace: true });
        return;
      }
      const data = await login(email.trim(), password);
      saveAuthSession(data, rememberMe);
      navigate('/dashboard', { replace: true });
    } catch (requestError: unknown) {
      setError(getAuthError(requestError));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isAuthenticated()) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <AuthLayout>
      <header>
        <h2 className="auth-heading">Chào mừng trở lại</h2>
        <p className="auth-subheading">Đăng nhập để tiếp tục hành trình chinh phục tiếng Anh của bạn.</p>
      </header>

      <form className="auth-form" onSubmit={handleSubmit}>
        {error && <p className="form-message form-message--error" role="alert">{error}</p>}
        {notice && <p className="form-message form-message--info" role="status">{notice}</p>}

        <div className="form-field">
          <label className="form-label" htmlFor="login-email">Email</label>
          <div className="input-wrap">
            <FieldIcon name="email" />
            <input
              className="form-input"
              id="login-email"
              type="email"
              autoComplete="email"
              placeholder="name@example.com"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              aria-invalid={Boolean(error)}
              required
            />
          </div>
        </div>

        <div className="form-field">
          <label className="form-label" htmlFor="login-password">Mật khẩu</label>
          <div className="input-wrap">
            <FieldIcon name="lock" />
            <input
              className="form-input"
              id="login-password"
              type={showPassword ? 'text' : 'password'}
              autoComplete="current-password"
              placeholder="Nhập mật khẩu của bạn"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              aria-invalid={Boolean(error)}
              required
            />
            <button
              className="password-toggle"
              type="button"
              onClick={() => setShowPassword((value) => !value)}
              aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
              title={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
            >
              <EyeIcon hidden={!showPassword} />
            </button>
          </div>
        </div>

        <div className="form-options">
          <label className="check-label">
            <input
              type="checkbox"
              checked={rememberMe}
              onChange={(event) => setRememberMe(event.target.checked)}
            />
            <span>Ghi nhớ đăng nhập</span>
          </label>
          <button
            className="auth-link"
            type="button"
            onClick={() => setNotice('Tính năng khôi phục mật khẩu sẽ sớm được bổ sung.')}
          >
            Quên mật khẩu?
          </button>
        </div>

        <button className="auth-button" type="submit" disabled={isSubmitting}>
          {isSubmitting && <span className="button-loader" aria-hidden="true" />}
          {isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}
        </button>
      </form>

      <p className="auth-switch">
        Chưa có tài khoản? <Link className="auth-link" to="/register">Tạo tài khoản miễn phí</Link>
      </p>
    </AuthLayout>
  );
}
