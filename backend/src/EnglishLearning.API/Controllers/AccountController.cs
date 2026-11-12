using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using EnglishLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.API.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "Token không chứa ID người dùng hợp lệ." });
        }

        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản." });
        }

        var completedLessonIds = await _context.UserProgresses
            .AsNoTracking()
            .Where(progress => progress.UserId == userId && progress.IsCompleted)
            .Select(progress => progress.LessonId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var lessonCount = await _context.Lessons
            .AsNoTracking()
            .CountAsync(lesson => lesson.LessonType.ToLower() == "lesson", cancellationToken);

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.CreatedAt,
            user.Exp,
            user.StreakCount,
            CompletedLessons = completedLessonIds.Count,
            TotalLessons = lessonCount
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "Token không chứa ID người dùng hợp lệ." });
        }

        var fullName = request.FullName.Trim();
        if (fullName.Length is 0 or > 100)
        {
            return BadRequest(new { message = "Tên hiển thị phải từ 1 đến 100 ký tự." });
        }

        var user = await _context.Users
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản." });
        }

        user.FullName = fullName;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { user.FullName });
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "Token không chứa ID người dùng hợp lệ." });
        }

        if (string.IsNullOrEmpty(request.CurrentPassword) ||
            string.IsNullOrEmpty(request.NewPassword) ||
            request.NewPassword.Length < 8 ||
            Encoding.UTF8.GetByteCount(request.NewPassword) > 72)
        {
            return BadRequest(new
            {
                message = "Vui lòng nhập mật khẩu hiện tại và mật khẩu mới từ 8 ký tự, tối đa 72 byte UTF-8."
            });
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            return BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại." });
        }

        var user = await _context.Users
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản." });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã cập nhật mật khẩu." });
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(userId, out var parsedId) ? parsedId : null;
    }

    public sealed record UpdateProfileRequest(
        [Required, StringLength(100, MinimumLength = 1)] string FullName);

    public sealed record ChangePasswordRequest(
        [Required] string CurrentPassword,
        [Required, StringLength(72, MinimumLength = 8)] string NewPassword);
}
