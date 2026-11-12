import axios from 'axios';
import { getAuthToken } from '../auth/authStorage';

const API_URL = `${import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5219'}/api/account`;

export type AccountProfile = {
  id: string;
  fullName: string;
  email: string;
  createdAt: string;
  exp: number;
  streakCount: number;
  completedLessons: number;
  totalLessons: number;
};

function authorizationHeaders(): { Authorization: string } {
  const token = getAuthToken();
  if (!token) throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
  return { Authorization: `Bearer ${token}` };
}

export async function getAccountProfile(): Promise<AccountProfile> {
  const response = await axios.get<AccountProfile>(`${API_URL}/me`, {
    headers: authorizationHeaders(),
  });
  return response.data;
}

export async function updateAccountProfile(fullName: string): Promise<void> {
  await axios.put(`${API_URL}/profile`, { fullName }, { headers: authorizationHeaders() });
}

export async function changeAccountPassword(
  currentPassword: string,
  newPassword: string,
): Promise<void> {
  await axios.put(
    `${API_URL}/password`,
    { currentPassword, newPassword },
    { headers: authorizationHeaders() },
  );
}

export function getAccountApiError(error: unknown): string {
  if (axios.isAxiosError<{ message?: string }>(error)) {
    if (error.response?.status === 401) {
      return error.response.data?.message ?? 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
    }
    if (!error.response) return 'Không thể kết nối đến máy chủ. Vui lòng thử lại.';
    return error.response.data?.message ?? 'Yêu cầu chưa thể xử lý. Vui lòng thử lại.';
  }

  return error instanceof Error ? error.message : 'Đã có lỗi không mong muốn xảy ra.';
}
