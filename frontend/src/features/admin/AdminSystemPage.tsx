import { useCallback, useEffect, useState } from 'react';
import {
  downloadDatabaseBackup,
  getAdminSystemApiError,
  getGamificationSettings,
  getSystemLogs,
  saveGamificationSettings,
  type SystemLog,
} from './adminSystemApi';
import './adminSystem.css';

export default function AdminSystemPage() {
  const [xpRules, setXpRules] = useState({
    vocabularyLearnedXp: '',
    lessonCompletedXp: '',
    correctAnswerXp: '',
    dailyStreakXp: '',
  });
  const [logs, setLogs] = useState<SystemLog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isBackingUp, setIsBackingUp] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadSystemData = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const [settings, recentLogs] = await Promise.all([
        getGamificationSettings(),
        getSystemLogs(),
      ]);
      setXpRules({
        vocabularyLearnedXp: String(settings.vocabularyLearnedXp),
        lessonCompletedXp: String(settings.lessonCompletedXp),
        correctAnswerXp: String(settings.correctAnswerXp),
        dailyStreakXp: String(settings.dailyStreakXp),
      });
      setLogs(recentLogs);
    } catch (loadError: unknown) {
      setError(getAdminSystemApiError(loadError));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadSystemData();
  }, [loadSystemData]);

  const saveRules = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');
    setNotice('');
    const settings = Object.fromEntries(
      Object.entries(xpRules).map(([key, value]) => [key, Number(value)]),
    ) as Record<keyof typeof xpRules, number>;
    if (Object.values(xpRules).some((value) => value.trim() === '') ||
        Object.values(settings).some((value) => !Number.isInteger(value) || value < 0 || value > 10000)) {
      setError('XP thưởng phải là số nguyên từ 0 đến 10000.');
      return;
    }

    setIsSaving(true);
    try {
      await saveGamificationSettings(settings);
      setNotice('Đã lưu quy tắc XP.');
      await loadSystemData();
    } catch (saveError: unknown) {
      setError(getAdminSystemApiError(saveError));
    } finally {
      setIsSaving(false);
    }
  };

  const createBackup = async () => {
    setError('');
    setNotice('');
    setIsBackingUp(true);
    try {
      await downloadDatabaseBackup();
      setNotice('Đã tạo và tải xuống bản sao lưu database.');
      await loadSystemData();
    } catch (backupError: unknown) {
      setError(getAdminSystemApiError(backupError));
    } finally {
      setIsBackingUp(false);
    }
  };

  return (
    <main className="admin-system">
      <header className="admin-system__header">
        <div>
          <p className="admin-system__eyebrow">ADMIN</p>
          <h1>Quản lý hệ thống</h1>
          <p>Cấu hình XP, sao lưu database và theo dõi nhật ký hệ thống.</p>
        </div>
        <button type="button" className="admin-system__button admin-system__button--secondary" onClick={() => { void loadSystemData(); }} disabled={isLoading}>
          Tải lại nhật ký
        </button>
      </header>

      {error && <p className="admin-system__message admin-system__message--error" role="alert">{error}</p>}
      {notice && <p className="admin-system__message admin-system__message--success" role="status">{notice}</p>}

      <section className="admin-system__card">
        <h2>Cấu hình Gamification</h2>
        <p>XP được áp dụng ở backend và lưu trong bảng SystemSettings.</p>
        <form className="admin-system__form" onSubmit={(event) => { void saveRules(event); }}>
          {([
            ['vocabularyLearnedXp', 'XP mỗi từ vựng'],
            ['lessonCompletedXp', 'XP hoàn thành bài học'],
            ['correctAnswerXp', 'XP mỗi câu trả lời đúng'],
            ['dailyStreakXp', 'XP điểm danh streak'],
          ] as const).map(([key, label]) => (
            <label key={key} htmlFor={key}>
              {label}
              <input
                id={key}
                type="number"
                min={0}
                max={10000}
                step={1}
                value={xpRules[key]}
                onChange={(event) => setXpRules((current) => ({ ...current, [key]: event.target.value }))}
                required
              />
            </label>
          ))}
          <button type="submit" className="admin-system__button" disabled={isSaving || isLoading}>
            {isSaving ? 'Đang lưu...' : 'Lưu quy tắc'}
          </button>
        </form>
      </section>

      <section className="admin-system__card admin-system__backup">
        <div>
          <h2>Sao lưu dữ liệu</h2>
          <p>Xuất cấu trúc và dữ liệu thành BACPAC, đóng gói ZIP và tải về máy.</p>
        </div>
        <button type="button" className="admin-system__button" onClick={() => { void createBackup(); }} disabled={isBackingUp}>
          {isBackingUp ? 'Đang tạo bản sao lưu...' : 'Tạo và tải backup'}
        </button>
      </section>

      <section className="admin-system__card">
        <div className="admin-system__logs-heading">
          <div>
            <h2>Nhật ký hệ thống</h2>
            <p>Lịch sử thay đổi cấu hình, sao lưu và lỗi máy chủ gần đây.</p>
          </div>
          <span>{isLoading ? 'Đang tải...' : `${logs.length} mục gần nhất`}</span>
        </div>
        {logs.length === 0 && !isLoading ? (
          <p className="admin-system__empty">Chưa có nhật ký hệ thống.</p>
        ) : (
          <div className="admin-system__logs-wrap">
            <table className="admin-system__logs">
              <thead>
                <tr><th>Thời gian</th><th>Mức độ</th><th>Danh mục</th><th>Hoạt động / lỗi</th></tr>
              </thead>
              <tbody>
                {logs.map((log) => (
                  <tr key={log.id}>
                    <td>{new Date(log.occurredAt).toLocaleString()}</td>
                    <td><span className={`admin-system__level admin-system__level--${log.level.toLowerCase()}`}>{log.level}</span></td>
                    <td>{log.category}</td>
                    <td>{log.message}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
