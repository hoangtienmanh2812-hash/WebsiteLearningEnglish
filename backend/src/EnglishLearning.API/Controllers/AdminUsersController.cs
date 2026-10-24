using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using EnglishLearning.Application.Features.Admin.DTOs;
using EnglishLearning.Application.Features.Auth.Interfaces;
using EnglishLearning.Domain.Entities;
using EnglishLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtProvider _jwtProvider;

    public AdminUsersController(ApplicationDbContext context, IJwtProvider jwtProvider)
    {
        _context = context;
        _jwtProvider = jwtProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(role) &&
            role.Trim().ToLowerInvariant() is not ("admin" or "user" or "all"))
        {
            return BadRequest(new { message = "Vai trò lọc phải là 'admin', 'user' hoặc 'all'." });
        }

        var usersQuery = _context.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            usersQuery = usersQuery.Where(user =>
                EF.Functions.Like(user.FullName, pattern) ||
                EF.Functions.Like(user.Email, pattern));
        }

        var users = await usersQuery
            .OrderBy(user => user.FullName)
            .Select(user => new UserListProjection(
                user.Id,
                user.FullName,
                user.Email,
                user.IsAdmin,
                user.Exp,
                user.StreakCount,
                user.CreatedAt))
            .ToListAsync(cancellationToken);
        var lessonCount = await _context.Lessons
            .AsNoTracking()
            .CountAsync(lesson => lesson.LessonType.ToLower() == "lesson", cancellationToken);
        var completedByUser = await _context.UserProgresses
            .AsNoTracking()
            .Where(progress => progress.IsCompleted)
            .GroupBy(progress => progress.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Count = group.Select(progress => progress.LessonId).Distinct().Count()
            })
            .ToDictionaryAsync(result => result.UserId, result => result.Count, cancellationToken);

        var result = users
            .Select(user =>
            {
                var isAdmin = IsAdmin(user.Email, user.IsAdmin);
                return new AdminUserListItemDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsAdmin = isAdmin,
                    Exp = user.Exp,
                    StreakCount = user.StreakCount,
                    CompletedLessons = completedByUser.GetValueOrDefault(user.Id),
                    TotalLessons = lessonCount,
                    CreatedAt = user.CreatedAt
                };
            })
            .Where(user => role?.Trim().ToLowerInvariant() switch
            {
                "admin" => user.IsAdmin,
                "user" => !user.IsAdmin,
                _ => true
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        var lessons = await _context.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.LessonType.ToLower() == "lesson")
            .OrderBy(lesson => lesson.Id)
            .Select(lesson => new { lesson.Id, lesson.Title })
            .ToListAsync(cancellationToken);
        var completedLessonIds = (await _context.UserProgresses
            .AsNoTracking()
            .Where(progress => progress.UserId == id && progress.IsCompleted)
            .Select(progress => progress.LessonId)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var progressItems = lessons
            .Select(lesson => new AdminUserProgressItemDto
            {
                LessonId = lesson.Id,
                Title = lesson.Title,
                IsCompleted = completedLessonIds.Contains(lesson.Id)
            })
            .ToList();

        return Ok(new AdminUserDetailsDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsAdmin = IsAdmin(user.Email, user.IsAdmin),
            Exp = user.Exp,
            StreakCount = user.StreakCount,
            LastLoginDate = user.LastLoginDate,
            CreatedAt = user.CreatedAt,
            CompletedLessons = completedLessonIds.Count,
            TotalLessons = lessons.Count,
            Progress = progressItems
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Token không chứa ID người dùng hợp lệ." });
        }

        var fullName = request.FullName.Trim();
        var email = request.Email.Trim();
        if (fullName.Length is 0 or > 100)
        {
            return BadRequest(new { message = "Tên người dùng phải từ 1 đến 100 ký tự." });
        }

        if (email.Length is 0 or > 256 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            return BadRequest(new { message = "Địa chỉ email không hợp lệ." });
        }

        if (!string.IsNullOrEmpty(request.Password) &&
            (request.Password.Length < 8 || Encoding.UTF8.GetByteCount(request.Password) > 72))
        {
            return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 8 ký tự và tối đa 72 byte UTF-8." });
        }

        var user = await _context.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        if (currentUserId == id &&
            IsAdmin(user.Email, user.IsAdmin) &&
            !request.IsAdmin)
        {
            return Conflict(new { message = "Admin không thể tự hạ quyền tài khoản đang đăng nhập." });
        }

        var duplicateEmail = await _context.Users
            .AnyAsync(item => item.Id != id && item.Email == email, cancellationToken);
        if (duplicateEmail)
        {
            return Conflict(new { message = "Email này đã được sử dụng bởi tài khoản khác." });
        }

        user.FullName = fullName;
        user.Email = email;
        user.IsAdmin = request.IsAdmin;
        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Không thể cập nhật người dùng. Hãy kiểm tra email đã tồn tại chưa." });
        }

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            IsAdmin = IsAdmin(user.Email, user.IsAdmin),
            user.Exp,
            user.StreakCount
        });
    }

    private bool IsAdmin(string email, bool isAdmin) =>
        _jwtProvider.IsAdmin(new User { Email = email, IsAdmin = isAdmin });

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(userId, out var parsedId) ? parsedId : null;
    }

    private sealed record UserListProjection(
        Guid Id,
        string FullName,
        string Email,
        bool IsAdmin,
        int Exp,
        int StreakCount,
        DateTime CreatedAt);
}
