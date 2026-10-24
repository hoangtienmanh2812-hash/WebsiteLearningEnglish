using EnglishLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261024110000_AddLessonTypeToLessons")]
public sealed class AddLessonTypeToLessons : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'dbo.Lessons', N'LessonType') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Lessons]
                ADD [LessonType] nvarchar(50) NOT NULL
                    CONSTRAINT [DF_Lessons_LessonType] DEFAULT N'lesson';
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
