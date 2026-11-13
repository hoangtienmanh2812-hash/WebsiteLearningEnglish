import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type MouseEvent as ReactMouseEvent,
  type PointerEvent as ReactPointerEvent,
} from 'react';
import {
  getChatbotApiError,
  getChatbotConversations,
  getChatbotMessages,
  sendChatbotMessage,
  type ChatbotMessage,
} from '../../features/chatbot/chatbotApi';
import './chatbotWidget.css';

interface ChatMessage {
  id: number;
  text: string;
  sender: 'assistant' | 'user';
}

interface FloatingPosition {
  x: number;
  y: number;
}

interface DragState extends FloatingPosition {
  pointerId: number;
  startX: number;
  startY: number;
  moved: boolean;
}

const starterMessage: ChatMessage = {
  id: 0,
  sender: 'assistant',
  text: 'Chào bạn! Mình là English Buddy. Bạn có thể hỏi mình về từ vựng, ngữ pháp hoặc luyện giao tiếp tiếng Anh nhé.',
};

function toChatMessage(message: ChatbotMessage): ChatMessage {
  return {
    id: message.id,
    text: message.content,
    sender: message.role,
  };
}

const suggestions = [
  'Phân biệt “say” và “tell”',
  'Luyện giao tiếp cùng mình',
  'Gợi ý từ vựng hôm nay',
];

export default function ChatbotWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [draft, setDraft] = useState('');
  const [messages, setMessages] = useState<ChatMessage[]>([starterMessage]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [historyLoaded, setHistoryLoaded] = useState(false);
  const [historyLoadVersion, setHistoryLoadVersion] = useState(0);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const bodyRef = useRef<HTMLDivElement | null>(null);
  const widgetRef = useRef<HTMLDivElement | null>(null);
  const dragRef = useRef<DragState | null>(null);
  const suppressClickRef = useRef(false);
  const [floatingPosition, setFloatingPosition] = useState<FloatingPosition | null>(null);

  useEffect(() => {
    if (!floatingPosition || !widgetRef.current) return;
    const position = floatingPosition;

    function keepWidgetInViewport() {
      const bounds = widgetRef.current?.getBoundingClientRect();
      if (!bounds) return;

      const x = Math.min(Math.max(0, position.x), window.innerWidth - bounds.width);
      const y = Math.min(Math.max(0, position.y), window.innerHeight - bounds.height);
      if (x !== position.x || y !== position.y) {
        setFloatingPosition({ x, y });
      }
    }

    keepWidgetInViewport();
    window.addEventListener('resize', keepWidgetInViewport);
    return () => window.removeEventListener('resize', keepWidgetInViewport);
  }, [floatingPosition, isOpen]);

  useEffect(() => {
    if (!isOpen || historyLoaded) return;

    const controller = new AbortController();
    async function loadLatestConversation() {
      setIsLoadingHistory(true);
      setError(null);
      try {
        const conversations = await getChatbotConversations(controller.signal);
        const latestConversation = conversations[0];
        if (latestConversation) {
          const history = await getChatbotMessages(latestConversation.id, controller.signal);
          setConversationId(latestConversation.id);
          if (history.length > 0) {
            setMessages(history.map(toChatMessage));
          }
        }
        setHistoryLoaded(true);
      } catch (caughtError) {
        if (!controller.signal.aborted) {
          setError(getChatbotApiError(caughtError));
        }
      } finally {
        if (!controller.signal.aborted) setIsLoadingHistory(false);
      }
    }

    void loadLatestConversation();
    return () => controller.abort();
  }, [historyLoadVersion, historyLoaded, isOpen]);

  useEffect(() => {
    if (isOpen && bodyRef.current) {
      bodyRef.current.scrollTop = bodyRef.current.scrollHeight;
    }
  }, [isLoadingHistory, isOpen, isSending, messages]);

  async function sendMessage(text: string) {
    const message = text.trim();
    if (!message || isSending || isLoadingHistory) return;

    setIsSending(true);
    setError(null);
    try {
      const response = await sendChatbotMessage(message, conversationId);
      setConversationId(response.conversationId);
      setMessages((current) => [
        ...current,
        toChatMessage(response.userMessage),
        toChatMessage(response.assistantMessage),
      ]);
      setDraft('');
      setHistoryLoaded(true);
    } catch (caughtError) {
      setError(getChatbotApiError(caughtError));
    } finally {
      setIsSending(false);
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void sendMessage(draft);
  }

  function handleLauncherPointerDown(event: ReactPointerEvent<HTMLButtonElement>) {
    if (!event.isPrimary || event.button !== 0 || !widgetRef.current) return;

    const bounds = widgetRef.current.getBoundingClientRect();
    dragRef.current = {
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
      x: bounds.left,
      y: bounds.top,
      moved: false,
    };
    suppressClickRef.current = false;
    event.currentTarget.setPointerCapture(event.pointerId);
  }

  function handleLauncherPointerMove(event: ReactPointerEvent<HTMLButtonElement>) {
    const drag = dragRef.current;
    if (!drag || drag.pointerId !== event.pointerId) return;

    const deltaX = event.clientX - drag.startX;
    const deltaY = event.clientY - drag.startY;
    if (!drag.moved && Math.hypot(deltaX, deltaY) < 5) return;

    drag.moved = true;
    suppressClickRef.current = true;
    const bounds = widgetRef.current?.getBoundingClientRect();
    if (!bounds) return;

    setFloatingPosition({
      x: Math.min(Math.max(0, drag.x + deltaX), window.innerWidth - bounds.width),
      y: Math.min(Math.max(0, drag.y + deltaY), window.innerHeight - bounds.height),
    });
  }

  function handleLauncherPointerUp(event: ReactPointerEvent<HTMLButtonElement>) {
    if (dragRef.current?.pointerId === event.pointerId) {
      dragRef.current = null;
    }
  }

  function handleLauncherClick(event: ReactMouseEvent<HTMLButtonElement>) {
    if (suppressClickRef.current) {
      event.preventDefault();
      suppressClickRef.current = false;
      return;
    }
    setIsOpen((open) => !open);
  }

  return (
    <div
      className="chatbot"
      ref={widgetRef}
      style={floatingPosition ? {
        left: floatingPosition.x,
        top: floatingPosition.y,
        right: 'auto',
        bottom: 'auto',
      } : undefined}
      data-panel-alignment={
        floatingPosition && floatingPosition.x < window.innerWidth / 2 ? 'left' : 'right'
      }
    >
      {isOpen && (
        <section
          className="chatbot-panel"
          aria-label="Trò chuyện với English Buddy"
          aria-busy={isLoadingHistory || isSending}
        >
          <header className="chatbot-panel__header">
            <span className="chatbot-avatar" aria-hidden="true">
              <svg viewBox="0 0 32 32" fill="none">
                <path d="M8 13.5a8 8 0 0 1 16 0v7a3 3 0 0 1-3 3h-2v-7h4M8 16.5h4v7h-1a3 3 0 0 1-3-3v-4Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                <path d="M13 14h.01M19 14h.01M13 19c1.7 1.4 4.3 1.4 6 0" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" />
              </svg>
            </span>
            <div className="chatbot-panel__identity">
              <strong>English Buddy</strong>
              <span><i aria-hidden="true" /> Gia sư tiếng Anh</span>
            </div>
            <button
              className="chatbot-panel__close"
              type="button"
              onClick={() => setIsOpen(false)}
              aria-label="Đóng khung chat"
            >
              <svg viewBox="0 0 20 20" aria-hidden="true">
                <path d="m5 5 10 10M15 5 5 15" />
              </svg>
            </button>
          </header>

          <div className="chatbot-panel__body" ref={bodyRef}>
            <div className="chatbot-panel__welcome">
              <span className="chatbot-avatar chatbot-avatar--small" aria-hidden="true">
                <svg viewBox="0 0 32 32" fill="none">
                  <path d="M8 13.5a8 8 0 0 1 16 0v7a3 3 0 0 1-3 3h-2v-7h4M8 16.5h4v7h-1a3 3 0 0 1-3-3v-4Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                  <path d="M13 14h.01M19 14h.01M13 19c1.7 1.4 4.3 1.4 6 0" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" />
                </svg>
              </span>
              <div>
                <strong>English Buddy</strong>
                <span>Hôm nay mình có thể giúp gì cho bạn?</span>
              </div>
            </div>

            <div className="chatbot-panel__messages" aria-live="polite">
              {messages.map((message) => (
                <div
                  className={`chatbot-message chatbot-message--${message.sender}`}
                  key={message.id}
                >
                  {message.sender === 'assistant' && (
                    <span className="chatbot-message__avatar" aria-hidden="true">E</span>
                  )}
                  <p>{message.text}</p>
                </div>
              ))}
              {isSending && (
                <div className="chatbot-message chatbot-message--assistant" role="status">
                  <span className="chatbot-message__avatar" aria-hidden="true">E</span>
                  <p className="chatbot-typing"><i /><i /><i /><span className="visually-hidden">English Buddy đang trả lời</span></p>
                </div>
              )}
            </div>

            {isLoadingHistory && (
              <p className="chatbot-panel__loading" role="status">Đang tải hội thoại...</p>
            )}

            {messages.length === 1 && historyLoaded && (
              <div className="chatbot-suggestions" aria-label="Gợi ý câu hỏi">
                {suggestions.map((suggestion) => (
                  <button
                    key={suggestion}
                    type="button"
                    disabled={isLoadingHistory || isSending}
                    onClick={() => void sendMessage(suggestion)}
                  >
                    {suggestion}
                    <svg viewBox="0 0 16 16" aria-hidden="true">
                      <path d="m6 3 5 5-5 5" />
                    </svg>
                  </button>
                ))}
              </div>
            )}
          </div>

          {error && (
            <div className="chatbot-panel__error" role="alert">
              <span>{error}</span>
              {!historyLoaded && !isLoadingHistory && (
                <button
                  type="button"
                  onClick={() => setHistoryLoadVersion((version) => version + 1)}
                >
                  Thử tải lại
                </button>
              )}
            </div>
          )}

          <form className="chatbot-composer" onSubmit={handleSubmit}>
            <label className="chatbot-composer__input">
              <span className="visually-hidden">Nhập tin nhắn</span>
              <input
                type="text"
                value={draft}
                onChange={(event) => setDraft(event.target.value)}
                placeholder="Nhắn tin cho English Buddy..."
                disabled={!historyLoaded || isLoadingHistory || isSending}
              />
            </label>
            <button
              className="chatbot-composer__send"
              type="submit"
              disabled={!draft.trim() || !historyLoaded || isLoadingHistory || isSending}
              aria-label="Gửi tin nhắn"
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="m21 3-7.2 18-3.9-7.9L2 9.2 21 3Z" />
                <path d="M9.9 13.1 21 3" />
              </svg>
            </button>
          </form>
          <p className="chatbot-panel__disclaimer">Câu trả lời được tạo bởi Gemini</p>
        </section>
      )}

      <button
        className={`chatbot-launcher${isOpen ? ' chatbot-launcher--open' : ''}`}
        type="button"
        onPointerDown={handleLauncherPointerDown}
        onPointerMove={handleLauncherPointerMove}
        onPointerUp={handleLauncherPointerUp}
        onPointerCancel={handleLauncherPointerUp}
        onClick={handleLauncherClick}
        aria-label={isOpen ? 'Đóng English Buddy' : 'Mở chat với English Buddy'}
        aria-expanded={isOpen}
        title="Kéo để di chuyển, nhấn để mở chat"
      >
        {isOpen ? (
          <svg viewBox="0 0 24 24" aria-hidden="true">
            <path d="m6 6 12 12M18 6 6 18" />
          </svg>
        ) : (
          <>
            <svg viewBox="0 0 28 28" aria-hidden="true">
              <path d="M14 3.5a10.5 10.5 0 0 0-9 15.9L4 24l4.8-1.6A10.5 10.5 0 1 0 14 3.5Z" />
              <path d="M9.5 11h.01M14 11h.01M18.5 11h.01M9 15.5c1.1 1.6 2.8 2.5 5 2.5s3.9-.9 5-2.5" />
            </svg>
          </>
        )}
      </button>
    </div>
  );
}
