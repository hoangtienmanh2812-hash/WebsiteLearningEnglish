using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using EnglishLearning.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace EnglishLearning.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuizController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;

    public QuizController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuiz(int id)
    {
        // 1. Tìm bài học trong cơ sở dữ liệu SQL Server theo ID
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null || string.IsNullOrEmpty(lesson.DriveUrl))
        {
            return NotFound(new { message = "Không tìm thấy bài học hoặc chưa có link dữ liệu!" });
        }

        try
        {
            // 2. Dùng HttpClient tải nội dung file JSON trực tiếp từ Google Drive
            var jsonResponse = await _httpClient.GetStringAsync(lesson.DriveUrl);

            // 3. Trả thẳng chuỗi JSON đó về cho Frontend (React)
            return Content(jsonResponse, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi tải dữ liệu từ Google Drive", error = ex.Message });
        }
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
            var quizJson = await _httpClient.GetStringAsync(lesson.DriveUrl);
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
}