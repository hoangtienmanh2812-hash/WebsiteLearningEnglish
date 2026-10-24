using EnglishLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261024200000_AddAdminSystemManagement")]
public sealed class AddAdminSystemManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'dbo.GamificationSettings', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GamificationSettings]
                (
                    [Id] int NOT NULL CONSTRAINT [PK_GamificationSettings] PRIMARY KEY,
                    [VocabularyLearnedXp] int NOT NULL
                        CONSTRAINT [DF_GamificationSettings_VocabularyLearnedXp] DEFAULT 10
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [dbo].[GamificationSettings] WHERE [Id] = 1)
            BEGIN
                INSERT INTO [dbo].[GamificationSettings] ([Id], [VocabularyLearnedXp]) VALUES (1, 10);
            END;

            IF OBJECT_ID(N'dbo.SystemLogs', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SystemLogs]
                (
                    [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_SystemLogs] PRIMARY KEY,
                    [OccurredAt] datetime2 NOT NULL,
                    [Level] nvarchar(16) NOT NULL,
                    [Category] nvarchar(200) NOT NULL,
                    [Message] nvarchar(2000) NOT NULL
                );
                CREATE INDEX [IX_SystemLogs_OccurredAt] ON [dbo].[SystemLogs] ([OccurredAt]);
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS [dbo].[SystemLogs];
            DROP TABLE IF EXISTS [dbo].[GamificationSettings];
            """);
    }
}
