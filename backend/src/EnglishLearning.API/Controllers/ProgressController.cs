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

            var totalLessons = 10;
            var userProgresses = await _context.UserProgresses
                .Where(p => p.UserId == parsedUserId)
                .ToListAsync();

            var result = new List<object>();

            for (int i = 1; i <= totalLessons; i++)
            {
                if (i == 1)
                {
                    var isDone = userProgresses.Any(p => p.LessonId == 1 && p.IsCompleted);
                    result.Add(new { lessonId = 1, isUnlocked = true, isCompleted = isDone });
                }
                else
                {
                    var previousLessonCompleted = userProgresses.Any(p => p.LessonId == (i - 1) && p.IsCompleted);
                    var currentLessonDone = userProgresses.Any(p => p.LessonId == i && p.IsCompleted);

                    result.Add(new { 
                        lessonId = i, 
                        isUnlocked = previousLessonCompleted, 
                        isCompleted = currentLessonDone 
                    });
                }
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