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
        // 👉 Thêm DbSet cho bảng Questions vào đây
        public DbSet<Question> Questions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            });

            // 👉 Thêm cấu hình Fluent API cho bảng Questions (tuỳ chọn nhưng giúp DB chuẩn chỉnh hơn)
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QuestionText).IsRequired();
                entity.Property(e => e.CorrectAnswer).IsRequired();
            });
        }
    }
}