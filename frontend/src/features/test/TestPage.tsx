import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import './TestPage.css';

interface Question {
  question: string;
  [key: string]: string;
}

interface TestData {
  name?: string;
  type?: string;
  questions: Question[];
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5219';

function formatTime(seconds: number) {
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
}

export default function TestPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [testData, setTestData] = useState<TestData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentQuestion, setCurrentQuestion] = useState(0);
  const [userAnswers, setUserAnswers] = useState<Record<number, string>>({});
  const [timeLeft, setTimeLeft] = useState(40 * 60);
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    const controller = new AbortController();

    async function fetchTest() {
      try {
        setLoading(true);
        setError(null);
        setSubmitted(false);
        setUserAnswers({});
        setCurrentQuestion(0);
        setTimeLeft(40 * 60);

        const response = await fetch(`${API_BASE_URL}/api/Quiz/${id ?? '1'}?lessonType=test`, {
          signal: controller.signal,
        });
        if (!response.ok) {
          throw new Error('Không thể tải dữ liệu bài kiểm tra từ máy chủ.');
        }

        const data: TestData = await response.json();
        if (!Array.isArray(data.questions) || data.questions.length === 0) {
          throw new Error('Bài kiểm tra này chưa có câu hỏi.');
        }
        setTestData(data);
      } catch (caughtError) {
        if (caughtError instanceof Error && caughtError.name !== 'AbortError') {
          setError(caughtError.message);
        }
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }

    fetchTest();
    return () => controller.abort();
  }, [id]);

  useEffect(() => {
    if (loading || error || submitted || timeLeft === 0) return;

    const timer = window.setInterval(() => {
      setTimeLeft((previous) => Math.max(previous - 1, 0));
    }, 1000);
    return () => window.clearInterval(timer);
  }, [loading, error, submitted, timeLeft]);

  const questions = testData?.questions ?? [];
  const answeredCount = Object.keys(userAnswers).length;
  const score = questions.reduce((total, question, index) => (
    userAnswers[index] === question['correct-ans'] ? total + 1 : total
  ), 0);

  const selectAnswer = (questionIndex: number, answer: string) => {
    if (submitted || timeLeft === 0) return;
    setUserAnswers((previous) => ({ ...previous, [questionIndex]: answer }));
  };

  if (loading) {
    return <main className="test-page-container test-state"><p>Đang tải bài kiểm tra...</p></main>;
  }

  if (error || !testData) {
    return (
      <main className="test-page-container test-state" role="alert">
        <p>{error ?? 'Không thể tải bài kiểm tra.'}</p>
        <button type="button" className="test-button" onClick={() => navigate('/dashboard')}>
          Quay lại lộ trình
        </button>
      </main>
    );
  }

  if (submitted || timeLeft === 0) {
    return (
      <main className="test-page-container test-state">
        <h1>{timeLeft === 0 ? 'Đã hết giờ' : 'Đã nộp bài'}</h1>
        <p>Bạn trả lời đúng <strong>{score}/{questions.length}</strong> câu.</p>
        <button type="button" className="test-button" onClick={() => navigate('/dashboard')}>
          Quay lại lộ trình
        </button>
      </main>
    );
  }

  const activeQuestion = questions[currentQuestion];
  const optionKeys = ['1', '2', '3', '4'].filter((key) => activeQuestion[key]);

  return (
    <main className="test-page-container">
      <header className="test-header">
        <div>
          <p className="test-eyebrow">BÀI HỌC & KIỂM TRA</p>
          <h1>{testData.name || `Bài ${id ?? 1}`}</h1>
        </div>
        <div className="test-timer" aria-label={`Thời gian còn lại: ${formatTime(timeLeft)}`}>
          Còn lại: <strong>{formatTime(timeLeft)}</strong>
        </div>
      </header>

      <div className="test-body">
        <section className="test-main-content" aria-live="polite">
          <p className="question-counter">Câu {currentQuestion + 1}/{questions.length}</p>
          <h2>{activeQuestion.question}</h2>
          <div className="answer-list">
            {optionKeys.map((key) => (
              <button
                key={key}
                type="button"
                className={`answer-option ${userAnswers[currentQuestion] === key ? 'selected' : ''}`}
                onClick={() => selectAnswer(currentQuestion, key)}
              >
                <span>{key}</span>{activeQuestion[key]}
              </button>
            ))}
          </div>
          <div className="test-actions">
            <button type="button" className="test-button secondary" disabled={currentQuestion === 0} onClick={() => setCurrentQuestion((index) => index - 1)}>
              Câu trước
            </button>
            <button type="button" className="test-button" disabled={currentQuestion === questions.length - 1} onClick={() => setCurrentQuestion((index) => index + 1)}>
              Câu tiếp
            </button>
          </div>
        </section>

        <aside className="test-sidebar" aria-label="Điều hướng câu hỏi">
          <div>
            <h2>Tiến độ</h2>
            <p>Đã trả lời {answeredCount}/{questions.length} câu</p>
          </div>
          <div className="nav-grid">
            {questions.map((_, index) => (
              <button
                key={index}
                type="button"
                className={`nav-cell ${index === currentQuestion ? 'active' : ''} ${userAnswers[index] ? 'answered' : ''}`}
                aria-label={`Câu ${index + 1}${userAnswers[index] ? ', đã trả lời' : ''}`}
                onClick={() => setCurrentQuestion(index)}
              >
                {index + 1}
              </button>
            ))}
          </div>
          <button type="button" className="test-button submit" onClick={() => setSubmitted(true)}>
            Nộp bài
          </button>
        </aside>
      </div>
    </main>
  );
}
