using EnglishLearning.Application.Features.Chatbot.DTOs;

namespace EnglishLearning.Application.Features.Chatbot.Interfaces;

public interface IChatbotService
{
    Task<IReadOnlyList<ChatbotConversationDto>> GetConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatbotMessageDto>?> GetMessagesAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<SendChatMessageResponse> SendMessageAsync(
        Guid userId,
        SendChatMessageRequest request,
        CancellationToken cancellationToken);
}
