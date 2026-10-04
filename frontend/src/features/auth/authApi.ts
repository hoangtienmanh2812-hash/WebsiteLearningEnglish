import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5219/api/auth';

export type AuthResponse = {
  userId: string;
  email: string;
  fullName: string;
  token: string;
};

type ApiError = {
  message?: string;
};

export async function login(email: string, password: string): Promise<AuthResponse> {
  const response = await axios.post<AuthResponse>(`${API_URL}/login`, { email, password });
  return response.data;
}

export async function register(fullName: string, email: string, password: string): Promise<AuthResponse> {
  const response = await axios.post<AuthResponse>(`${API_URL}/register`, { fullName, email, password });
  return response.data;
}

export function getAuthError(error: unknown): string {
  if (axios.isAxiosError<ApiError>(error)) {
    if (!error.response) {
      return 'Không thể kết nối đến máy chủ. Vui lòng thử lại sau.';
    }

    return error.response.data?.message ?? 'Yêu cầu chưa thể xử lý. Vui lòng thử lại.';
  }

  return 'Đã có lỗi không mong muốn xảy ra. Vui lòng thử lại.';
}
