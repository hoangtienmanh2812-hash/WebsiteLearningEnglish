using System.IO.Compression;
using System.Security.Claims;
using EnglishLearning.Domain.Entities;
using EnglishLearning.Application.Features.Admin.DTOs;
using EnglishLearning.Application.Features.Admin.Interfaces;
using EnglishLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Dac;

namespace EnglishLearning.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/system")]
public sealed class AdminSystemController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSystemController> _logger;

    public AdminSystemController(
        ApplicationDbContext context,
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        ILogger<AdminSystemController> logger)
    {
        _context = context;
        _systemSettingsService = systemSettingsService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("gamification")]
    public async Task<IActionResult> GetGamificationSettings(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _systemSettingsService.GetGamificationSettingsAsync(cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Gamification settings are missing or invalid.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Cấu hình Gamification chưa được khởi tạo hoặc không hợp lệ. Hãy áp dụng migration database."
            });
        }
    }

    [HttpPut("gamification")]
    public async Task<IActionResult> UpdateGamificationSettings(
        [FromBody] UpdateGamificationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VocabularyLearnedXp is < 0 or > 10000 ||
            request.LessonCompletedXp is < 0 or > 10000 ||
            request.CorrectAnswerXp is < 0 or > 10000 ||
            request.DailyStreakXp is < 0 or > 10000)
        {
            return BadRequest(new { message = "XP thưởng từ 0 đến 10000." });
        }

        if (!Guid.TryParse(GetActorId(), out var adminUserId))
        {
            return Unauthorized(new { message = "Không xác định được tài khoản Admin hiện tại." });
        }

        await _systemSettingsService.UpdateGamificationSettingsAsync(request, adminUserId, cancellationToken);
        _context.SystemLogs.Add(CreateLog(
            "Information",
            "Gamification",
            $"Admin {GetActorId()} updated Gamification XP rules."));
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Đã cập nhật quy tắc XP."
        });
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetSystemLogs(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (take is < 1 or > 100)
        {
            return BadRequest(new { message = "Số lượng nhật ký phải từ 1 đến 100." });
        }

        var logs = await _context.SystemLogs
            .AsNoTracking()
            .OrderByDescending(log => log.OccurredAt)
            .Take(take)
            .Select(log => new SystemLogResponse(
                log.Id,
                log.OccurredAt,
                log.Level,
                log.Category,
                log.Message))
            .ToListAsync(cancellationToken);
        return Ok(logs);
    }

    [HttpPost("backup")]
    public async Task<IActionResult> CreateBackup(CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var directory = Path.Combine(Path.GetTempPath(), "EnglishLearningBackups", Guid.NewGuid().ToString("N"));
        var bacpacPath = Path.Combine(directory, "database.bacpac");
        var archivePath = $"{directory}.zip";
        var responseOwnsTemporaryFiles = false;
        try
        {
            Directory.CreateDirectory(directory);
            var databaseName = connection.Database;
            var dacServices = new DacServices(connection.ConnectionString);
            await Task.Run(
                () => dacServices.ExportBacpac(
                    bacpacPath,
                    databaseName,
                    DacSchemaModelStorageType.Memory,
                    null,
                    cancellationToken),
                cancellationToken);
            ZipFile.CreateFromDirectory(directory, archivePath, CompressionLevel.NoCompression, false);

            _context.SystemLogs.Add(CreateLog("Information", "DatabaseBackup", $"Admin {GetActorId()} created a database backup."));
            await _context.SaveChangesAsync(cancellationToken);
            var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Response.OnCompleted(() =>
            {
                DeleteBackupFiles(directory, archivePath);
                return Task.CompletedTask;
            });
            Response.Headers.CacheControl = "no-store";
            responseOwnsTemporaryFiles = true;
            return File(stream, "application/zip", $"database-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
        }
        catch (DacServicesException exception)
        {
            _logger.LogError(exception, "Could not export database {DatabaseName} to a BACPAC.", connection.Database);
            await TryLogBackupFailureAsync("Database export failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Không thể xuất database. Hãy kiểm tra quyền đọc schema và dữ liệu của tài khoản kết nối SQL Server."
            });
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "Could not create the downloadable database backup archive.");
            await TryLogBackupFailureAsync("Could not create the downloadable archive.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Không thể đóng gói bản sao lưu. Hãy kiểm tra dung lượng ổ đĩa và quyền ghi thư mục tạm của backend."
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogError(exception, "Backend cannot access the temporary database backup directory.");
            await TryLogBackupFailureAsync("Backend cannot access its temporary backup directory.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Backend không có quyền ghi thư mục tạm để tạo tệp backup."
            });
        }
        finally
        {
            if (!responseOwnsTemporaryFiles)
            {
                DeleteBackupFiles(directory, archivePath);
            }
        }
    }

    private async Task TryLogBackupFailureAsync(string detail)
    {
        try
        {
            _context.SystemLogs.Add(CreateLog("Error", "DatabaseBackup", detail));
            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception exception) when (
            exception is DbUpdateException or IOException or InvalidOperationException)
        {
            _logger.LogError(exception, "Could not persist the database backup failure log.");
        }
    }

    private SystemLog CreateLog(string level, string category, string message) => new()
    {
        OccurredAt = DateTime.UtcNow,
        Level = level,
        Category = category,
        Message = message
    };

    private string GetActorId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub") ??
        "unknown";

    private void DeleteBackupFiles(string directory, string archivePath)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException exception)
        {
            _logger.LogWarning("Could not remove temporary backup files ({ExceptionType}).", exception.GetType().Name);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning("Could not remove temporary backup files ({ExceptionType}).", exception.GetType().Name);
        }
        try
        {
            System.IO.File.Delete(archivePath);
        }
        catch (IOException exception)
        {
            _logger.LogWarning("Could not remove temporary backup archive ({ExceptionType}).", exception.GetType().Name);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning("Could not remove temporary backup archive ({ExceptionType}).", exception.GetType().Name);
        }
    }
}

public sealed record SystemLogResponse(long Id, DateTime OccurredAt, string Level, string Category, string Message);
