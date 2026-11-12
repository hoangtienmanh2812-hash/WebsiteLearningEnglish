import { useState, useEffect } from 'react';
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
  const lessonIcons = ['📖', '🎧', '🗣️️', '📝', '📚', '🧩', '🎯', '🏆', '🚀', '🎁'];
  const firstUnlockedIndex = lessonsProgress.findIndex((progress) => progress.isUnlocked && !progress.isCompleted);

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

  return (
    <section className="dashboard" aria-labelledby="dashboard-title">
      {/* --- KHU VỰC TIẾN ĐỘ HỌC TẬP (CUỘN NGANG) --- */}
      <div className="learning-path-section">
        <div className="path-container">
          <div className="nodes-row" aria-label="Lộ trình các cấp độ học">
            {lessonsProgress.map((progress, index) => {
              const lessonId = progress.lessonId;
              const isUnlocked = progress.isUnlocked;
              const isCompleted = progress.isCompleted;
              const isStartingLesson = index === firstUnlockedIndex;
              const displayTitle = progress.title.replace(/^Bài\s*\d+\s*:?\s*/i, '') || `Bài ${index + 1}`;
              const nodeClass = `node ${isCompleted ? 'completed' : ''} ${isUnlocked ? 'unlocked' : 'locked'}`;

              return (
                <button
                  type="button"
                  key={lessonId}
                  className={nodeClass}
                  style={{ gridRow: index + 1 }}
                  disabled={!isUnlocked}
                  aria-current={isStartingLesson ? 'step' : undefined}
                  aria-label={`${progress.title}. ${isCompleted ? 'Đã hoàn thành' : isUnlocked ? 'Sẵn sàng học' : 'Chưa mở khóa'}`}
                  onClick={() => {
                    navigate(`/quiz/${lessonId}`);
                  }}
                >
                  <div className="circle">
                    {isCompleted ? '✓' : isUnlocked ? isStartingLesson ? '★' : lessonIcons[index % lessonIcons.length] : '🔒'}
                  </div>
                  {isStartingLesson && <span className="node__start" aria-hidden="true">Bắt đầu</span>}
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
