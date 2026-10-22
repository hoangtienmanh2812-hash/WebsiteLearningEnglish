using EnglishLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LessonsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LessonsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLessons([FromQuery] string? type)
    {
        var lessonType = string.IsNullOrWhiteSpace(type) ? "lesson" : type.Trim().ToLowerInvariant();
        if (lessonType is not ("lesson" or "test"))
        {
            return BadRequest(new { message = "Loại nội dung phải là 'lesson' hoặc 'test'." });
        }

        var lessons = await _context.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.LessonType.ToLower() == lessonType)
            .OrderBy(lesson => lesson.Id)
            .Select(lesson => new { lesson.Id, lesson.Title, lesson.LessonType })
            .ToListAsync();

        return Ok(lessons);
    }
}
