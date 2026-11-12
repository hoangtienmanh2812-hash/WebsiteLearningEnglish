import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getAuthToken, getAuthUser } from '../auth/authStorage';
import { SkeletonBlock } from '../../components/common/Skeleton';
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
  const [loadedProgressUserId, setLoadedProgressUserId] = useState<string | null>(null);
  const userId = user?.id;
  const isLoadingProgress = Boolean(userId && loadedProgressUserId !== userId);
  const lessonIcons = ['📖', '🎧', '🗣️️', '📝', '📚', '🧩', '🎯', '🏆', '🚀', '🎁'];
  const firstUnlockedIndex = lessonsProgress.findIndex((progress) => progress.isUnlocked && !progress.isCompleted);

  useEffect(() => {
    if (!userId) {
      return;
    }

    const activeUserId = userId;
    const controller = new AbortController();
    async function loadLessonProgress() {
      try {
        const response = await fetch(`http://localhost:5219/api/progress/${activeUserId}`, {
          signal: controller.signal,
        });
        if (!response.ok) {
          throw new Error(`Không thể tải tiến độ học (HTTP ${response.status}).`);
        }

        const data: LessonProgress[] = await response.json();
        if (Array.isArray(data)) {
          setLessonsProgress(data);
        }
      } catch (caughtError) {
        if (!controller.signal.aborted) {
          console.error('Lỗi khi tải tiến độ học:', caughtError);
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoadedProgressUserId(activeUserId);
        }
      }
    }

    void loadLessonProgress();
    return () => controller.abort();
  }, [userId]);

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
    <section
      className={`dashboard${isLoadingProgress ? ' dashboard--loading' : ''}`}
      aria-label="Lộ trình học tập"
      aria-busy={isLoadingProgress}
    >
      {/* --- KHU VỰC TIẾN ĐỘ HỌC TẬP (CUỘN NGANG) --- */}
      <div className="learning-path-section">
        <div className="path-container">
          {isLoadingProgress ? (
            <div className="nodes-row dashboard-skeleton__nodes" role="status" aria-label="Đang tải lộ trình học">
              {Array.from({ length: 6 }, (_, index) => (
                <div
                  className="node dashboard-skeleton__node"
                  key={index}
                  style={{ gridRow: index + 1 }}
                >
                  <SkeletonBlock
                    className="skeleton-block--circle dashboard-skeleton__circle"
                    height="clamp(74px, 8vw, 94px)"
                    width="clamp(74px, 8vw, 94px)"
                  />
                  <SkeletonBlock height="11px" width="42px" />
                  <SkeletonBlock height="13px" width={`${48 + ((index * 13) % 35)}%`} />
                </div>
              ))}
            </div>
          ) : (
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
          )}
        </div>
      </div>
      
    </section>
  );
}
