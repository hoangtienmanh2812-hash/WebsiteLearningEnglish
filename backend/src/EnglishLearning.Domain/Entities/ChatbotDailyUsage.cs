namespace EnglishLearning.Domain.Entities;

public sealed class ChatbotDailyUsage
{
    public Guid UserId { get; set; }
    public DateOnly UsageDate { get; set; }
    public int RequestCount { get; set; }
}
