import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './TestsPage.css';

interface TestSummary {
  id: number;
  title: string;
  lessonType: 'test';
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5219';

export default function TestsPage() {
  const navigate = useNavigate();
  const [tests, setTests] = useState<TestSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function fetchTests() {
      try {
        const response = await fetch(`${API_BASE_URL}/api/Lessons?type=test`, {
          signal: controller.signal,
        });
        if (!response.ok) throw new Error('Không thể tải danh sách bài kiểm tra.');
        const data: TestSummary[] = await response.json();
        setTests(Array.isArray(data) ? data : []);
      } catch (caughtError) {
        if (caughtError instanceof Error && caughtError.name !== 'AbortError') {
          setError(caughtError.message);
        }
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }

    fetchTests();
    return () => controller.abort();
  }, []);

  return (
    <section className="tests-page" aria-labelledby="tests-title">
      <p className="tests-page__eyebrow">LUYỆN TẬP</p>
      <h1 id="tests-title">Bài kiểm tra</h1>
      <p className="tests-page__description">Chọn một bài kiểm tra để đánh giá kiến thức của bạn.</p>

      {loading && <p>Đang tải danh sách bài kiểm tra...</p>}
      {error && <p className="tests-page__error" role="alert">{error}</p>}
      {!loading && !error && tests.length === 0 && <p>Chưa có bài kiểm tra nào.</p>}

      <div className="tests-grid">
        {tests.map((test, index) => (
          <article className="test-card" key={test.id}>
            <span className="test-card__number">{String(index + 1).padStart(2, '0')}</span>
            <h2>{test.title}</h2>
            <p>Bài kiểm tra</p>
            <button type="button" onClick={() => navigate(`/test/${test.id}`)}>
              Làm bài
            </button>
          </article>
        ))}
      </div>
    </section>
  );
}
