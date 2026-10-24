namespace EnglishLearning.Application.Features.Admin.DTOs;

public sealed class AdminFolderDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}

public sealed class AdminLessonDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string LessonType { get; init; } = "lesson";
    public string FolderId { get; init; } = string.Empty;
    public string FolderPath { get; init; } = string.Empty;
    public DateTimeOffset? ModifiedTime { get; init; }
}

public sealed class AdminLessonContentDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string LessonType { get; init; } = "lesson";
    public string FolderId { get; init; } = string.Empty;
    public string FolderPath { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public sealed class SaveAdminLessonRequest
{
    public string FolderId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string LessonType { get; init; } = "lesson";
    public string Content { get; init; } = string.Empty;
}

public sealed class UpdateAdminLessonRequest
{
    public string Title { get; init; } = string.Empty;
    public string LessonType { get; init; } = "lesson";
    public string Content { get; init; } = string.Empty;
}
