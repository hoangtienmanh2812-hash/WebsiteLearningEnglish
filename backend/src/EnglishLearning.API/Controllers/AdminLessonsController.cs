using System.Net;
using System.Text.Json;
using EnglishLearning.Application.Features.Admin.DTOs;
using EnglishLearning.Application.Features.Admin.Interfaces;
using Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EnglishLearning.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/lessons")]
public sealed class AdminLessonsController : ControllerBase
{
    private const int MaximumJsonLength = 2 * 1024 * 1024;
    private readonly IAdminLessonService _lessonService;
    private readonly ILogger<AdminLessonsController> _logger;

    public AdminLessonsController(
        IAdminLessonService lessonService,
        ILogger<AdminLessonsController> logger)
    {
        _lessonService = lessonService;
        _logger = logger;
    }

    [HttpGet("folders")]
    public async Task<IActionResult> GetFolders(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _lessonService.GetFoldersAsync(cancellationToken));
        }
        catch (GoogleApiException exception)
        {
            return DriveError(exception);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLessons([FromQuery] string? folderId, CancellationToken cancellationToken)
    {
        try
        {
            var lessons = await _lessonService.GetLessonsAsync(folderId, cancellationToken);
            if (folderId is not null && lessons is null)
            {
                return NotFound(new { message = "Không tìm thấy thư mục nội dung." });
            }

            return Ok(lessons);
        }
        catch (GoogleApiException exception)
        {
            return DriveError(exception);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLesson(int id, CancellationToken cancellationToken)
    {
        try
        {
            var lesson = await _lessonService.GetLessonAsync(id, cancellationToken);
            return lesson is null
                ? NotFound(new { message = "Không tìm thấy bài học." })
                : Ok(lesson);
        }
        catch (GoogleApiException exception)
        {
            return DriveError(exception);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateLesson(
        [FromBody] SaveAdminLessonRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request.Title, request.LessonType, request.Content);
        if (validationError is not null || string.IsNullOrWhiteSpace(request.FolderId))
        {
            return BadRequest(new { message = validationError ?? "Cần chọn thư mục lưu bài học." });
        }

        try
        {
            var lesson = await _lessonService.CreateLessonAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetLesson), new { id = lesson.Id }, lesson);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (GoogleApiException exception)
        {
            return DriveError(exception);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLesson(
        int id,
        [FromBody] UpdateAdminLessonRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request.Title, request.LessonType, request.Content);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        try
        {
            var lesson = await _lessonService.UpdateLessonAsync(id, request, cancellationToken);
            return lesson is null
                ? NotFound(new { message = "Không tìm thấy bài học." })
                : Ok(lesson);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (GoogleApiException exception)
        {
            return DriveError(exception);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLesson(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await _lessonService.DeleteLessonAsync(id, cancellationToken)
                ? NoContent()
                : NotFound(new { message = "Không tìm thấy bài học." });
        }
        catch (GoogleApiException exception)
        {
            return DriveError(exception);
        }
    }

    private static string? ValidateRequest(string title, string lessonType, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Tiêu đề bài học không được để trống.";
        }

        if (string.IsNullOrWhiteSpace(lessonType) ||
            lessonType.Trim().ToLowerInvariant() is not ("lesson" or "test"))
        {
            return "Loại bài học phải là 'lesson' hoặc 'test'.";
        }

        if (string.IsNullOrWhiteSpace(content) || content.Length > MaximumJsonLength)
        {
            return "Nội dung JSON trống hoặc vượt quá giới hạn 2 MB.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                return "Nội dung JSON phải là object hoặc array.";
            }
        }
        catch (JsonException)
        {
            return "Nội dung không phải JSON hợp lệ.";
        }

        return null;
    }

    private ObjectResult DriveError(GoogleApiException exception)
    {
        var reasons = exception.Error?.Errors?
            .Select(error => error.Reason)
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
        var reason = reasons.FirstOrDefault();

        _logger.LogError(
            exception,
            "Google Drive request failed with HTTP {StatusCode}; reason codes: {ReasonCodes}",
            (int)exception.HttpStatusCode,
            reasons.Length == 0 ? "none" : string.Join(", ", reasons));

        if (reason is not null)
        {
            var reasonMessage = reason switch
            {
                "storageQuotaExceeded" =>
                    "Service Account đã hết hoặc không có dung lượng Drive để tạo file. Hãy dùng Shared Drive hoặc xác thực bằng tài khoản Google có dung lượng.",
                "insufficientFilePermissions" =>
                    "Service Account chưa có đủ quyền trên đúng thư mục/file Google Drive đang thao tác.",
                "teamDriveMembershipRequired" =>
                    "Service Account cần được thêm làm thành viên của Shared Drive chứa thư mục này.",
                "domainPolicy" =>
                    "Chính sách Google Workspace đang chặn Service Account truy cập hoặc ghi vào thư mục.",
                "accessNotConfigured" =>
                    "Google Drive API chưa được bật trong Google Cloud project của Service Account.",
                _ => $"Google Drive từ chối yêu cầu (reason: {reason})."
            };

            return StatusCode(
                (int)exception.HttpStatusCode is >= 400 and < 600
                    ? (int)exception.HttpStatusCode
                    : StatusCodes.Status502BadGateway,
                new { message = reasonMessage, reason });
        }

        var (statusCode, message) = exception.HttpStatusCode switch
        {
            HttpStatusCode.NotFound => (StatusCodes.Status404NotFound, "File hoặc thư mục Google Drive không còn tồn tại."),
            HttpStatusCode.Forbidden => (StatusCodes.Status403Forbidden, "Google Drive từ chối thao tác. Hãy kiểm tra quyền thư mục, Shared Drive và dung lượng của Service Account."),
            HttpStatusCode.Conflict => (StatusCodes.Status409Conflict, "File bị xung đột với dữ liệu hiện có trên Google Drive."),
            _ => (StatusCodes.Status502BadGateway, "Google Drive chưa thể xử lý yêu cầu.")
        };

        return StatusCode(statusCode, new { message });
    }
}
