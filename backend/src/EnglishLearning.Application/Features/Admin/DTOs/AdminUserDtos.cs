namespace EnglishLearning.Application.Features.Admin.DTOs;

public sealed class AdminUserListItemDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public int Exp { get; init; }
    public int StreakCount { get; init; }
    public int CompletedLessons { get; init; }
    public int TotalLessons { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class AdminUserProgressItemDto
{
    public int LessonId { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
}

public sealed class AdminUserDetailsDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public int Exp { get; init; }
    public int StreakCount { get; init; }
    public DateTime? LastLoginDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public int CompletedLessons { get; init; }
    public int TotalLessons { get; init; }
    public IReadOnlyList<AdminUserProgressItemDto> Progress { get; init; } = [];
}

public sealed class UpdateAdminUserRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public string? Password { get; init; }
}
