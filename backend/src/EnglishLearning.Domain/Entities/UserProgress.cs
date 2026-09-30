namespace EnglishLearning.Domain.Entities;

public class UserProgress
{
    public int Id { get; set; }
    public Guid UserId { get; set; }       // ID của người dùng
    public int LessonId { get; set; }      // ID bài học (Bài 1, Bài 2,...)
    public bool IsCompleted { get; set; }  // Đã hoàn thành hay chưa
}