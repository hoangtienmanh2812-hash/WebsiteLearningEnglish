import axios from 'axios';
import { getAuthToken } from '../auth/authStorage';

const API_URL = `${import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5219'}/api/chatbot`;

export type ChatbotMessage = {
  id: number;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
};

export type ChatbotConversation = {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
};

type SendMessageResponse = {
  conversationId: string;
  userMessage: ChatbotMessage;
  assistantMessage: ChatbotMessage;
};

function authorizationHeaders(): { Authorization: string } {
  const token = getAuthToken();
  if (!token) {
    throw new Error('Bạn cần đăng nhập để sử dụng English Buddy.');
  }
  return { Authorization: `Bearer ${token}` };
}

export async function getChatbotConversations(
  signal?: AbortSignal,
): Promise<ChatbotConversation[]> {
  const response = await axios.get<ChatbotConversation[]>(`${API_URL}/conversations`, {
    headers: authorizationHeaders(),
    signal,
  });
  return response.data;
}

export async function getChatbotMessages(
  conversationId: string,
  signal?: AbortSignal,
): Promise<ChatbotMessage[]> {
  const response = await axios.get<ChatbotMessage[]>(
    `${API_URL}/conversations/${conversationId}/messages`,
    {
      headers: authorizationHeaders(),
      signal,
    },
  );
  return response.data;
}

export async function sendChatbotMessage(
  message: string,
  conversationId: string | null,
  signal?: AbortSignal,
): Promise<SendMessageResponse> {
  const response = await axios.post<SendMessageResponse>(
    `${API_URL}/messages`,
    { message, conversationId },
    {
      headers: authorizationHeaders(),
      signal,
    },
  );
  return response.data;
}

export function getChatbotApiError(error: unknown): string {
  if (axios.isAxiosError<{
    message?: string;
    title?: string;
    errors?: Record<string, string[]>;
  }>(error)) {
    if (error.response?.status === 401) {
      return 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
    }
    if (!error.response) {
      return 'Không thể kết nối đến máy chủ. Hãy kiểm tra backend và thử lại.';
    }

    const responseData = error.response.data;
    const validationErrors = responseData?.errors
      ? Object.values(responseData.errors).flat()
      : [];
    if (responseData?.message) return responseData.message;
    if (validationErrors.length > 0) return validationErrors.join(' ');
    if (responseData?.title) return responseData.title;
    return `Backend trả lỗi HTTP ${error.response.status}. Hãy kiểm tra log backend.`;
  }

  return error instanceof Error ? error.message : 'Đã có lỗi không mong muốn xảy ra.';
}
