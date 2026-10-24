using EnglishLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261024220000_MoveGamificationToSystemSettings")]
public sealed class MoveGamificationToSystemSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DECLARE @VocabularyXp int = 10;

            IF OBJECT_ID(N'dbo.GamificationSettings', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.GamificationSettings', N'Id') IS NULL OR
                   COL_LENGTH(N'dbo.GamificationSettings', N'VocabularyLearnedXp') IS NULL
                BEGIN
                    THROW 51000, 'GamificationSettings has an unexpected schema; it was not changed.', 1;
                END;

                SELECT TOP (1) @VocabularyXp = [VocabularyLearnedXp]
                FROM [dbo].[GamificationSettings]
                WHERE [Id] = 1;
            END;

            IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SystemSettings]
                (
                    [Idsetting] int IDENTITY(1,1) NOT NULL
                        CONSTRAINT [PK_SystemSettings] PRIMARY KEY,
                    [SettingKey] nvarchar(100) NOT NULL,
                    [SettingValue] nvarchar(100) NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    [UsersId] uniqueidentifier NULL,
                    CONSTRAINT [FK_SystemSettings_Users_UsersId]
                        FOREIGN KEY ([UsersId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE SET NULL
                );
                CREATE UNIQUE INDEX [IX_SystemSettings_SettingKey]
                    ON [dbo].[SystemSettings] ([SettingKey]);
                CREATE INDEX [IX_SystemSettings_UsersId]
                    ON [dbo].[SystemSettings] ([UsersId]);
            END
            ELSE IF COL_LENGTH(N'dbo.SystemSettings', N'Idsetting') IS NULL OR
                    COL_LENGTH(N'dbo.SystemSettings', N'SettingKey') IS NULL OR
                    COL_LENGTH(N'dbo.SystemSettings', N'SettingValue') IS NULL OR
                    COL_LENGTH(N'dbo.SystemSettings', N'UpdatedAt') IS NULL OR
                    COL_LENGTH(N'dbo.SystemSettings', N'UsersId') IS NULL
            BEGIN
                THROW 51001, 'SystemSettings has an unexpected schema; it was not changed.', 1;
            END;

            IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemSettings] WHERE [SettingKey] = N'Gamification.VocabularyLearnedXp')
                INSERT INTO [dbo].[SystemSettings] ([SettingKey], [SettingValue], [UpdatedAt])
                VALUES (N'Gamification.VocabularyLearnedXp', CONVERT(nvarchar(100), @VocabularyXp), SYSUTCDATETIME());

            IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemSettings] WHERE [SettingKey] = N'Gamification.LessonCompletedXp')
                INSERT INTO [dbo].[SystemSettings] ([SettingKey], [SettingValue], [UpdatedAt])
                VALUES (N'Gamification.LessonCompletedXp', N'50', SYSUTCDATETIME());

            IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemSettings] WHERE [SettingKey] = N'Gamification.CorrectAnswerXp')
                INSERT INTO [dbo].[SystemSettings] ([SettingKey], [SettingValue], [UpdatedAt])
                VALUES (N'Gamification.CorrectAnswerXp', N'10', SYSUTCDATETIME());

            IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemSettings] WHERE [SettingKey] = N'Gamification.DailyStreakXp')
                INSERT INTO [dbo].[SystemSettings] ([SettingKey], [SettingValue], [UpdatedAt])
                VALUES (N'Gamification.DailyStreakXp', N'5', SYSUTCDATETIME());

            IF OBJECT_ID(N'dbo.UserQuestionXpAwards', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[UserQuestionXpAwards]
                (
                    [Id] bigint IDENTITY(1,1) NOT NULL
                        CONSTRAINT [PK_UserQuestionXpAwards] PRIMARY KEY,
                    [UserId] uniqueidentifier NOT NULL,
                    [LessonId] int NOT NULL,
                    [QuestionIndex] int NOT NULL,
                    [AwardedAt] datetime2 NOT NULL
                );
                CREATE UNIQUE INDEX [IX_UserQuestionXpAwards_UserId_LessonId_QuestionIndex]
                    ON [dbo].[UserQuestionXpAwards] ([UserId], [LessonId], [QuestionIndex]);
            END;

            IF OBJECT_ID(N'dbo.GamificationSettings', N'U') IS NOT NULL
                DROP TABLE [dbo].[GamificationSettings];
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS [dbo].[UserQuestionXpAwards];

            IF OBJECT_ID(N'dbo.GamificationSettings', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GamificationSettings]
                (
                    [Id] int NOT NULL CONSTRAINT [PK_GamificationSettings] PRIMARY KEY,
                    [VocabularyLearnedXp] int NOT NULL
                        CONSTRAINT [DF_GamificationSettings_VocabularyLearnedXp] DEFAULT 10
                );
            END;

            IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NOT NULL
            BEGIN
                DECLARE @VocabularyXp int = 10;
                SELECT TOP (1) @VocabularyXp = TRY_CONVERT(int, [SettingValue])
                FROM [dbo].[SystemSettings]
                WHERE [SettingKey] = N'Gamification.VocabularyLearnedXp';

                IF NOT EXISTS (SELECT 1 FROM [dbo].[GamificationSettings] WHERE [Id] = 1)
                    INSERT INTO [dbo].[GamificationSettings] ([Id], [VocabularyLearnedXp])
                    VALUES (1, COALESCE(@VocabularyXp, 10));
            END;

            """);
    }
}
