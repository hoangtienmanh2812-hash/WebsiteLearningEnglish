using EnglishLearning.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace EnglishLearning.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<UserProgress> UserProgresses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Category> Categories { get; set; }
        // 👉 Thêm DbSet cho bảng Questions vào đây
        public DbSet<Question> Questions { get; set; }
        public DbSet<DailyVocabPack> DailyVocabPacks { get; set; }
        public DbSet<UserLearnedVocab> UserLearnedVocabs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<UserQuestionXpAward> UserQuestionXpAwards { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IsAdmin).HasDefaultValue(false);
            });

            // 👉 Thêm cấu hình Fluent API cho bảng Questions (tuỳ chọn nhưng giúp DB chuẩn chỉnh hơn)
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QuestionText).IsRequired();
                entity.Property(e => e.CorrectAnswer).IsRequired();
            });
            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.HasIndex(lesson => lesson.CategoriesId);
                entity.Property(e => e.LessonType)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("lesson");
                entity.HasOne(lesson => lesson.Category)
                    .WithMany(category => category.Lessons)
                    .HasForeignKey(lesson => lesson.CategoriesId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(category => category.Idcategories);
                entity.Property(category => category.Idcategories).ValueGeneratedOnAdd();
                entity.Property(category => category.CategoryName)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(category => category.Description);
            });

            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.HasKey(setting => setting.Idsetting);
                entity.Property(setting => setting.SettingKey).IsRequired().HasMaxLength(100);
                entity.Property(setting => setting.SettingValue).IsRequired().HasMaxLength(100);
                entity.Property(setting => setting.UpdatedAt).IsRequired();
                entity.HasIndex(setting => setting.SettingKey).IsUnique();
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(setting => setting.UsersId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UserQuestionXpAward>(entity =>
            {
                entity.HasKey(award => award.Id);
                entity.HasIndex(award => new { award.UserId, award.LessonId, award.QuestionIndex })
                    .IsUnique();
            });

            modelBuilder.Entity<SystemLog>(entity =>
            {
                entity.HasKey(log => log.Id);
                entity.Property(log => log.Level).IsRequired().HasMaxLength(16);
                entity.Property(log => log.Category).IsRequired().HasMaxLength(200);
                entity.Property(log => log.Message).IsRequired().HasMaxLength(2000);
                entity.HasIndex(log => log.OccurredAt);
            });
        }
    }
}
