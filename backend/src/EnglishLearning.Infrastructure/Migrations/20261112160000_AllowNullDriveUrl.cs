using EnglishLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261112160000_AllowNullDriveUrl")]
public sealed class AllowNullDriveUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "DriveUrl",
            table: "Lessons",
            type: "nvarchar(max)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE [dbo].[Lessons] SET [DriveUrl] = N'' WHERE [DriveUrl] IS NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "DriveUrl",
            table: "Lessons",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: true);
    }
}
