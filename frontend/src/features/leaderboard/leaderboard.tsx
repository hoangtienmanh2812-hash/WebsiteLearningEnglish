import { useEffect, useState } from 'react';
import axios from 'axios';
import { SkeletonBlock } from '../../components/common/Skeleton';
import './leaderboard.css';

interface LeaderboardUser {
  id: string;
  fullName: string;
  exp: number;
  streakCount: number;
}

const podiumPlaces = [
  { rank: 2, modifier: 'second', medal: '🥈' },
  { rank: 1, modifier: 'first', medal: '🥇' },
  { rank: 3, modifier: 'third', medal: '🥉' },
] as const;

export const Leaderboard = () => {
  const [users, setUsers] = useState<LeaderboardUser[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

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
      } finally {
        setIsLoading(false);
      }
    }

    void fetchLeaderboard();
  }, []);

  return (
    <section className="leaderboard" aria-labelledby="leaderboard-title">
      <header className="leaderboard__header">
        <div>
          <p className="leaderboard__eyebrow">CỘNG ĐỒNG HỌC TẬP</p>
          <h1 id="leaderboard-title">Bảng xếp hạng học viên</h1>
          <p className="leaderboard__intro">
            Vinh danh những thành viên đang nỗ lực chinh phục tiếng Anh.
          </p>
        </div>
        <span className="leaderboard__trophy" aria-hidden="true">🏆</span>
      </header>

      {error && <p className="leaderboard__message leaderboard__message--error" role="alert">{error}</p>}

      {isLoading ? (
        <div className="leaderboard__content leaderboard__content--skeleton" role="status" aria-label="Đang tải bảng xếp hạng">
          <section className="podium-card">
            <div className="leaderboard__section-heading">
              <div>
                <SkeletonBlock height="22px" width="160px" />
                <SkeletonBlock className="leaderboard-skeleton__caption" height="13px" width="210px" />
              </div>
              <SkeletonBlock className="skeleton-block--circle" height="26px" width="26px" />
            </div>
            <div className="podium">
              {[0, 1, 2].map((item) => (
                <div className={`leaderboard-skeleton__place leaderboard-skeleton__place--${item}`} key={item}>
                  <SkeletonBlock className="skeleton-block--circle" height={item === 1 ? '72px' : '58px'} width={item === 1 ? '72px' : '58px'} />
                  <SkeletonBlock height="13px" width="75%" />
                  <SkeletonBlock height="12px" width="48%" />
                  <SkeletonBlock className="leaderboard-skeleton__platform" height={item === 1 ? '118px' : item === 0 ? '78px' : '60px'} width="100%" />
                </div>
              ))}
            </div>
          </section>
          <section className="ranking-card">
            <div className="leaderboard__section-heading ranking-card__heading">
              <div>
                <SkeletonBlock height="22px" width="155px" />
                <SkeletonBlock className="leaderboard-skeleton__caption" height="13px" width="76px" />
              </div>
              <SkeletonBlock height="38px" width="38px" />
            </div>
            <div className="leaderboard-skeleton__rows">
              {[0, 1, 2, 3, 4].map((item) => (
                <div className="leaderboard-skeleton__row" key={item}>
                  <SkeletonBlock height="20px" width="24px" />
                  <SkeletonBlock height="30px" width="30px" />
                  <SkeletonBlock height="14px" width="70%" />
                  <SkeletonBlock height="14px" width="40px" />
                </div>
              ))}
            </div>
          </section>
        </div>
      ) : !error && users.length === 0 ? (
        <p className="leaderboard__message">Chưa có dữ liệu xếp hạng.</p>
      ) : !error ? (
        <div className="leaderboard__content">
          <section className="podium-card" aria-labelledby="podium-title">
            <div className="leaderboard__section-heading">
              <div>
                <h2 id="podium-title">Top học viên</h2>
                <p>Tiếp tục giữ vững phong độ nhé!</p>
              </div>
              <span className="leaderboard__sparkle" aria-hidden="true">✦</span>
            </div>

            <div className="podium">
              {podiumPlaces.map(({ rank, modifier, medal }) => {
                const user = users[rank - 1];
                if (!user) return <div className="podium__place" key={rank} aria-hidden="true" />;

                const name = user.fullName?.trim() || 'Học viên';

                return (
                  <article className={`podium__place podium__place--${modifier}`} key={user.id}>
                    <span className="podium__medal" aria-label={`Hạng ${rank}`}>{medal}</span>
                    <div className="podium__avatar" aria-hidden="true">
                      {name.charAt(0).toUpperCase()}
                    </div>
                    <h3 className="podium__name" title={name}>{name}</h3>
                    <p className="podium__score">{user.exp.toLocaleString()} <span>EXP</span></p>
                    <div className="podium__platform">
                      <span>{rank}</span>
                    </div>
                  </article>
                );
              })}
            </div>
          </section>

          <section className="ranking-card" aria-labelledby="ranking-title">
            <div className="leaderboard__section-heading ranking-card__heading">
              <div>
                <h2 id="ranking-title">Bảng xếp hạng</h2>
                <p>{users.length} học viên</p>
              </div>
              <span className="ranking-card__icon" aria-hidden="true">📋</span>
            </div>

            <div className="ranking-card__table-wrap">
              <table className="ranking-table">
                <thead>
                  <tr>
                    <th scope="col">Hạng</th>
                    <th scope="col">Học viên</th>
                    <th scope="col">Streak</th>
                    <th scope="col">EXP</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user, index) => (
                    <tr key={user.id} className={index < 3 ? 'ranking-table__top-row' : undefined}>
                      <td>
                        <span className={`ranking-table__rank ${index < 3 ? `ranking-table__rank--${index + 1}` : ''}`}>
                          {index < 3 ? ['🥇', '🥈', '🥉'][index] : index + 1}
                        </span>
                      </td>
                      <td>
                        <span className="ranking-table__learner">
                          <span className="ranking-table__avatar" aria-hidden="true">
                            {(user.fullName?.trim() || 'H').charAt(0).toUpperCase()}
                          </span>
                          <span className="ranking-table__name">{user.fullName?.trim() || 'Học viên'}</span>
                        </span>
                      </td>
                      <td><span className="ranking-table__streak">🔥 {user.streakCount}</span></td>
                      <td><strong className="ranking-table__exp">{user.exp.toLocaleString()}</strong></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      ) : null}
    </section>
  );
};
