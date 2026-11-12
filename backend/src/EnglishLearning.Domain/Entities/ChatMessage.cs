namespace EnglishLearning.Domain.Entities;

public sealed class ChatMessage
{
    public long Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChatConversation? Conversation { get; set; }
}
