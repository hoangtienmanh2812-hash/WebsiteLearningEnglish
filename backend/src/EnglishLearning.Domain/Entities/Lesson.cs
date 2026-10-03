namespace EnglishLearning.Domain.Entities;

public class Lesson
{
    public int Id { get; set; }           // LessonId (1, 2, 3...)
    public string Title { get; set; }     // Tên bài học
    public string DriveUrl { get; set; }  // Đường dẫn tải trực tiếp file JSON trên Google Drive
}