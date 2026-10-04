namespace EnglishLearning.Application.DTOs
{
    // DTO cho Bảng xếp hạng
    public class LeaderboardUserDto
    {
        public Guid Id { get; set; } // Dùng Guid cho khớp với Users.Id trong CSDL
        public string FullName { get; set; } = string.Empty;
        public int Exp { get; set; }
        public int StreakCount { get; set; }
    }

    // Request body khi đánh dấu thuộc từ vựng
    public class MarkVocabLearnedRequest
    {
        public Guid UserId { get; set; } // Dùng Guid
        public int DayNumber { get; set; }
        public int VocabId { get; set; }
    }
}