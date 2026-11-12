using System.ComponentModel.DataAnnotations;

namespace EnglishLearning.Application.Features.Chatbot.DTOs;

public sealed class SendChatMessageRequest
{
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;

    public Guid? ConversationId { get; init; }
}

public sealed record ChatbotConversationDto(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ChatbotMessageDto(
    long Id,
    string Role,
    string Content,
    DateTime CreatedAt);

public sealed record SendChatMessageResponse(
    Guid ConversationId,
    ChatbotMessageDto UserMessage,
    ChatbotMessageDto AssistantMessage);
