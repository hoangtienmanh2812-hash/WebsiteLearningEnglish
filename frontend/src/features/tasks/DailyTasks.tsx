import { useEffect, useState } from 'react';
import axios from 'axios';
import { getAuthUser } from '../auth/authStorage';

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
}

export const DailyTasks = () => {
  const [vocabs, setVocabs] = useState<Vocab[]>([]);
  const [learnedIds, setLearnedIds] = useState<number[]>([]);
  const [title, setTitle] = useState<string>('');
  const [currentIndex, setCurrentIndex] = useState(0);
  const [showMeaning, setShowMeaning] = useState(false);
  const [dayNumber, setDayNumber] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
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
        );
        if (!Array.isArray(response.data.vocabularies)) {
          throw new Error('Dữ liệu gói từ vựng không đúng định dạng.');
        }

        setTitle(response.data.title);
        setVocabs(response.data.vocabularies);
        setLearnedIds(response.data.learnedVocabIds ?? []);
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

  const handleMarkLearned = (vocabId: number) => {
    if (!userId) {
      setError('Không tìm thấy thông tin tài khoản. Vui lòng đăng nhập lại.');
      return;
    }

    axios.post('http://localhost:5219/api/Gamification/mark-vocab-learned', { userId, dayNumber, vocabId })
      .then(() => {
        if (!learnedIds.includes(vocabId)) {
          setLearnedIds(prev => [...prev, vocabId]);
        }
        setShowMeaning(false);
        if (currentIndex < vocabs.length - 1) {
          setCurrentIndex(currentIndex + 1);
        }
      })
      .catch(err => {
        console.error('Lỗi đánh dấu từ vựng:', err);
        setError('Không thể lưu tiến độ từ vựng. Vui lòng thử lại.');
      });
  };

  if (loading) {
    return <div style={{ padding: '30px', textAlign: 'center' }}>Đang tải gói từ vựng Ngày {dayNumber}...</div>;
  }

  if (error) {
    return <div role="alert" style={{ padding: '30px', textAlign: 'center', color: '#b91c1c' }}>{error}</div>;
  }

  if (vocabs.length === 0) {
    return <div style={{ padding: '30px', textAlign: 'center' }}>Chưa có từ vựng cho Ngày {dayNumber}.</div>;
  }

  const currentVocab = vocabs[currentIndex];
  if (!currentVocab) {
    return <div role="alert" style={{ padding: '30px', textAlign: 'center', color: '#b91c1c' }}>Không tìm thấy từ vựng hiện tại.</div>;
  }

  const isCurrentLearned = learnedIds.includes(currentVocab.id);

  return (
    <div style={{ maxWidth: '500px', margin: '30px auto', padding: '20px', textAlign: 'center' }}>
      <h2>🎯 {title}</h2>

      <div style={{ marginBottom: '15px' }}>
        <label><strong>Nhiệm vụ theo ngày: </strong></label>
        <select value={dayNumber} onChange={(e) => setDayNumber(Number(e.target.value))}>
          <option value={1}>Ngày 1</option>
          <option value={2}>Ngày 2</option>
        </select>
      </div>

      <p style={{ color: '#666' }}>Tiến độ: <strong>{learnedIds.length} / {vocabs.length} từ đã thuộc</strong></p>

      {/* Thẻ Flashcard */}
      <div 
        onClick={() => setShowMeaning(!showMeaning)}
        style={{
          border: '2px solid #3498db',
          borderRadius: '12px',
          padding: '40px 20px',
          marginTop: '15px',
          cursor: 'pointer',
          background: showMeaning ? '#f0f7ff' : '#ffffff',
          boxShadow: '0 4px 8px rgba(0,0,0,0.1)',
          transition: 'all 0.3s ease'
        }}
      >
        <h1 style={{ fontSize: '36px', color: '#2C3E50', margin: '0' }}>{currentVocab.word}</h1>
        <p style={{ color: '#888', fontStyle: 'italic' }}>{currentVocab.phonetic}</p>

        {showMeaning ? (
          <div style={{ marginTop: '20px', borderTop: '1px solid #ddd', paddingTop: '15px' }}>
            <h2 style={{ color: '#27AE60' }}>{currentVocab.meaning}</h2>
            <p style={{ fontSize: '14px', color: '#555' }}>💡 <em>"{currentVocab.exampleSentence}"</em></p>
          </div>
        ) : (
          <p style={{ color: '#3498DB', marginTop: '20px', fontSize: '14px' }}>👉 Bấm vào thẻ để xem nghĩa</p>
        )}
      </div>

      {/* Các nút điều khiển */}
      <div style={{ marginTop: '25px', display: 'flex', justifyContent: 'space-between', gap: '10px' }}>
        <button 
          disabled={currentIndex === 0} 
          onClick={() => { setCurrentIndex(currentIndex - 1); setShowMeaning(false); }}
          style={{ padding: '10px 15px', borderRadius: '6px', cursor: 'pointer' }}
        >
          ⬅️ Từ trước
        </button>

        <button 
          onClick={() => handleMarkLearned(currentVocab.id)}
          style={{ 
            padding: '10px 15px', 
            borderRadius: '6px', 
            background: isCurrentLearned ? '#95A5A6' : '#2ECC71', 
            color: 'white', 
            border: 'none', 
            fontWeight: 'bold',
            cursor: 'pointer' 
          }}
        >
          {isCurrentLearned ? '✅ Đã Thuộc' : '✔️ Thuộc Từ Này (+10 EXP)'}
        </button>

        <button 
          disabled={currentIndex === vocabs.length - 1} 
          onClick={() => { setCurrentIndex(currentIndex + 1); setShowMeaning(false); }}
          style={{ padding: '10px 15px', borderRadius: '6px', cursor: 'pointer' }}
        >
          Từ tiếp ➡️
        </button>
      </div>
    </div>
  );
};