import axios from 'axios';
import { getAuthToken } from '../auth/authStorage';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5219';
const API_URL = `${API_BASE_URL}/api/admin/system`;

export type GamificationSettings = {
  vocabularyLearnedXp: number;
  lessonCompletedXp: number;
  correctAnswerXp: number;
  dailyStreakXp: number;
};

export type SystemLog = {
  id: number;
  occurredAt: string;
  level: string;
  category: string;
  message: string;
};

function authorizationHeaders(): { Authorization: string } {
  const token = getAuthToken();
  if (!token) {
    throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
  }
  return { Authorization: `Bearer ${token}` };
}

export async function getGamificationSettings(): Promise<GamificationSettings> {
  const response = await axios.get<GamificationSettings>(`${API_URL}/gamification`, {
    headers: authorizationHeaders(),
  });
  return response.data;
}

export async function saveGamificationSettings(
  settings: GamificationSettings,
): Promise<void> {
  await axios.put(`${API_URL}/gamification`, settings, {
    headers: authorizationHeaders(),
  });
}

export async function getSystemLogs(): Promise<SystemLog[]> {
  const response = await axios.get<SystemLog[]>(`${API_URL}/logs`, {
    headers: authorizationHeaders(),
    params: { take: 100 },
  });
  return response.data;
}

export async function downloadDatabaseBackup(): Promise<void> {
  let response;
  try {
    response = await axios.post<Blob>(`${API_URL}/backup`, null, {
      headers: authorizationHeaders(),
      responseType: 'blob',
    });
  } catch (error: unknown) {
    if (axios.isAxiosError<Blob>(error) && error.response?.data instanceof Blob) {
      const responseText = await error.response.data.text();
      let responseBody: { message?: unknown; title?: unknown } | null = null;
      try {
        responseBody = JSON.parse(responseText) as { message?: unknown; title?: unknown };
      } catch {
        responseBody = null;
      }
      if (typeof responseBody?.message === 'string') {
        throw new Error(responseBody.message);
      }
      if (typeof responseBody?.title === 'string') {
        throw new Error(responseBody.title);
      }
    }
    throw error;
  }

  const url = URL.createObjectURL(response.data);
  const link = document.createElement('a');
  link.href = url;
  link.download = `database-backup-${new Date().toISOString().replaceAll(':', '-')}.zip`;
  document.body.append(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function getAdminSystemApiError(error: unknown): string {
  if (axios.isAxiosError<{ message?: string }>(error)) {
    if (error.response?.status === 401) {
      return 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.';
    }
    if (error.response?.status === 403) {
      return 'Chỉ Admin mới được phép sử dụng chức năng này.';
    }
    if (!error.response) {
      return 'Không thể kết nối đến máy chủ. Vui lòng thử lại.';
    }
    return error.response.data?.message ?? 'Yêu cầu chưa thể xử lý. Vui lòng thử lại.';
  }

  return error instanceof Error ? error.message : 'Đã có lỗi không mong muốn xảy ra.';
}
