namespace EnglishLearning.Domain.Entities;

public sealed class UserQuestionXpAward
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public int LessonId { get; set; }
    public int QuestionIndex { get; set; }
    public DateTime AwardedAt { get; set; }
}
