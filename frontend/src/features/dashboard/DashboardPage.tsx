import { useNavigate } from 'react-router-dom';
import { getAuthUser } from '../auth/authStorage';
import './dashboard.css';

export default function DashboardPage() {
  const user = getAuthUser();
  const navigate = useNavigate(); // Khởi tạo hàm chuyển trang
  const firstName = user?.fullName.trim().split(/\s+/).at(-1) || 'bạn';
  const today = new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(new Date());

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
          {/* Đường uốn lượn SVG ngang */}
          <svg className="path-svg" viewBox="0 0 2000 150" preserveAspectRatio="none">
            <path d="M 50 75 Q 150 150 250 75 T 450 75 T 650 75 T 850 75 T 1050 75 T 1250 75 T 1450 75 T 1650 75 T 1850 75" 
                  fill="none" stroke="#e5e7eb" strokeWidth="8" strokeLinecap="round"/>
          </svg>
          
          {/* Danh sách 10 nút bài học có gắn sự kiện click sang Quiz */}
          <div className="nodes-row">
            <div className="node up active" onClick={() => navigate('/quiz/1')}><div className="circle">📖</div><span>Bài 1</span></div>
            <div className="node down" onClick={() => navigate('/quiz/2')}><div className="circle">🎧</div><span>Bài 2</span></div>
            <div className="node up" onClick={() => navigate('/quiz/3')}><div className="circle">🗣️</div><span>Bài 3</span></div>
            <div className="node down" onClick={() => navigate('/quiz/4')}><div className="circle">📝</div><span>Bài 4</span></div>
            <div className="node up" onClick={() => navigate('/quiz/5')}><div className="circle">📚</div><span>Bài 5</span></div>
            <div className="node down" onClick={() => navigate('/quiz/6')}><div className="circle">🧩</div><span>Bài 6</span></div>
            <div className="node up" onClick={() => navigate('/quiz/7')}><div className="circle">🎯</div><span>Bài 7</span></div>
            <div className="node down" onClick={() => navigate('/quiz/8')}><div className="circle">🏆</div><span>Bài 8</span></div>
            <div className="node up" onClick={() => navigate('/quiz/9')}><div className="circle">🚀</div><span>Bài 9</span></div>
            <div className="node down chest" onClick={() => navigate('/quiz/10')}><div className="circle">🎁</div><span>Thưởng</span></div>
          </div>
        </div>
      </div>
      
    </section>
  );
}