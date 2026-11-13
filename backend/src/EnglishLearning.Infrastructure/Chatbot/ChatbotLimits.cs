namespace EnglishLearning.Infrastructure.Chatbot;

public sealed class ChatbotLimits
{
    public int RequestsPerMinute { get; init; } = 5;
    public int RequestsPerDay { get; init; } = 20;
    public int MaxInputCharacters { get; init; } = 2000;
    public int MaxOutputTokens { get; init; } = 512;
    public int MaxContextMessages { get; init; } = 10;

    public void Validate()
    {
        if (RequestsPerMinute <= 0 ||
            RequestsPerDay <= 0 ||
            MaxInputCharacters <= 0 ||
            MaxOutputTokens <= 0 ||
            MaxContextMessages < 0)
        {
            throw new InvalidOperationException(
                "Các giá trị Chatbot:Limits phải lớn hơn 0, ngoại trừ MaxContextMessages có thể bằng 0.");
        }
    }
}
