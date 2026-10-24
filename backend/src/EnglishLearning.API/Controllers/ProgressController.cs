using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EnglishLearning.Infrastructure.Data;
using EnglishLearning.Application.Features.Admin.Interfaces;
using EnglishLearning.Domain.Entities;
using System.Security.Claims;

namespace EnglishLearning.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProgressController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISystemSettingsService _systemSettingsService;

    public ProgressController(ApplicationDbContext context, ISystemSettingsService systemSettingsService)
    {
        _context = context;
        _systemSettingsService = systemSettingsService;
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

    [Authorize]
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteLesson([FromBody] CompleteLessonDto dto)
    {
        try
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdValue, out var authenticatedUserId))
            {
                return Unauthorized(new { message = "Không xác định được tài khoản hiện tại." });
            }

            if (dto == null || dto.LessonId <= 0 ||
                (dto.UserId != Guid.Empty && dto.UserId != authenticatedUserId))
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
                .FirstOrDefaultAsync(p => p.UserId == authenticatedUserId && p.LessonId == dto.LessonId);
            var xpReward = 0;

            if (progress?.IsCompleted != true)
            {
                var user = await _context.Users.FindAsync(authenticatedUserId);
                if (user is null)
                {
                    return NotFound(new { message = "Không tìm thấy người dùng." });
                }

                xpReward = await _systemSettingsService.GetXpRewardAsync(
                    SystemSettingsService.LessonCompletedXpKey);
                user.Exp += xpReward;

                if (progress is null)
                {
                    _context.UserProgresses.Add(new Domain.Entities.UserProgress
                    {
                        UserId = authenticatedUserId,
                        LessonId = dto.LessonId,
                        IsCompleted = true
                    });
                }
                else
                {
                    progress.IsCompleted = true;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã hoàn thành bài học!", xpReward });
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
