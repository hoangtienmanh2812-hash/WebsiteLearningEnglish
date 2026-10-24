namespace EnglishLearning.Domain.Entities;

public class Lesson
{
    public int Id { get; set; }           // LessonId (1, 2, 3...)
    public string Title { get; set; } = string.Empty;     // Tên bài học
    public string DriveUrl { get; set; } = string.Empty;  // Đường dẫn tải trực tiếp file JSON trên Google Drive
    public string LessonType { get; set; } = "lesson";
    public int? CategoriesId { get; set; }
    public Category? Category { get; set; }
}
