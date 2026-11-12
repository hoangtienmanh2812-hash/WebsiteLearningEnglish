using EnglishLearning.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EnglishLearning.Infrastructure.Data;

public static class LessonIdGenerator
{
    public static async Task AssignIdIfNeededAsync(
        ApplicationDbContext context,
        Lesson lesson,
        CancellationToken cancellationToken = default)
    {
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Lesson IDs must be generated inside a database transaction.");
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText =
            """
            DECLARE @nextId int = NULL;
            IF COLUMNPROPERTY(OBJECT_ID(N'dbo.Lessons'), N'Id', 'IsIdentity') <> 1
            BEGIN
                DECLARE @lockResult int;
                EXEC @lockResult = sys.sp_getapplock
                    @Resource = N'EnglishLearning.Lessons.Id',
                    @LockMode = N'Exclusive',
                    @LockOwner = N'Transaction',
                    @LockTimeout = 10000;
                IF @lockResult < 0
                    THROW 51003, 'Could not acquire the lesson ID generation lock.', 1;

                SELECT @nextId = ISNULL(MAX([Id]), 0) + 1
                FROM [dbo].[Lessons] WITH (UPDLOCK, HOLDLOCK);
            END;
            SELECT @nextId;
            """;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not null and not DBNull)
        {
            lesson.Id = Convert.ToInt32(result);
        }
    }
}
