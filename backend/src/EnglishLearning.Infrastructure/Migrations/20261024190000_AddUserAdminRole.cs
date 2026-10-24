using EnglishLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261024190000_AddUserAdminRole")]
public sealed class AddUserAdminRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'dbo.Users', N'IsAdmin') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Users]
                ADD [IsAdmin] bit NOT NULL
                    CONSTRAINT [DF_Users_IsAdmin] DEFAULT 0;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
