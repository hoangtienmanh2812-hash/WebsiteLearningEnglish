import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthLayout, EyeIcon, FieldIcon } from './AuthLayout';
import { getAuthError, register } from './authApi';

export default function RegisterForm() {
  const navigate = useNavigate();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');

    if (password.length < 8) {
      setError('Mật khẩu cần có ít nhất 8 ký tự.');
      return;
    }

    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận chưa khớp. Vui lòng kiểm tra lại.');
      return;
    }

    setIsSubmitting(true);
    try {
      await register(fullName.trim(), email.trim(), password);
      navigate('/login', {
        replace: true,
        state: { notice: 'Tạo tài khoản thành công! Hãy đăng nhập để bắt đầu học.' },
      });
    } catch (requestError: unknown) {
      setError(getAuthError(requestError));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthLayout>
      <header>
        <h2 className="auth-heading">Bắt đầu cùng chúng mình</h2>
        <p className="auth-subheading">Tạo tài khoản miễn phí và xây dựng thói quen học tiếng Anh của riêng bạn.</p>
      </header>

      <form className="auth-form" onSubmit={handleSubmit}>
        {error && <p className="form-message form-message--error" role="alert">{error}</p>}

        <div className="form-field">
          <label className="form-label" htmlFor="register-name">Họ và tên</label>
          <div className="input-wrap">
            <FieldIcon name="user" />
            <input
              className="form-input"
              id="register-name"
              type="text"
              autoComplete="name"
              placeholder="Ví dụ: Nguyễn Minh Anh"
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              required
            />
          </div>
        </div>

        <div className="form-field">
          <label className="form-label" htmlFor="register-email">Email</label>
          <div className="input-wrap">
            <FieldIcon name="email" />
            <input
              className="form-input"
              id="register-email"
              type="email"
              autoComplete="email"
              placeholder="name@example.com"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
          </div>
        </div>

        <div className="form-field">
          <label className="form-label" htmlFor="register-password">Mật khẩu</label>
          <div className="input-wrap">
            <FieldIcon name="lock" />
            <input
              className="form-input"
              id="register-password"
              type={showPassword ? 'text' : 'password'}
              autoComplete="new-password"
              placeholder="Tạo mật khẩu an toàn"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
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
          <p className="password-hint">Ít nhất 8 ký tự.</p>
        </div>

        <div className="form-field">
          <label className="form-label" htmlFor="register-confirm-password">Xác nhận mật khẩu</label>
          <div className="input-wrap">
            <FieldIcon name="lock" />
            <input
              className="form-input"
              id="register-confirm-password"
              type={showConfirmPassword ? 'text' : 'password'}
              autoComplete="new-password"
              placeholder="Nhập lại mật khẩu"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              required
            />
            <button
              className="password-toggle"
              type="button"
              onClick={() => setShowConfirmPassword((value) => !value)}
              aria-label={showConfirmPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
              title={showConfirmPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
            >
              <EyeIcon hidden={!showConfirmPassword} />
            </button>
          </div>
        </div>

        <button className="auth-button" type="submit" disabled={isSubmitting}>
          {isSubmitting && <span className="button-loader" aria-hidden="true" />}
          {isSubmitting ? 'Đang tạo tài khoản...' : 'Tạo tài khoản'}
        </button>
      </form>

      <p className="auth-switch">
        Đã có tài khoản? <Link className="auth-link" to="/login">Đăng nhập</Link>
      </p>
      <p className="auth-terms">
        Khi tiếp tục, bạn đồng ý với Điều khoản sử dụng và Chính sách bảo mật của English Learning.
      </p>
    </AuthLayout>
  );
}
