using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnglishLearning.Infrastructure.Data;

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
}