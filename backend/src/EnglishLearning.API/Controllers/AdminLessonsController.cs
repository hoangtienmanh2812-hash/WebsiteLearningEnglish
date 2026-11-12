using System.Net;
using System.Text.Json;
using EnglishLearning.Application.Features.Admin.DTOs;
using EnglishLearning.Application.Features.Admin.Interfaces;
using EnglishLearning.Domain.Entities;
using EnglishLearning.Infrastructure.Data;
using Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishLearning.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/lessons")]
public sealed class AdminLessonsController : ControllerBase
{
    private const int MaximumJsonLength = 2 * 1024 * 1024;
    private readonly ApplicationDbContext _context;
    private readonly IAdminLessonService _lessonService;
    private readonly ILogger<AdminLessonsController> _logger;

    public AdminLessonsController(
        ApplicationDbContext context,
        IAdminLessonService lessonService,
        ILogger<AdminLessonsController> logger)
    {
        _context = context;
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
        var metadata = await _context.Lessons
            .AsNoTracking()
            .SingleOrDefaultAsync(lesson => lesson.Id == id, cancellationToken);
        if (metadata is null)
        {
            return NotFound(new { message = "Không tìm thấy bài học." });
        }

        if (!string.Equals(metadata.LessonType, "test", StringComparison.OrdinalIgnoreCase))
        {
            var questions = await _context.Questions
                .AsNoTracking()
                .Where(question => question.LessonId == id)
                .OrderBy(question => question.Id)
                .ToListAsync(cancellationToken);

            return Ok(new AdminLessonContentDto
            {
                Id = metadata.Id,
                Title = metadata.Title,
                LessonType = metadata.LessonType,
                Content = SerializeSqlLessonContent(metadata.Title, questions)
            });
        }

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
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        if (string.Equals(request.LessonType, "test", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.FolderId))
        {
            return BadRequest(new { message = "Cần chọn thư mục lưu bài kiểm tra." });
        }

        if (string.Equals(request.LessonType, "lesson", StringComparison.OrdinalIgnoreCase))
        {
            var parsedQuestions = ParseSqlQuestions(request.Content);
            if (parsedQuestions.Error is not null)
            {
                return BadRequest(new { message = parsedQuestions.Error });
            }

            try
            {
                var lesson = new Lesson
                {
                    Title = request.Title.Trim(),
                    LessonType = "lesson"
                };
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                await LessonIdGenerator.AssignIdIfNeededAsync(_context, lesson, cancellationToken);
                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync(cancellationToken);
                foreach (var question in parsedQuestions.Questions!)
                {
                    question.LessonId = lesson.Id;
                    _context.Questions.Add(question);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return CreatedAtAction(nameof(GetLesson), new { id = lesson.Id }, new AdminLessonContentDto
                {
                    Id = lesson.Id,
                    Title = lesson.Title,
                    LessonType = lesson.LessonType,
                    Content = SerializeSqlLessonContent(lesson.Title, parsedQuestions.Questions)
                });
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Failed to save SQL-backed lesson content for {LessonTitle}.", request.Title);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Không thể lưu bài học vào SQL Server. Vui lòng kiểm tra cấu trúc bảng Lessons và Questions."
                });
            }
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

        var existingLesson = await _context.Lessons.FindAsync(new object?[] { id }, cancellationToken);
        if (existingLesson is null)
        {
            return NotFound(new { message = "Không tìm thấy bài học." });
        }

        if (!string.Equals(existingLesson.LessonType, request.LessonType, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Không thể đổi loại nội dung vì bài học và bài kiểm tra dùng nơi lưu trữ khác nhau."
            });
        }

        if (string.Equals(existingLesson.LessonType, "lesson", StringComparison.OrdinalIgnoreCase))
        {
            var parsedQuestions = ParseSqlQuestions(request.Content);
            if (parsedQuestions.Error is not null)
            {
                return BadRequest(new { message = parsedQuestions.Error });
            }

            existingLesson.Title = request.Title.Trim();
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var currentQuestions = await _context.Questions
                .Where(question => question.LessonId == id)
                .ToListAsync(cancellationToken);
            _context.Questions.RemoveRange(currentQuestions);
            foreach (var question in parsedQuestions.Questions!)
            {
                question.LessonId = id;
                _context.Questions.Add(question);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Ok(new AdminLessonContentDto
            {
                Id = existingLesson.Id,
                Title = existingLesson.Title,
                LessonType = existingLesson.LessonType,
                Content = SerializeSqlLessonContent(existingLesson.Title, parsedQuestions.Questions)
            });
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
        var lesson = await _context.Lessons.FindAsync(new object?[] { id }, cancellationToken);
        if (lesson is null)
        {
            return NotFound(new { message = "Không tìm thấy bài học." });
        }

        if (!string.Equals(lesson.LessonType, "test", StringComparison.OrdinalIgnoreCase))
        {
            var hasProgress = await _context.UserProgresses
                .AnyAsync(progress => progress.LessonId == id, cancellationToken);
            var hasXpAwards = await _context.UserQuestionXpAwards
                .AnyAsync(award => award.LessonId == id, cancellationToken);
            if (hasProgress || hasXpAwards)
            {
                return Conflict(new
                {
                    message = "Không thể xóa bài học đã có tiến độ hoặc EXP của người dùng. Hãy sửa câu hỏi thay vì xóa bài."
                });
            }

            var questions = await _context.Questions
                .Where(question => question.LessonId == id)
                .ToListAsync(cancellationToken);
            _context.Questions.RemoveRange(questions);
            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

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

    private static (List<Question>? Questions, string? Error) ParseSqlQuestions(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("questions", out var questionArray) ||
                questionArray.ValueKind != JsonValueKind.Array ||
                questionArray.GetArrayLength() == 0)
            {
                return (null, "Bài học SQL cần có ít nhất một câu hỏi.");
            }

            var questions = new List<Question>(questionArray.GetArrayLength());
            var questionNumber = 0;
            foreach (var item in questionArray.EnumerateArray())
            {
                questionNumber++;
                if (item.ValueKind != JsonValueKind.Object ||
                    !TryGetRequiredString(item, "question", out var questionText) ||
                    !TryGetRequiredString(item, "1", out var option1) ||
                    !TryGetRequiredString(item, "2", out var option2) ||
                    !TryGetRequiredString(item, "3", out var option3) ||
                    !TryGetRequiredString(item, "4", out var option4) ||
                    !TryGetRequiredString(item, "correct-ans", out var correctAnswer))
                {
                    return (null, $"Câu {questionNumber}: cần nhập nội dung, đủ 4 lựa chọn và đáp án đúng.");
                }

                var options = new[] { option1, option2, option3, option4 };
                if (correctAnswer is not ("1" or "2" or "3" or "4"))
                {
                    var correctAnswerIndex = Array.FindIndex(options, option =>
                        string.Equals(option, correctAnswer, StringComparison.OrdinalIgnoreCase));
                    if (correctAnswerIndex < 0)
                    {
                        return (null, $"Câu {questionNumber}: đáp án đúng phải khớp một trong bốn lựa chọn.");
                    }

                    correctAnswer = (correctAnswerIndex + 1).ToString();
                }

                questions.Add(new Question
                {
                    QuestionText = questionText,
                    Option1 = option1,
                    Option2 = option2,
                    Option3 = option3,
                    Option4 = option4,
                    CorrectAnswer = correctAnswer
                });
            }

            return (questions, null);
        }
        catch (JsonException)
        {
            return (null, "Nội dung câu hỏi không phải JSON hợp lệ.");
        }
    }

    private static bool TryGetRequiredString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString()?.Trim() ?? string.Empty;
        return value.Length > 0;
    }

    private static string SerializeSqlLessonContent(string title, IReadOnlyList<Question> questions)
    {
        var serializedQuestions = questions.Select(question =>
        {
            var serialized = new Dictionary<string, string>
            {
                ["question"] = question.QuestionText,
                ["1"] = question.Option1,
                ["2"] = question.Option2,
                ["3"] = question.Option3,
                ["4"] = question.Option4,
                ["correct-ans"] = GetCorrectAnswerIndex(question)
            };
            return serialized;
        });

        return JsonSerializer.Serialize(new
        {
            name = title,
            type = "lesson",
            questions = serializedQuestions,
            file_dinh_kem = ""
        });
    }

    private static string GetCorrectAnswerIndex(Question question)
    {
        var correctAnswer = question.CorrectAnswer?.Trim() ?? string.Empty;
        if (correctAnswer is "1" or "2" or "3" or "4")
        {
            return correctAnswer;
        }

        var options = new[] { question.Option1, question.Option2, question.Option3, question.Option4 };
        var index = Array.FindIndex(options, option =>
            string.Equals(option?.Trim(), correctAnswer, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? (index + 1).ToString() : string.Empty;
    }

    private static string? ValidateRequest(string title, string lessonType, string content)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 180)
        {
            return "Tiêu đề không được để trống và không vượt quá 180 ký tự.";
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
