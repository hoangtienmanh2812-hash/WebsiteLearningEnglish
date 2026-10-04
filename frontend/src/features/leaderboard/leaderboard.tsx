import { useEffect, useState } from 'react';
import axios from 'axios';

interface LeaderboardUser {
  id: string;
  fullName: string;
  exp: number;
  streakCount: number;
}

export const Leaderboard = () => {
  const [users, setUsers] = useState<LeaderboardUser[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchLeaderboard() {
      try {
        const response = await axios.get<LeaderboardUser[]>(
          'http://localhost:5219/api/Gamification/leaderboard',
        );
        if (!Array.isArray(response.data)) {
          throw new Error('Dữ liệu bảng xếp hạng không đúng định dạng.');
        }

        setUsers(response.data);
      } catch (err) {
        console.error('Lỗi lấy danh sách xếp hạng:', err);
        setError('Không thể tải bảng xếp hạng. Vui lòng thử lại sau.');
      }
    }

    void fetchLeaderboard();
  }, []);

  return (
    <div style={{ padding: '20px', maxWidth: '600px', margin: '0 auto' }}>
      <h2>🏆 Bảng Xếp Hạng Học Viên</h2>
      {error && <p role="alert" style={{ color: '#b91c1c' }}>{error}</p>}
      <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '15px' }}>
        <thead>
          <tr style={{ background: '#f4f4f4', textAlign: 'left' }}>
            <th style={{ padding: '12px' }}>Hạng</th>
            <th>Tên học viên</th>
            <th>Chuỗi (Streak)</th>
            <th>Điểm (EXP)</th>
          </tr>
        </thead>
        <tbody>
          {users.map((u, index) => (
            <tr key={u.id} style={{ borderBottom: '1px solid #ddd' }}>
              <td style={{ padding: '12px' }}>
                {index === 0 ? '🥇 1' : index === 1 ? '🥈 2' : index === 2 ? '🥉 3' : index + 1}
              </td>
              <td>{u.fullName || 'Học viên'}</td>
              <td>🔥 {u.streakCount} ngày</td>
              <td>⭐ {u.exp} EXP</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};