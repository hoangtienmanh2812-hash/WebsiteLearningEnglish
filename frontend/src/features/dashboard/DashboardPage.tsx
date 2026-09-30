import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getAuthUser } from '../auth/authStorage';
import './dashboard.css';

interface LessonProgress {
  lessonId: number;
  isUnlocked: boolean;
  isCompleted: boolean;
}

export default function DashboardPage() {
  const user = getAuthUser();
  const navigate = useNavigate();
  const [lessonsProgress, setLessonsProgress] = useState<LessonProgress[]>([]);

  // Ép kiểu user.fullName để tránh lỗi TypeScript nếu user có thể là null
  const userName = user?.fullName || 'bạn';
  const firstName = userName.trim().split(/\s+/).at(-1) || 'bạn';
  
  const today = new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(new Date());

  const lessonIcons = ['📖', '🎧', '🗣️️', '📝', '📚', '🧩', '🎯', '🏆', '🚀', '🎁'];

  useEffect(() => {
    if (user?.id) {
      // Đảm bảo URL API trỏ đúng cổng Backend của bạn (ví dụ: port 5000 hoặc 5173 tùy cấu hình)
      fetch(`http://localhost:5219/api/progress/${user.id}`)
        .then((res) => res.json())
        .then((data: LessonProgress[]) => {
          if (Array.isArray(data)) {
            setLessonsProgress(data);
          }
        })
        .catch((err) => console.error("Lỗi khi tải tiến độ học:", err));
    }
  }, [user]);

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

      {/* --- KHU VỰC TIẾN ĐỘ HỌC TẬP (CUỘN NGANG) --- */}
      <div className="learning-path-section">
        <h3 className="section-title">TIẾN ĐỘ HÀNH TRÌNH CỦA BẠN</h3>
        
        <div className="path-container">
          <svg className="path-svg" viewBox="0 0 2000 150" preserveAspectRatio="none">
            <path d="M 50 75 Q 150 150 250 75 T 450 75 T 650 75 T 850 75 T 1050 75 T 1250 75 T 1450 75 T 1650 75 T 1850 75" 
                fill="none" stroke="#e5e7eb" strokeWidth="8" strokeLinecap="round"/>
          </svg>
          
          <div className="nodes-row">
            {Array.from({ length: 10 }, (_, index) => {
              const lessonId = index + 1;
              const progress = lessonsProgress.find((p) => p.lessonId === lessonId);
              const isUnlocked = lessonId === 1 ? true : (progress ? progress.isUnlocked : false);
              const isCompleted = progress ? progress.isCompleted : false;
              
              const isUp = lessonId % 2 !== 0;
              const nodeClass = `node ${isUp ? 'up' : 'down'} ${isCompleted ? 'completed' : ''} ${isUnlocked ? 'active' : 'locked'}`;

              return (
                <div 
                  key={lessonId}
                  className={nodeClass}
                  onClick={() => {
                    if (isUnlocked) {
                      navigate(`/quiz/${lessonId}`);
                    } else {
                      alert("Bạn cần hoàn thành bài học trước để mở khóa bài này!");
                    }
                  }}
                  style={{ cursor: isUnlocked ? 'pointer' : 'not-allowed', opacity: isUnlocked ? 1 : 0.5 }}
                >
                  <div className="circle">
                    {isUnlocked ? lessonIcons[index] : '🔒'}
                  </div>
                  <span>{lessonId === 10 ? 'Thưởng' : `Bài ${lessonId}`}</span>
                </div>
              );
            })}
          </div>
        </div>
      </div>
      
    </section>
  );
}