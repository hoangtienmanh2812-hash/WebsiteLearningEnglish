import { useEffect, useState } from 'react';
import axios from 'axios';
import { SkeletonBlock } from '../../components/common/Skeleton';
import { getAuthToken, getAuthUser } from '../auth/authStorage';
import './dailyTasks.css';

interface Vocab {
  id: number;
  word: string;
  meaning: string;
  phonetic: string;
  exampleSentence: string;
}

interface DailyPackResponse {
  title: string;
  vocabularies: Vocab[];
  learnedVocabIds?: number[];
  xpPerVocabulary: number;
}

const levelGoals = [10, 20, 30];

export const DailyTasks = () => {
  const [vocabs, setVocabs] = useState<Vocab[]>([]);
  const [learnedIds, setLearnedIds] = useState<number[]>([]);
  const [title, setTitle] = useState<string>('');
  const [currentIndex, setCurrentIndex] = useState(0);
  const [showMeaning, setShowMeaning] = useState(false);
  const [dayNumber, setDayNumber] = useState(1);
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [xpPerVocabulary, setXpPerVocabulary] = useState<number | null>(null);
  const userId = getAuthUser()?.id;

  useEffect(() => {
    async function fetchDailyPack() {
      setLoading(true);
      setError(null);
      setVocabs([]);

      if (!userId) {
        setError('Không tìm thấy thông tin tài khoản. Vui lòng đăng nhập lại.');
        setLoading(false);
        return;
      }

      try {
        const response = await axios.get<DailyPackResponse>(
          `http://localhost:5219/api/Gamification/daily-vocab-json/${userId}/${dayNumber}`,
          { headers: authorizationHeaders() },
        );
        if (!Array.isArray(response.data.vocabularies)) {
          throw new Error('Dữ liệu gói từ vựng không đúng định dạng.');
        }

        setTitle(response.data.title);
        setVocabs(response.data.vocabularies);
        setLearnedIds(response.data.learnedVocabIds ?? []);
        setXpPerVocabulary(response.data.xpPerVocabulary);
        setCurrentIndex(0);
        setShowMeaning(false);
      } catch (err) {
        console.error('Lỗi tải từ vựng:', err);
        setError('Không thể tải nhiệm vụ từ vựng. Vui lòng thử lại sau.');
      } finally {
        setLoading(false);
      }
    }

    void fetchDailyPack();
  }, [dayNumber, userId]);

  const handleMarkLearned = async (vocabId: number) => {
    if (!userId) {
      setError('Không tìm thấy thông tin tài khoản. Vui lòng đăng nhập lại.');
      return;
    }

    setIsSaving(true);
    setError(null);
    try {
      await axios.post(
        'http://localhost:5219/api/Gamification/mark-vocab-learned',
        { userId, dayNumber, vocabId },
        { headers: authorizationHeaders() },
      );
      setLearnedIds((previousIds) => (
        previousIds.includes(vocabId) ? previousIds : [...previousIds, vocabId]
      ));
      setShowMeaning(false);
      if (currentIndex < vocabs.length - 1) {
        setCurrentIndex((index) => index + 1);
      }
    } catch (err) {
      console.error('Lỗi đánh dấu từ vựng:', err);
      setError('Không thể lưu tiến độ từ vựng. Vui lòng thử lại.');
    } finally {
      setIsSaving(false);
    }
  };

  if (loading) {
    return (
      <section className="daily-tasks daily-tasks--skeleton" role="status" aria-label={`Đang tải nhiệm vụ Ngày ${dayNumber}`}>
        <header className="daily-tasks__header">
          <div className="daily-skeleton__title">
            <SkeletonBlock height="12px" width="150px" />
            <SkeletonBlock height="38px" width="min(340px, 70vw)" />
            <SkeletonBlock height="15px" width="min(390px, 80vw)" />
          </div>
          <SkeletonBlock className="daily-skeleton__picker" height="48px" width="120px" />
        </header>
        <div className="daily-tasks__levels">
          {levelGoals.map((goal) => (
            <article className="daily-level daily-level--skeleton" key={goal}>
              <SkeletonBlock height="13px" width="92px" />
              <SkeletonBlock height="21px" width="140px" />
              <SkeletonBlock className="daily-skeleton__progress" height="27px" width="100%" />
            </article>
          ))}
        </div>
        <section className="daily-word daily-word--skeleton">
          <div className="daily-word__heading">
            <div className="daily-skeleton__word-heading">
              <SkeletonBlock height="12px" width="140px" />
              <SkeletonBlock height="23px" width="200px" />
            </div>
            <SkeletonBlock className="skeleton-block--pill" height="31px" width="64px" />
          </div>
          <SkeletonBlock className="daily-skeleton__card" height="240px" width="100%" />
          <div className="daily-skeleton__actions">
            <SkeletonBlock height="44px" width="100%" />
            <SkeletonBlock height="44px" width="min(420px, 100%)" />
            <SkeletonBlock height="44px" width="100%" />
          </div>
        </section>
      </section>
    );
  }

  if (error && vocabs.length === 0) {
    return <p className="daily-tasks__message daily-tasks__message--error" role="alert">{error}</p>;
  }

  if (vocabs.length === 0) {
    return <p className="daily-tasks__message">Chưa có từ vựng cho Ngày {dayNumber}.</p>;
  }

  const currentVocab = vocabs[currentIndex];
  if (!currentVocab) {
    return <p className="daily-tasks__message daily-tasks__message--error" role="alert">Không tìm thấy từ vựng hiện tại.</p>;
  }

  const currentLearnedCount = learnedIds.filter((id) => vocabs.some((vocab) => vocab.id === id)).length;
  const earnedXp = currentLearnedCount * (xpPerVocabulary ?? 0);
  const isCurrentLearned = learnedIds.includes(currentVocab.id);
  const allWordsLearned = currentLearnedCount === vocabs.length;

  return (
    <section className="daily-tasks" aria-labelledby="daily-tasks-title">
      <header className="daily-tasks__header">
        <div>
          <p className="daily-tasks__eyebrow">MỖI NGÀY MỘT CHÚT TIẾN BỘ</p>
          <h1 id="daily-tasks-title">Nhiệm vụ hàng ngày</h1>
          <p className="daily-tasks__subtitle">Học từ mới, tích lũy EXP và chinh phục từng cấp độ.</p>
        </div>
        <label className="daily-tasks__day-picker">
          <span>Gói học</span>
          <select value={dayNumber} onChange={(event) => setDayNumber(Number(event.target.value))}>
            <option value={1}>Ngày 1</option>
            <option value={2}>Ngày 2</option>
          </select>
        </label>
      </header>

      <section className="daily-tasks__levels" aria-label="Ba cấp độ nhiệm vụ">
        {levelGoals.map((goal, index) => {
          const progress = Math.min(earnedXp, goal);
          const isComplete = earnedXp >= goal;
          const isActive = !isComplete && (index === 0 || earnedXp >= levelGoals[index - 1]);

          return (
            <article
              className={`daily-level${isComplete ? ' daily-level--complete' : ''}${isActive ? ' daily-level--active' : ''}`}
              key={goal}
            >
              <div className="daily-level__topline">
                <span className="daily-level__name">Cấp độ {index + 1}</span>
                <span className={`daily-level__status${isComplete ? ' daily-level__status--complete' : ''}`}>
                  {isComplete ? '✓ Hoàn thành' : `${goal} EXP`}
                </span>
              </div>
              <h2>Kiếm {goal} EXP</h2>
              <div
                className="daily-level__progress"
                role="progressbar"
                aria-label={`Tiến độ cấp độ ${index + 1}`}
                aria-valuemin={0}
                aria-valuemax={goal}
                aria-valuenow={progress}
              >
                <span style={{ width: `${(progress / goal) * 100}%` }} />
                <span className="daily-level__count">{progress} / {goal}</span>
              </div>
            </article>
          );
        })}
      </section>

      <section className="daily-word" aria-labelledby="daily-word-title">
        <div className="daily-word__heading">
          <div>
            <p className="daily-tasks__eyebrow">NHIỆM VỤ TỪ VỰNG</p>
            <h2 id="daily-word-title">{title}</h2>
          </div>
          <span className="daily-word__counter">
            {currentLearnedCount}/{vocabs.length} từ
          </span>
        </div>

        {error && <p className="daily-tasks__inline-error" role="alert">{error}</p>}

        {allWordsLearned ? (
          <div className="daily-word__complete" role="status">
            <span aria-hidden="true">🎉</span>
            <h3>Hoàn thành gói từ vựng!</h3>
            <p>Bạn đã học hết từ mới trong gói này. Hãy quay lại sau để tiếp tục.</p>
          </div>
        ) : (
          <>
            <button
              className={`vocab-card${showMeaning ? ' vocab-card--revealed' : ''}`}
              type="button"
              onClick={() => setShowMeaning((isShown) => !isShown)}
              aria-expanded={showMeaning}
            >
              <span className="vocab-card__hint">{showMeaning ? 'TỪ VỰNG' : 'CHẠM ĐỂ LẬT THẺ'}</span>
              <span className="vocab-card__word">{currentVocab.word}</span>
              {currentVocab.phonetic && <span className="vocab-card__phonetic">{currentVocab.phonetic}</span>}
              {showMeaning ? (
                <span className="vocab-card__definition">
                  <strong>{currentVocab.meaning}</strong>
                  {currentVocab.exampleSentence && <em>“{currentVocab.exampleSentence}”</em>}
                </span>
              ) : (
                <span className="vocab-card__instruction">Thử đoán nghĩa rồi lật thẻ để kiểm tra</span>
              )}
            </button>

            <div className="daily-word__actions">
              <button
                className="daily-word__nav"
                type="button"
                disabled={currentIndex === 0 || isSaving}
                onClick={() => { setCurrentIndex((index) => index - 1); setShowMeaning(false); }}
              >
                ← Từ trước
              </button>
              <button
                className={`daily-word__complete-button${isCurrentLearned ? ' daily-word__complete-button--done' : ''}`}
                type="button"
                disabled={isCurrentLearned || isSaving}
                onClick={() => void handleMarkLearned(currentVocab.id)}
              >
                {isSaving ? 'Đang lưu...' : isCurrentLearned ? '✓ Đã hoàn thành' : `Đã thuộc từ này  ·  +${xpPerVocabulary ?? 0} EXP`}
              </button>
              <button
                className="daily-word__nav"
                type="button"
                disabled={currentIndex === vocabs.length - 1 || isSaving}
                onClick={() => { setCurrentIndex((index) => index + 1); setShowMeaning(false); }}
              >
                Từ tiếp →
              </button>
            </div>
          </>
        )}
      </section>
    </section>
  );
};

function authorizationHeaders(): { Authorization: string } {
  const token = getAuthToken();
  if (!token) {
    throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
  }
  return { Authorization: `Bearer ${token}` };
}
