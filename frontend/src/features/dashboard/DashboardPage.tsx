import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { getAuthToken, getAuthUser } from '../auth/authStorage';
import './dashboard.css';

interface LessonProgress {
  lessonId: number;
  title: string;
  isUnlocked: boolean;
  isCompleted: boolean;
}

export default function DashboardPage() {
  const user = getAuthUser();
  const navigate = useNavigate();
  const [lessonsProgress, setLessonsProgress] = useState<LessonProgress[]>([]);
  const pathContainerRef = useRef<HTMLDivElement>(null);

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
  }, [user?.id]);

  useEffect(() => {
    const token = getAuthToken();
    if (!user?.id || !token) return;

    fetch(`http://localhost:5219/api/Gamification/checkin-streak/${user.id}`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error('Không thể ghi nhận điểm danh streak hôm nay.');
        }
        await response.json();
      })
      .catch((caughtError: unknown) => {
        console.error('Không thể ghi nhận điểm danh streak:', caughtError);
      });
  }, [user?.id]);

  useEffect(() => () => {
    pathContainerRef.current
      ?.closest<HTMLElement>('.main-layout__content')
      ?.style.setProperty('--dashboard-background-shift', '0px');
  }, []);

  const handlePathScroll = (event: React.UIEvent<HTMLDivElement>) => {
    const container = event.currentTarget;
    const maxScroll = container.scrollWidth - container.clientWidth;
    const scrollProgress = maxScroll > 0 ? container.scrollLeft / maxScroll : 0;
    const backgroundShift = Math.min(Math.max(scrollProgress, 0), 1) * 42;

    container
      .closest<HTMLElement>('.main-layout__content')
      ?.style.setProperty('--dashboard-background-shift', `${backgroundShift}px`);
  };

  return (
    <section className="dashboard" aria-labelledby="dashboard-title">
      {/* --- KHU VỰC TIẾN ĐỘ HỌC TẬP (CUỘN NGANG) --- */}
      <div className="learning-path-section">
        <div className="path-container" ref={pathContainerRef} onScroll={handlePathScroll}>
          <div className="nodes-row">
            {lessonsProgress.map((progress, index) => {
              const lessonId = progress.lessonId;
              const isUnlocked = progress.isUnlocked;
              const isCompleted = progress.isCompleted;
              const displayTitle = progress.title.replace(/^Bài\s*\d+\s*:?\s*/i, '') || `Bài ${index + 1}`;
              const nodeClass = `node ${isCompleted ? 'completed' : ''} ${isUnlocked ? 'unlocked' : 'locked'}`;

              return (
                <button
                  type="button"
                  key={lessonId}
                  className={nodeClass}
                  disabled={!isUnlocked}
                  aria-label={`${progress.title}. ${isCompleted ? 'Đã hoàn thành' : isUnlocked ? 'Sẵn sàng học' : 'Chưa mở khóa'}`}
                  onClick={() => {
                    navigate(`/quiz/${lessonId}`);
                  }}
                >
                  <div className="circle">
                    {isCompleted ? '✓' : isUnlocked ? lessonIcons[index % lessonIcons.length] : '🔒'}
                  </div>
                  <span className="node__order">Bài {index + 1}</span>
                  <span className="node__title">{displayTitle}</span>
                </button>
              );
            })}
          </div>
        </div>
      </div>
      
    </section>
  );
}
