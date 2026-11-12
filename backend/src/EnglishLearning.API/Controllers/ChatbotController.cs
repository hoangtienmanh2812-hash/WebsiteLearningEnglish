using System.Security.Claims;
using EnglishLearning.Application.Features.Chatbot;
using EnglishLearning.Application.Features.Chatbot.DTOs;
using EnglishLearning.Application.Features.Chatbot.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.API.Controllers;

[ApiController]
[Authorize]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "Token không chứa ID người dùng hợp lệ." });
        }

        var conversations = await _chatbotService.GetConversationsAsync(userId, cancellationToken);
        return Ok(conversations);
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "Token không chứa ID người dùng hợp lệ." });
        }

        var messages = await _chatbotService.GetMessagesAsync(
            userId,
            conversationId,
            cancellationToken);
        return messages is null
            ? NotFound(new { message = "Không tìm thấy cuộc hội thoại." })
            : Ok(messages);
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "Token không chứa ID người dùng hợp lệ." });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Tin nhắn không được để trống." });
        }

        try
        {
            var response = await _chatbotService.SendMessageAsync(
                userId,
                request,
                cancellationToken);
            return Ok(response);
        }
        catch (ChatbotConfigurationException exception)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = exception.Message });
        }
        catch (ChatbotProviderException exception)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(userIdValue, out userId);
    }
}
