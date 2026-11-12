using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishLearning.Application.Features.Chatbot;
using EnglishLearning.Application.Features.Chatbot.DTOs;
using EnglishLearning.Application.Features.Chatbot.Interfaces;
using EnglishLearning.Domain.Entities;
using EnglishLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnglishLearning.Infrastructure.Chatbot;

public sealed class GeminiChatbotService : IChatbotService
{
    private const int MaxContextMessages = 20;
    private const int MaxConversationMessages = 100;
    private const string DefaultModel = "gemini-3.5-flash-lite";
    private const string TutorInstructions =
        "You are English Buddy, a patient English tutor for learners. Help with English grammar, " +
        "vocabulary, correcting English sentences (explain corrections), and conversational practice. " +
        "Use English for conversation practice. Explain in Vietnamese when the learner asks or when " +
        "Vietnamese best clarifies a concept. Keep answers clear, friendly, and appropriate for a learner.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiChatbotService> _logger;

    public GeminiChatbotService(
        ApplicationDbContext context,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiChatbotService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChatbotConversationDto>> GetConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.ChatConversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Take(50)
            .Select(conversation => new ChatbotConversationDto(
                conversation.Id,
                conversation.Title,
                conversation.CreatedAt,
                conversation.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatbotMessageDto>?> GetMessagesAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var ownsConversation = await _context.ChatConversations
            .AsNoTracking()
            .AnyAsync(
                conversation => conversation.Id == conversationId && conversation.UserId == userId,
                cancellationToken);
        if (!ownsConversation)
        {
            return null;
        }

        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .ThenByDescending(message => message.Id)
            .Take(MaxConversationMessages)
            .ToListAsync(cancellationToken);

        messages.Reverse();
        return messages
            .Select(ToMessageDto)
            .ToList();
    }

    public async Task<SendChatMessageResponse> SendMessageAsync(
        Guid userId,
        SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var messageText = request.Message.Trim();
        if (messageText.Length == 0)
        {
            throw new ArgumentException("Tin nhắn không được để trống.", nameof(request));
        }
        if (messageText.Length > 4000)
        {
            throw new ArgumentException("Tin nhắn không được vượt quá 4000 ký tự.", nameof(request));
        }

        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);
        if (!userExists)
        {
            throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        }

        var conversation = request.ConversationId is Guid conversationId
            ? await _context.ChatConversations.SingleOrDefaultAsync(
                item => item.Id == conversationId && item.UserId == userId,
                cancellationToken)
            : null;

        if (request.ConversationId.HasValue && conversation is null)
        {
            throw new KeyNotFoundException("Không tìm thấy cuộc hội thoại.");
        }

        var history = conversation is null
            ? new List<ChatMessage>()
            : await _context.ChatMessages
                .AsNoTracking()
                .Where(item => item.ConversationId == conversation.Id)
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Take(MaxContextMessages)
                .ToListAsync(cancellationToken);
        history.Reverse();

        var assistantText = await GenerateResponseAsync(history, messageText, cancellationToken);
        var now = DateTime.UtcNow;

        if (conversation is null)
        {
            conversation = new ChatConversation
            {
                UserId = userId,
                Title = CreateTitle(messageText),
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.ChatConversations.Add(conversation);
        }
        else
        {
            conversation.UpdatedAt = now;
        }

        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = messageText,
            CreatedAt = now
        };
        var assistantMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = assistantText,
            CreatedAt = now.AddTicks(1)
        };

        _context.ChatMessages.AddRange(userMessage, assistantMessage);
        await _context.SaveChangesAsync(cancellationToken);

        return new SendChatMessageResponse(
            conversation.Id,
            ToMessageDto(userMessage),
            ToMessageDto(assistantMessage));
    }

    private async Task<string> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string currentMessage,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ChatbotConfigurationException(
                "Chatbot chưa được cấu hình. Hãy thiết lập Gemini:ApiKey trong User Secrets hoặc biến môi trường.");
        }

        var model = _configuration["Gemini:Model"];
        if (string.IsNullOrWhiteSpace(model))
        {
            model = DefaultModel;
        }

        var contents = history
            .Select(message => new GeminiContent(
                message.Role == "assistant" ? "model" : "user",
                [new GeminiPart(message.Content)]))
            .Append(new GeminiContent("user", [new GeminiPart(currentMessage)]))
            .ToArray();

        var payload = new GeminiRequest(
            new GeminiSystemInstruction([new GeminiPart(TutorInstructions)]),
            contents,
            new GeminiGenerationConfig(0.7, 1024));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                _logger.LogWarning(
                    "Gemini returned HTTP status {StatusCode}.",
                    statusCode);
                throw new ChatbotProviderException(
                    response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                            "Gemini từ chối quyền truy cập. Hãy kiểm tra API key và quyền sử dụng Gemini API.",
                        HttpStatusCode.TooManyRequests =>
                            "Gemini đang giới hạn yêu cầu hoặc đã hết quota. Hãy kiểm tra quota rồi thử lại.",
                        HttpStatusCode.BadRequest or HttpStatusCode.NotFound =>
                            $"Gemini không nhận diện model hoặc cấu hình (HTTP {statusCode}). Hãy kiểm tra Gemini:Model và API key.",
                        _ =>
                            $"Gemini trả HTTP {statusCode}. Hãy kiểm tra API key, model và log backend."
                    });
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken);

            var answer = ReadCandidateText(document.RootElement);
            if (string.IsNullOrWhiteSpace(answer))
            {
                _logger.LogWarning("Gemini returned no text candidate.");
                throw new ChatbotProviderException(
                    "Gemini chưa tạo được câu trả lời. Vui lòng thử diễn đạt lại câu hỏi.");
            }

            return answer.Trim();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Could not reach the Gemini API.");
            throw new ChatbotProviderException(
                "Không thể kết nối đến Gemini. Vui lòng thử lại sau.",
                exception);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Gemini returned an invalid response.");
            throw new ChatbotProviderException(
                "Gemini trả về dữ liệu không hợp lệ. Vui lòng thử lại sau.",
                exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "The request to Gemini timed out.");
            throw new ChatbotProviderException(
                "Gemini phản hồi quá lâu. Vui lòng thử lại sau.",
                exception);
        }
    }

    private static string? ReadCandidateText(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array ||
            candidates.GetArrayLength() == 0)
        {
            return null;
        }

        var candidate = candidates[0];
        if (candidate.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!candidate.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Object ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var textParts = parts
            .EnumerateArray()
            .Where(part => part.ValueKind == JsonValueKind.Object &&
                part.TryGetProperty("text", out var text) &&
                text.ValueKind == JsonValueKind.String)
            .Select(part => part.GetProperty("text").GetString())
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(Environment.NewLine, textParts);
    }

    private static string CreateTitle(string message)
    {
        const int maxTitleLength = 120;
        return message.Length <= maxTitleLength
            ? message
            : $"{message[..(maxTitleLength - 3)]}...";
    }

    private static ChatbotMessageDto ToMessageDto(ChatMessage message)
    {
        return new ChatbotMessageDto(
            message.Id,
            message.Role,
            message.Content,
            message.CreatedAt);
    }

    private sealed record GeminiRequest(
        [property: JsonPropertyName("systemInstruction")] GeminiSystemInstruction SystemInstruction,
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiSystemInstruction(
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);
}
