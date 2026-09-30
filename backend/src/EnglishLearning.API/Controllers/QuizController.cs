using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnglishLearning.Infrastructure.Data; // Thêm namespace chứa ApplicationDbContext

namespace EnglishLearning.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuizController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    // Tiêm ApplicationDbContext vào Controller
    public QuizController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuizQuestions(int id)
    {
        // Truy vấn danh sách câu hỏi từ database dựa theo LessonId (id bài học)
        var questionsFromDb = await _context.Questions
            .Where(q => q.LessonId == id)
            .ToListAsync();

        if (questionsFromDb == null || !questionsFromDb.Any())
        {
            return NotFound(new { message = "Không tìm thấy câu hỏi cho bài học này." });
        }

        // Map dữ liệu từ DB sang cấu trúc mà Frontend đang cần
        var result = questionsFromDb.Select(q => new
        {
            id = q.Id,
            question = q.QuestionText,
            options = new string[] { q.Option1, q.Option2, q.Option3, q.Option4 },
            correctAnswer = q.CorrectAnswer
        });

        return Ok(result);
    }
}