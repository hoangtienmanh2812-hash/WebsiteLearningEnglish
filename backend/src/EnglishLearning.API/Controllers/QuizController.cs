using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EnglishLearning.Infrastructure.Data;
using EnglishLearning.Infrastructure.GoogleDrive;
using Google;
using EnglishLearning.Application.Features.Admin.Interfaces;
using EnglishLearning.Domain.Entities;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishLearning.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuizController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly GoogleDriveService _driveService;
    private readonly ISystemSettingsService _systemSettingsService;

    public QuizController(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        GoogleDriveService driveService,
        ISystemSettingsService systemSettingsService)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
        _driveService = driveService;
        _systemSettingsService = systemSettingsService;
    }

    public sealed record SubmitQuizAnswerRequest(string Answer);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuiz(
        int id,
        [FromQuery] string? lessonType,
        CancellationToken cancellationToken)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (lesson is null)
        {
            return NotFound(new { message = "Không tìm thấy bài học." });
        }

        if (!string.IsNullOrWhiteSpace(lessonType) &&
            !string.Equals(lesson.LessonType, lessonType, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = "Nội dung không thuộc loại được yêu cầu." });
        }

        if (string.Equals(lesson.LessonType, "test", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(lesson.DriveUrl))
            {
                return NotFound(new { message = "Bài kiểm tra chưa có đường dẫn dữ liệu Google Drive." });
            }

            try
            {
                var testContent = await _driveService.DownloadLessonContentAsync(
                    lesson.DriveUrl,
                    cancellationToken);
                return Content(testContent, "application/json");
            }
            catch (GoogleApiException)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message = "Không thể tải dữ liệu bài kiểm tra từ Google Drive." });
            }
        }

        var questions = await _context.Questions
            .AsNoTracking()
            .Where(question => question.LessonId == id)
            .OrderBy(question => question.Id)
            .ToListAsync(cancellationToken);
        var responseQuestions = new List<QuizQuestionResponse>(questions.Count);
        foreach (var question in questions)
        {
            var correctAnswer = GetCorrectAnswerIndex(question);
            if (correctAnswer is null)
            {
                return UnprocessableEntity(new
                {
                    message = $"Câu hỏi {question.Id} thiếu đáp án đúng hợp lệ trong cơ sở dữ liệu."
                });
            }

            responseQuestions.Add(new QuizQuestionResponse(
                question.QuestionText,
                question.Option1,
                question.Option2,
                question.Option3,
                question.Option4,
                correctAnswer));
        }

        string? attachmentUrl = null;
        string? attachmentError = null;
        if (!string.IsNullOrWhiteSpace(lesson.DriveUrl))
        {
            try
            {
                var content = await _driveService.DownloadLessonContentAsync(lesson.DriveUrl, cancellationToken);
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind == JsonValueKind.Object &&
                    document.RootElement.TryGetProperty("file_dinh_kem", out var attachmentElement) &&
                    attachmentElement.ValueKind == JsonValueKind.String)
                {
                    attachmentUrl = attachmentElement.GetString();
                }
            }
            catch (GoogleApiException)
            {
                attachmentError = "Không thể tải thông tin tài liệu đính kèm từ Google Drive.";
            }
            catch (JsonException)
            {
                attachmentError = "Dữ liệu tài liệu đính kèm không đúng định dạng JSON.";
            }
            catch (HttpRequestException)
            {
                attachmentError = "Không thể tải thông tin tài liệu đính kèm từ Google Drive.";
            }
            catch (InvalidOperationException)
            {
                attachmentError = "Không thể tải thông tin tài liệu đính kèm từ Google Drive.";
            }
        }

        return Ok(new
        {
            questions = responseQuestions,
            file_dinh_kem = attachmentUrl,
            attachment_error = attachmentError
        });
    }

    [Authorize]
    [HttpPost("{id}/answers/{questionIndex:int}")]
    public async Task<IActionResult> SubmitAnswer(
        int id,
        int questionIndex,
        [FromBody] SubmitQuizAnswerRequest request,
        CancellationToken cancellationToken)
    {
        if (questionIndex < 0 || request is null ||
            request.Answer is not ("1" or "2" or "3" or "4"))
        {
            return BadRequest(new { message = "Câu trả lời không hợp lệ." });
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new { message = "Không xác định được tài khoản hiện tại." });
        }

        var lesson = await _context.Lessons
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (lesson is null)
        {
            return NotFound(new { message = "Không tìm thấy bài học hoặc bài kiểm tra." });
        }

        string? correctAnswer;
        if (string.Equals(lesson.LessonType, "test", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(lesson.DriveUrl))
            {
                return NotFound(new { message = "Bài kiểm tra chưa có đường dẫn dữ liệu Google Drive." });
            }

            try
            {
                var content = await _driveService.DownloadLessonContentAsync(
                    lesson.DriveUrl,
                    cancellationToken);
                using var document = JsonDocument.Parse(content);
                if (!document.RootElement.TryGetProperty("questions", out var questions) ||
                    questions.ValueKind != JsonValueKind.Array ||
                    questionIndex >= questions.GetArrayLength())
                {
                    return BadRequest(new { message = "Không tìm thấy câu hỏi trong dữ liệu bài kiểm tra." });
                }

                var driveQuestion = questions[questionIndex];
                if (!driveQuestion.TryGetProperty("correct-ans", out var correctAnswerElement) ||
                    correctAnswerElement.ValueKind != JsonValueKind.String)
                {
                    return UnprocessableEntity(new { message = "Câu hỏi thiếu đáp án đúng hợp lệ." });
                }

                correctAnswer = correctAnswerElement.GetString()?.Trim();
                if (correctAnswer is not ("1" or "2" or "3" or "4"))
                {
                    return UnprocessableEntity(new { message = "Câu hỏi thiếu đáp án đúng hợp lệ." });
                }

                if (!driveQuestion.TryGetProperty(request.Answer, out var selectedOption) ||
                    selectedOption.ValueKind != JsonValueKind.String)
                {
                    return BadRequest(new { message = "Lựa chọn không tồn tại trong câu hỏi." });
                }
            }
            catch (GoogleApiException)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message = "Không thể xác minh đáp án từ Google Drive." });
            }
            catch (JsonException)
            {
                return UnprocessableEntity(new { message = "Dữ liệu bài kiểm tra không đúng định dạng JSON." });
            }
        }
        else
        {
            var question = await _context.Questions
                .AsNoTracking()
                .Where(item => item.LessonId == id)
                .OrderBy(item => item.Id)
                .Skip(questionIndex)
                .FirstOrDefaultAsync(cancellationToken);
            if (question is null)
            {
                return BadRequest(new { message = "Không tìm thấy câu hỏi trong cơ sở dữ liệu." });
            }

            correctAnswer = GetCorrectAnswerIndex(question);
            if (correctAnswer is null)
            {
                return UnprocessableEntity(new { message = "Câu hỏi thiếu đáp án đúng hợp lệ trong cơ sở dữ liệu." });
            }
        }

        if (!string.Equals(correctAnswer, request.Answer, StringComparison.Ordinal))
        {
            return Ok(new { correct = false, awarded = false, xpReward = 0 });
        }

        var existingAward = await _context.UserQuestionXpAwards.AnyAsync(award =>
            award.UserId == userId &&
            award.LessonId == id &&
            award.QuestionIndex == questionIndex,
            cancellationToken);
        if (existingAward)
        {
            return Ok(new { correct = true, awarded = false, xpReward = 0 });
        }

        var user = await _context.Users.FindAsync(new object?[] { userId }, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        var xpReward = await _systemSettingsService.GetXpRewardAsync(
            SystemSettingsService.CorrectAnswerXpKey,
            cancellationToken);
        user.Exp += xpReward;
        _context.UserQuestionXpAwards.Add(new UserQuestionXpAward
        {
            UserId = userId,
            LessonId = id,
            QuestionIndex = questionIndex,
            AwardedAt = DateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            var wasAlreadyAwarded = await _context.UserQuestionXpAwards.AnyAsync(award =>
                award.UserId == userId &&
                award.LessonId == id &&
                award.QuestionIndex == questionIndex,
                cancellationToken);
            if (wasAlreadyAwarded)
            {
                return Ok(new { correct = true, awarded = false, xpReward = 0 });
            }

            throw;
        }

        return Ok(new { correct = true, awarded = true, xpReward });
    }

    [HttpGet("{id}/attachment")]
    public async Task<IActionResult> GetAttachment(int id)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null || string.IsNullOrEmpty(lesson.DriveUrl))
        {
            return NotFound(new { message = "Không tìm thấy bài học hoặc chưa có link dữ liệu!" });
        }

        try
        {
            var quizJson = await _driveService.DownloadLessonContentAsync(
                lesson.DriveUrl,
                HttpContext.RequestAborted);
            using var document = JsonDocument.Parse(quizJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("file_dinh_kem", out var attachmentElement) ||
                attachmentElement.ValueKind != JsonValueKind.String)
            {
                return NotFound(new { message = "Bài học chưa có file đọc hoặc nghe đính kèm." });
            }

            var attachmentUrl = attachmentElement.GetString();
            if (!Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(new { message = "Đường dẫn file đính kèm không hợp lệ." });
            }

            using var attachmentResponse = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            if (!attachmentResponse.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Không thể tải file đính kèm." });
            }

            var content = await attachmentResponse.Content.ReadAsByteArrayAsync();
            var contentType = attachmentResponse.Content.Headers.ContentType?.MediaType;
            if (string.IsNullOrWhiteSpace(contentType) || contentType == "application/octet-stream")
            {
                var fileName = attachmentResponse.Content.Headers.ContentDisposition?.FileNameStar
                    ?? attachmentResponse.Content.Headers.ContentDisposition?.FileName
                    ?? Path.GetFileName(uri.LocalPath);
                var contentTypeProvider = new FileExtensionContentTypeProvider();
                if (!contentTypeProvider.TryGetContentType(fileName, out contentType) ||
                    string.IsNullOrWhiteSpace(contentType) ||
                    contentType == "application/octet-stream")
                {
                    contentType = DetectContentType(content);
                }
            }

            return File(content, contentType, enableRangeProcessing: true);
        }
        catch (JsonException)
        {
            return UnprocessableEntity(new { message = "Dữ liệu bài học không đúng định dạng JSON." });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Lỗi khi tải file từ Google Drive." });
        }
        catch (GoogleApiException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Không thể tải dữ liệu bài học từ Google Drive." });
        }
    }

    private static string DetectContentType(byte[] content)
    {
        if (content.AsSpan().StartsWith("%PDF-"u8))
        {
            return "application/pdf";
        }

        if (content.AsSpan().StartsWith("ID3"u8) ||
            (content.Length >= 2 && content[0] == 0xFF && (content[1] & 0xE0) == 0xE0))
        {
            return "audio/mpeg";
        }

        if (content.Length >= 12 &&
            content.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            content.AsSpan(8, 4).SequenceEqual("WAVE"u8))
        {
            return "audio/wav";
        }

        if (content.AsSpan().StartsWith("OggS"u8))
        {
            return "audio/ogg";
        }

        if (content.AsSpan().StartsWith("fLaC"u8))
        {
            return "audio/flac";
        }

        try
        {
            var text = new UTF8Encoding(false, true).GetString(content);
            if (text.Length > 0 && text.All(character =>
                    !char.IsControl(character) || character is '\r' or '\n' or '\t'))
            {
                return "text/plain";
            }
        }
        catch (DecoderFallbackException)
        {
            // The bytes are not valid UTF-8 text.
        }

        return "application/octet-stream";
    }

    private static string? GetCorrectAnswerIndex(Question question)
    {
        var correctAnswer = question.CorrectAnswer.Trim();
        if (correctAnswer is "1" or "2" or "3" or "4")
        {
            return correctAnswer;
        }

        var options = new[] { question.Option1, question.Option2, question.Option3, question.Option4 };
        var answerIndex = Array.FindIndex(options, option =>
            string.Equals(option.Trim(), correctAnswer, StringComparison.OrdinalIgnoreCase));

        return answerIndex >= 0 ? (answerIndex + 1).ToString() : null;
    }

    private sealed record QuizQuestionResponse(
        [property: JsonPropertyName("question")] string Question,
        [property: JsonPropertyName("1")] string Option1,
        [property: JsonPropertyName("2")] string Option2,
        [property: JsonPropertyName("3")] string Option3,
        [property: JsonPropertyName("4")] string Option4,
        [property: JsonPropertyName("correct-ans")] string CorrectAnswer);
}
