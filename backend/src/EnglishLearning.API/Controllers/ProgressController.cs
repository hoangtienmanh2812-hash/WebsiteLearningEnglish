using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnglishLearning.Infrastructure.Data;

namespace EnglishLearning.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProgressController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProgressController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserProgress(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return BadRequest(new { message = $"UserId không hợp lệ: {userId}" });
            }

            var lessons = await _context.Lessons
                .AsNoTracking()
                .Where(lesson => lesson.LessonType.ToLower() == "lesson")
                .OrderBy(lesson => lesson.Id)
                .Select(lesson => new { lesson.Id, lesson.Title })
                .ToListAsync();
            var userProgresses = await _context.UserProgresses
                .Where(p => p.UserId == parsedUserId)
                .ToListAsync();

            var result = new List<object>();

            for (var index = 0; index < lessons.Count; index++)
            {
                var lesson = lessons[index];
                var isDone = userProgresses.Any(progress => progress.LessonId == lesson.Id && progress.IsCompleted);
                var previousLessonDone = index == 0 || userProgresses.Any(progress =>
                    progress.LessonId == lessons[index - 1].Id && progress.IsCompleted);
                result.Add(new {
                    lessonId = lesson.Id,
                    title = lesson.Title,
                    isUnlocked = previousLessonDone,
                    isCompleted = isDone
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in GetUserProgress: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteLesson([FromBody] CompleteLessonDto dto)
    {
        try
        {
            if (dto == null || dto.UserId == Guid.Empty)
            {
                return BadRequest(new { message = "Dữ liệu gửi lên không hợp lệ." });
            }

            var isLesson = await _context.Lessons.AnyAsync(lesson =>
                lesson.Id == dto.LessonId && lesson.LessonType.ToLower() == "lesson");
            if (!isLesson)
            {
                return BadRequest(new { message = "Chỉ bài học thuộc loại 'lesson' mới được cập nhật tiến độ." });
            }

            var progress = await _context.UserProgresses
                .FirstOrDefaultAsync(p => p.UserId == dto.UserId && p.LessonId == dto.LessonId);

            if (progress == null)
            {
                _context.UserProgresses.Add(new Domain.Entities.UserProgress
                {
                    UserId = dto.UserId,
                    LessonId = dto.LessonId,
                    IsCompleted = true
                });
            }
            else
            {
                progress.IsCompleted = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã hoàn thành bài học!" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in CompleteLesson: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class CompleteLessonDto
{
    public Guid UserId { get; set; }
    public int LessonId { get; set; }
}
