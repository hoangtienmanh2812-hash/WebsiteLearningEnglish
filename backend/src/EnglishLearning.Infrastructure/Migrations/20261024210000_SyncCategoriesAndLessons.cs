using EnglishLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261024210000_SyncCategoriesAndLessons")]
public sealed class SyncCategoriesAndLessons : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Categories]
                (
                    [Idcategories] int IDENTITY(1,1) NOT NULL,
                    [CategoryName] nvarchar(100) NOT NULL,
                    [Description] nvarchar(max) NULL,
                    CONSTRAINT [PK_Categories] PRIMARY KEY ([Idcategories])
                );
            END
            ELSE
            BEGIN
                IF COL_LENGTH(N'dbo.Categories', N'Idcategories') IS NULL
                    THROW 51000, 'Existing dbo.Categories table is missing Idcategories; migration stopped without changing existing data.', 1;

                IF COL_LENGTH(N'dbo.Categories', N'CategoryName') IS NULL
                    ALTER TABLE [dbo].[Categories]
                    ADD [CategoryName] nvarchar(100) NOT NULL
                        CONSTRAINT [DF_Categories_CategoryName] DEFAULT N'';

                IF COL_LENGTH(N'dbo.Categories', N'Description') IS NULL
                    ALTER TABLE [dbo].[Categories] ADD [Description] nvarchar(max) NULL;
            END;

            IF COL_LENGTH(N'dbo.Lessons', N'CategoriesId') IS NULL
                ALTER TABLE [dbo].[Lessons] ADD [CategoriesId] int NULL;

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.key_constraints kc
                INNER JOIN sys.index_columns ic
                    ON ic.object_id = kc.parent_object_id
                    AND ic.index_id = kc.unique_index_id
                INNER JOIN sys.columns c
                    ON c.object_id = ic.object_id
                    AND c.column_id = ic.column_id
                WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Categories')
                    AND kc.type = N'PK'
                    AND c.name = N'Idcategories'
                    AND ic.key_ordinal = 1
                    AND NOT EXISTS
                    (
                        SELECT 1 FROM sys.index_columns extra
                        WHERE extra.object_id = ic.object_id
                            AND extra.index_id = ic.index_id
                            AND extra.key_ordinal > 1
                    )
            )
            BEGIN
                IF EXISTS
                (
                    SELECT [Idcategories]
                    FROM [dbo].[Categories]
                    GROUP BY [Idcategories]
                    HAVING [Idcategories] IS NULL OR COUNT(*) > 1
                )
                    THROW 51001, 'dbo.Categories.Idcategories contains duplicate values; migration stopped before adding its primary key.', 1;

                ALTER TABLE [dbo].[Categories]
                ADD CONSTRAINT [PK_Categories] PRIMARY KEY ([Idcategories]);
            END;

            IF NOT EXISTS
            (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.Lessons')
                    AND name = N'IX_Lessons_CategoriesId'
            )
                CREATE INDEX [IX_Lessons_CategoriesId] ON [dbo].[Lessons] ([CategoriesId]);

            IF NOT EXISTS
            (
                SELECT 1 FROM sys.foreign_keys
                WHERE parent_object_id = OBJECT_ID(N'dbo.Lessons')
                    AND referenced_object_id = OBJECT_ID(N'dbo.Categories')
            )
            BEGIN
                IF EXISTS
                (
                    SELECT 1
                    FROM [dbo].[Lessons] lesson
                    LEFT JOIN [dbo].[Categories] category
                        ON category.[Idcategories] = lesson.[CategoriesId]
                    WHERE lesson.[CategoriesId] IS NOT NULL
                        AND category.[Idcategories] IS NULL
                )
                    THROW 51002, 'dbo.Lessons contains CategoriesId values that do not exist in dbo.Categories; migration stopped without changing lesson rows.', 1;

                ALTER TABLE [dbo].[Lessons] WITH CHECK
                ADD CONSTRAINT [FK_Lessons_Categories_CategoriesId]
                    FOREIGN KEY ([CategoriesId]) REFERENCES [dbo].[Categories] ([Idcategories])
                    ON DELETE SET NULL;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
