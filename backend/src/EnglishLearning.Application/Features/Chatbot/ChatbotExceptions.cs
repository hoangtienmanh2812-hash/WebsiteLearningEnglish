namespace EnglishLearning.Application.Features.Chatbot;

public sealed class ChatbotConfigurationException(string message) : Exception(message);

public sealed class ChatbotProviderException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class ChatbotQuotaExceededException(int retryAfterSeconds, string message)
    : Exception(message)
{
    public int RetryAfterSeconds { get; } = retryAfterSeconds;
}
