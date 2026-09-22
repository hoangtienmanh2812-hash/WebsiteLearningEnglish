import { getAuthUser } from '../auth/authStorage';
import './dashboard.css';

type DashboardIconName = 'lesson' | 'words' | 'exercise' | 'chat';

function DashboardIcon({ name }: { name: DashboardIconName }) {
  if (name === 'lesson') {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" />
        <path d="M4 18.5A2.5 2.5 0 0 1 6.5 16H20M8 7h8M8 10h6" />
      </svg>
    );
  }

  if (name === 'words') {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M4 4h16v12H9l-5 4V4Z" />
        <path d="M8 8h8M8 11h5" />
      </svg>
    );
  }

  if (name === 'exercise') {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <rect x="5" y="3" width="14" height="18" rx="2" />
        <path d="m8.5 12 2 2 4.5-5M8.5 17h7M9 6h6" />
      </svg>
    );
  }

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M20 11.5a7.7 7.7 0 0 1-8 7.5 8.8 8.8 0 0 1-3.1-.6L4 20l1.5-4.1A7.2 7.2 0 0 1 4 11.5 7.7 7.7 0 0 1 12 4a7.7 7.7 0 0 1 8 7.5Z" />
      <path d="M8.5 11.5h.01M12 11.5h.01M15.5 11.5h.01" />
    </svg>
  );
}

const featureCards: { title: string; description: string; icon: DashboardIconName; accent: string }[] = [
  { title: 'Bài học', description: 'Các bài học theo lộ trình sẽ xuất hiện tại đây.', icon: 'lesson', accent: 'coral' },
  { title: 'Từ vựng', description: 'Không gian ghi nhớ từ mới của bạn đang được chuẩn bị.', icon: 'words', accent: 'yellow' },
  { title: 'Bài tập', description: 'Luyện tập và củng cố kiến thức trong một nơi riêng.', icon: 'exercise', accent: 'rose' },
  { title: 'Chatbot', description: 'Người bạn đồng hành luyện tiếng Anh sẽ sớm có mặt.', icon: 'chat', accent: 'peach' },
];

export default function DashboardPage() {
  const user = getAuthUser();
  const firstName = user?.fullName.trim().split(/\s+/).at(-1) || 'bạn';
  const today = new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(new Date());

  return (
    <section className="dashboard" aria-labelledby="dashboard-title">
      <p className="dashboard__date">{today}</p>

      <div className="dashboard-hero">
        <div className="dashboard-hero__copy">
          <span className="dashboard-hero__eyebrow">✦ KHÔNG GIAN HỌC TẬP CỦA BẠN</span>
          <h1 id="dashboard-title">Chào {firstName}, hôm nay mình học gì nhỉ?</h1>
          <p>
            Chọn một khu vực để bắt đầu. Những tính năng học tập sẽ được mở dần trong hành trình của bạn.
          </p>
          <div className="dashboard-hero__note">
            <span aria-hidden="true">✦</span>
            <span>Học một chút mỗi ngày, tiến bộ một cách bền vững.</span>
          </div>
        </div>

        <div className="dashboard-hero__art" aria-hidden="true">
          <div className="dashboard-hero__shape dashboard-hero__shape--one" />
          <div className="dashboard-hero__shape dashboard-hero__shape--two" />
          <div className="dashboard-hero__flashcard">
            <small>WORD OF THE DAY</small>
            <strong>Shine</strong>
            <span>/ʃaɪn/</span>
          </div>
          <div className="dashboard-hero__sun">✦</div>
          <div className="dashboard-hero__mini-card">You can do it!</div>
        </div>
      </div>

      <section className="dashboard-features" aria-labelledby="learning-areas-title">
        <div className="dashboard-features__heading">
          <div>
            <p className="section-kicker">KHÁM PHÁ</p>
            <h2 id="learning-areas-title">Khu vực học tập</h2>
          </div>
          <span className="dashboard-features__caption">Đang được xây dựng</span>
        </div>

        <div className="feature-grid">
          {featureCards.map((feature) => (
            <article className="feature-card" key={feature.title}>
              <span className={`feature-card__icon feature-card__icon--${feature.accent}`}>
                <DashboardIcon name={feature.icon} />
              </span>
              <div>
                <h3>{feature.title}</h3>
                <p>{feature.description}</p>
              </div>
              <span className="feature-card__status">Sắp ra mắt</span>
            </article>
          ))}
        </div>
      </section>
    </section>
  );
}
