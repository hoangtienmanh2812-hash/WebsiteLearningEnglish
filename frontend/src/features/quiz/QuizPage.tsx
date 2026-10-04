import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { getAuthUser } from '../auth/authStorage';
import './quiz.css';

interface Question {
  id: number;
  question: string;
  options: string[];
  correctAnswer: string;
}

type Attachment =
  | { kind: 'text'; content: string }
  | { kind: 'audio' | 'document'; url: string };

export default function QuizPage() {
  const navigate = useNavigate();
  const { id } = useParams(); // Lấy ID bài học từ URL (ví dụ: /quiz/1 thì id = '1')
  const user = getAuthUser();
  
  const [questions, setQuestions] = useState<Question[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentIdx, setCurrentIdx] = useState(0);
  const [selectedOption, setSelectedOption] = useState<string | null>(null);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [score, setScore] = useState(0);
  const [showResult, setShowResult] = useState(false);
  const [attachmentPath, setAttachmentPath] = useState<string | null>(null);
  const [attachment, setAttachment] = useState<Attachment | null>(null);
  const [attachmentLoading, setAttachmentLoading] = useState(false);
  const [attachmentError, setAttachmentError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchQuestions() {
      try {
        setLoading(true);
        setAttachmentPath(null);
        setAttachment(null);
        setAttachmentLoading(false);
        setAttachmentError(null);
        // Gọi đúng cổng HTTP http://localhost:5219
        const response = await fetch(`http://localhost:5219/api/quiz/${id}`);
        if (!response.ok) {
          throw new Error('Không thể tải dữ liệu câu hỏi từ server');
        }
        const data = await response.json() as {
          questions?: Array<{
            question: string;
            "1": string;
            "2": string;
            "3": string;
            "4": string;
            "correct-ans": string;
          }>;
          file_dinh_kem?: string;
        };
        if (!Array.isArray(data.questions)) {
          throw new Error('Dữ liệu câu hỏi không đúng định dạng');
        }

        setQuestions(data.questions.map((item, index) => {
          const options = [item["1"], item["2"], item["3"], item["4"]];
          return {
            id: index + 1,
            question: item.question,
            options,
            correctAnswer: options[Number(item["correct-ans"]) - 1] ?? "",
          };
        }));
        setAttachmentPath(
          typeof data.file_dinh_kem === 'string' && data.file_dinh_kem.trim()
            ? `http://localhost:5219/api/quiz/${id}/attachment`
            : null,
        );
      } catch (error) {
        console.error('Lỗi khi gọi API:', error);
        setAttachmentPath(null);
      } finally {
        setLoading(false);
      }
    }

    fetchQuestions();
  }, [id]);

  useEffect(() => {
    if (!attachmentPath) {
      return;
    }

    const path = attachmentPath;
    const controller = new AbortController();
    let objectUrl: string | null = null;

    async function fetchAttachment() {
      try {
        setAttachmentLoading(true);
        setAttachment(null);
        setAttachmentError(null);

        const response = await fetch(path, { signal: controller.signal });
        if (!response.ok) {
          throw new Error('Không thể tải file đọc/nghe đính kèm.');
        }

        const contentType = response.headers.get('content-type')?.split(';')[0].trim().toLowerCase() ?? '';
        if (contentType.startsWith('audio/')) {
          objectUrl = URL.createObjectURL(await response.blob());
          setAttachment({ kind: 'audio', url: objectUrl });
        } else if (contentType === 'application/pdf') {
          objectUrl = URL.createObjectURL(await response.blob());
          setAttachment({ kind: 'document', url: objectUrl });
        } else if (contentType.startsWith('text/')) {
          setAttachment({ kind: 'text', content: await response.text() });
        } else {
          throw new Error(`Định dạng file chưa được hỗ trợ để hiển thị (${contentType || 'không xác định'}).`);
        }
      } catch (error) {
        if (error instanceof Error && error.name !== 'AbortError') {
          setAttachmentError(error.message);
          console.error('Lỗi khi tải file đính kèm:', error);
        }
      } finally {
        if (!controller.signal.aborted) {
          setAttachmentLoading(false);
        }
      }
    }

    fetchAttachment();

    return () => {
      controller.abort();
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [attachmentPath]);

  // Hàm gọi API báo hoàn thành bài học khi user bấm kết thúc
  const markLessonAsCompleted = async () => {
    if (!user?.id || !id) return;

    try {
      await fetch('http://localhost:5219/api/progress/complete', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          userId: user.id,
          lessonId: Number(id), // Gửi đúng số thứ tự bài học hiện tại lên để mở khóa bài tiếp theo
        }),
      });
    } catch (err) {
      console.error('Lỗi cập nhật tiến độ:', err);
    }
  };

  if (loading) {
    return (
      <div className="quiz-layout" style={{ justifyContent: 'center', alignItems: 'center' }}>
        <h2>Đang tải câu hỏi từ database...</h2>
      </div>
    );
  }

  if (questions.length === 0) {
    return (
      <div className="quiz-layout" style={{ justifyContent: 'center', alignItems: 'center', textAlign: 'center' }}>
        <h2>Chưa có câu hỏi nào cho bài học này!</h2>
        <button className="action-btn" style={{ marginTop: '20px' }} onClick={() => navigate('/dashboard')}>
          Quay lại lộ trình
        </button>
      </div>
    );
  }

  const currentQuestion = questions[currentIdx];
  const progressPercent = (currentIdx / questions.length) * 100;

  const handleCheck = () => {
    setIsSubmitted(true);
    if (selectedOption === currentQuestion.correctAnswer) {
      setScore(score + 1);
    }
  };

  const handleNext = async () => {
    if (currentIdx < questions.length - 1) {
      setCurrentIdx(currentIdx + 1);
      setSelectedOption(null);
      setIsSubmitted(false);
    } else {
      // Đã đến câu cuối cùng, tiến hành gọi API mở khóa bài tiếp theo
      await markLessonAsCompleted();
      setShowResult(true);
    }
  };

  if (showResult) {
    return (
      <div className="quiz-layout">
        <main className="quiz-result">
          <div className="result-icon">🏆</div>
          <h2 className="result-title">Hoàn thành bài học!</h2>
          <p className="result-stats">
            Bạn đã trả lời đúng <strong>{score}/{questions.length}</strong> câu hỏi.
          </p>
          <div className="result-actions">
            <button className="action-btn" onClick={() => navigate('/dashboard')}>
              Quay lại lộ trình
            </button>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="quiz-layout">
      <header className="quiz-header">
        <button className="close-btn" onClick={() => navigate('/dashboard')} title="Thoát">✕</button>
        <div className="progress-bar">
          <div className="progress-fill" style={{ width: `${progressPercent}%` }}></div>
        </div>
        <div className="heart-count">❤️ 5</div>
      </header>

      <main className="quiz-content">
        {(attachmentPath || attachmentLoading || attachmentError) && (
          <section className="quiz-attachment" aria-label="Tài liệu bài học">
            <h3 className="attachment-title">
              {attachment?.kind === 'audio' ? 'Bài nghe' : 'Bài đọc'}
            </h3>
            {attachmentLoading && <p className="attachment-status">Đang tải tài liệu...</p>}
            {attachmentError && <p className="attachment-error">{attachmentError}</p>}
            {attachment?.kind === 'text' && (
              <div className="reading-passage">{attachment.content}</div>
            )}
            {attachment?.kind === 'audio' && (
              <audio className="listening-audio" controls preload="metadata" src={attachment.url}>
                Trình duyệt của bạn không hỗ trợ phát âm thanh.
              </audio>
            )}
            {attachment?.kind === 'document' && (
              <iframe className="reading-document" title="Tài liệu bài đọc" src={attachment.url} />
            )}
          </section>
        )}
        <h2 className="question-title">{currentQuestion.question}</h2>
        
        <div className="options-grid">
          {currentQuestion.options.map((option, index) => {
            const isSelected = selectedOption === option;
            const isCorrect = isSubmitted && option === currentQuestion.correctAnswer;
            const isWrong = isSubmitted && isSelected && option !== currentQuestion.correctAnswer;

            let optionClass = "option-btn";
            if (isSelected) optionClass += " selected";
            if (isCorrect) optionClass += " correct";
            if (isWrong) optionClass += " wrong";

            return (
              <button 
                key={index} 
                className={optionClass}
                onClick={() => !isSubmitted && setSelectedOption(option)}
                disabled={isSubmitted}
              >
                {option}
              </button>
            );
          })}
        </div>
      </main>

      <footer className={`quiz-footer ${isSubmitted ? (selectedOption === currentQuestion.correctAnswer ? 'footer-correct' : 'footer-wrong') : ''}`}>
        <div className="footer-content">
          {isSubmitted ? (
            <div className="result-message">
              {selectedOption === currentQuestion.correctAnswer 
                ? '🎉 Tuyệt vời! Bạn đã chọn đúng.' 
                : `❌ Sai rồi. Đáp án đúng là: ${currentQuestion.correctAnswer}`}
            </div>
          ) : (
            <div></div> 
          )}
          
          <button 
            className="action-btn" 
            onClick={isSubmitted ? handleNext : handleCheck}
            disabled={!selectedOption && !isSubmitted}
          >
            {isSubmitted ? 'Tiếp tục' : 'Kiểm tra'}
          </button>
        </div>
      </footer>
    </div>
  );
}