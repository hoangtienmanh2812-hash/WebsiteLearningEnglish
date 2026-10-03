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

  useEffect(() => {
    async function fetchQuestions() {
      try {
        setLoading(true);
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
      } catch (error) {
        console.error('Lỗi khi gọi API:', error);
      } finally {
        setLoading(false);
      }
    }

    fetchQuestions();
  }, [id]);

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