import axios from 'axios';
import { getAuthToken } from '../auth/authStorage';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5219';
const API_URL = `${API_BASE_URL}/api/admin/users`;

export type AdminUser = {
  id: string;
  fullName: string;
  email: string;
  isAdmin: boolean;
  exp: number;
  streakCount: number;
  completedLessons: number;
  totalLessons: number;
  createdAt: string;
};

export type AdminUserProgress = {
  lessonId: number;
  title: string;
  isCompleted: boolean;
};

export type AdminUserDetails = Omit<AdminUser, 'createdAt'> & {
  createdAt: string;
  lastLoginDate: string | null;
  progress: AdminUserProgress[];
};

export type UpdateAdminUserRequest = {
  fullName: string;
  email: string;
  isAdmin: boolean;
  password?: string;
};

function authorizationHeaders(): { Authorization: string } {
  const token = getAuthToken();
  if (!token) {
    throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
  }

  return { Authorization: `Bearer ${token}` };
}

export async function getAdminUsers(search: string, role: 'all' | 'admin' | 'user'): Promise<AdminUser[]> {
  const response = await axios.get<AdminUser[]>(API_URL, {
    headers: authorizationHeaders(),
    params: { search: search.trim() || undefined, role },
  });
  return response.data;
}

export async function getAdminUser(id: string): Promise<AdminUserDetails> {
  const response = await axios.get<AdminUserDetails>(`${API_URL}/${encodeURIComponent(id)}`, {
    headers: authorizationHeaders(),
  });
  return response.data;
}

export async function updateAdminUser(
  id: string,
  request: UpdateAdminUserRequest,
): Promise<void> {
  await axios.put(`${API_URL}/${encodeURIComponent(id)}`, request, {
    headers: authorizationHeaders(),
  });
}

export function getAdminUsersApiError(error: unknown): string {
  if (axios.isAxiosError<{ message?: string }>(error)) {
    if (error.response?.status === 401) {
      return error.response.data?.message
        ?? 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.';
    }
    if (error.response?.status === 403) {
      return error.response.data?.message ?? 'Bạn không có quyền quản lý người dùng.';
    }
    if (!error.response) {
      return 'Không thể kết nối đến máy chủ. Vui lòng thử lại.';
    }

    return error.response.data?.message ?? 'Yêu cầu chưa thể xử lý. Vui lòng thử lại.';
  }

  return error instanceof Error
    ? error.message
    : 'Đã có lỗi không mong muốn xảy ra. Vui lòng thử lại.';
}
