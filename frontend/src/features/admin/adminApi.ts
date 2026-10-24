import axios from 'axios';
import { getAuthToken } from '../auth/authStorage';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5219';
const API_URL = `${API_BASE_URL}/api/admin/lessons`;

export type AdminFolder = {
  id: string;
  name: string;
  path: string;
};

export type AdminLesson = {
  id: number;
  title: string;
  lessonType: 'lesson' | 'test';
  folderId: string;
  folderPath: string;
  modifiedTime: string | null;
};

export type AdminLessonContent = {
  id: number;
  title: string;
  lessonType: 'lesson' | 'test';
  folderId: string;
  folderPath: string;
  content: string;
};

export type SaveLessonRequest = {
  folderId: string;
  title: string;
  lessonType: 'lesson' | 'test';
  content: string;
};

export type UpdateLessonRequest = Omit<SaveLessonRequest, 'folderId'>;

function authorizationHeaders(): { Authorization: string } {
  const token = getAuthToken();
  if (!token) {
    throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
  }

  return { Authorization: `Bearer ${token}` };
}

export async function getAdminFolders(): Promise<AdminFolder[]> {
  const response = await axios.get<AdminFolder[]>(`${API_URL}/folders`, {
    headers: authorizationHeaders(),
  });
  return response.data;
}

export async function getAdminLessons(folderId?: string): Promise<AdminLesson[]> {
  const response = await axios.get<AdminLesson[]>(API_URL, {
    headers: authorizationHeaders(),
    params: folderId ? { folderId } : undefined,
  });
  return response.data;
}

export async function getAdminLesson(id: number): Promise<AdminLessonContent> {
  const response = await axios.get<AdminLessonContent>(`${API_URL}/${id}`, {
    headers: authorizationHeaders(),
  });
  return response.data;
}

export async function createAdminLesson(request: SaveLessonRequest): Promise<AdminLessonContent> {
  const response = await axios.post<AdminLessonContent>(API_URL, request, {
    headers: authorizationHeaders(),
  });
  return response.data;
}

export async function updateAdminLesson(
  id: number,
  request: UpdateLessonRequest,
): Promise<AdminLessonContent> {
  const response = await axios.put<AdminLessonContent>(
    `${API_URL}/${id}`,
    request,
    { headers: authorizationHeaders() },
  );
  return response.data;
}

export async function deleteAdminLesson(id: number): Promise<void> {
  await axios.delete(`${API_URL}/${id}`, {
    headers: authorizationHeaders(),
  });
}

export function getAdminApiError(error: unknown): string {
  if (axios.isAxiosError<{ message?: string }>(error)) {
    if (error.response?.status === 401) {
      return error.response.data?.message
        ?? 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.';
    }
    if (error.response?.status === 403) {
      return error.response.data?.message
        ?? 'Tài khoản này chưa được cấp quyền quản trị nội dung.';
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
