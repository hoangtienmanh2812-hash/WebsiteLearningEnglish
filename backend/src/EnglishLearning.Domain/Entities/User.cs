namespace EnglishLearning.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int Exp { get; set; } = 0;
        public int StreakCount { get; set; } = 0;
        public DateTime? LastLoginDate { get; set; }
    }
}

