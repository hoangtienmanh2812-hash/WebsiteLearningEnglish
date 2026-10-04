import type { ReactNode } from 'react';
import './auth.css';

type AuthLayoutProps = {
  children: ReactNode;
};

function Brand({ mobile = false }: { mobile?: boolean }) {
  return (
    <a className={`brand ${mobile ? 'brand--mobile' : ''}`} href="/login" aria-label="English Learning">
      
    </a>
  );
}

export function AuthLayout({ children }: AuthLayoutProps) {
  return (
    <main className="auth-page">
      <section className="auth-visual" aria-label="Học tiếng Anh mỗi ngày">
        <Brand />

        <div className="auth-visual__copy">
          <h2>Học tiếng anh mỗi ngày</h2>
          <h1>Chạm tới sự tự tin khi dùng Tiếng Anh</h1>
          <p>
            Bài học ngắn gọn, lộ trình dành riêng cho bạn và cảm hứng để duy trì mỗi ngày.
          </p>
        </div>

      </section>

      <section className="auth-content">
        <Brand mobile />
        <div className="auth-card">{children}</div>
      </section>
    </main>
  );
}

export function FieldIcon({ name }: { name: 'email' | 'lock' | 'user' }) {
  if (name === 'email') {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <rect x="3" y="5" width="18" height="14" rx="2" />
        <path d="m4 7 8 6 8-6" />
      </svg>
    );
  }

  if (name === 'lock') {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <rect x="4" y="10" width="16" height="10" rx="2" />
        <path d="M8 10V7a4 4 0 0 1 8 0v3m-4 4v2" />
      </svg>
    );
  }

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="12" cy="8" r="3.5" />
      <path d="M4.5 20c.9-3.2 3.4-5 7.5-5s6.6 1.8 7.5 5" />
    </svg>
  );
}

export function EyeIcon({ hidden }: { hidden: boolean }) {
  return hidden ? (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M3 3 21 21M10.6 10.7a2 2 0 0 0 2.7 2.7M9.9 5.1A10.8 10.8 0 0 1 12 4.9c5 0 8.5 4.4 9.4 6.1a1.9 1.9 0 0 1 0 2c-.5.9-1.6 2.4-3.2 3.7M6.1 6.3C4.3 7.7 3.1 9.5 2.6 11a1.9 1.9 0 0 0 0 2C3.5 14.7 7 19.1 12 19.1c.9 0 1.7-.1 2.5-.4" />
    </svg>
  ) : (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M2.6 11a1.9 1.9 0 0 0 0 2c.9 1.7 4.4 6.1 9.4 6.1s8.5-4.4 9.4-6.1a1.9 1.9 0 0 0 0-2C20.5 9.3 17 4.9 12 4.9S3.5 9.3 2.6 11Z" />
      <circle cx="12" cy="12" r="3.2" />
    </svg>
  );
}
