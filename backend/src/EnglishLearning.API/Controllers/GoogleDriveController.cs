using EnglishLearning.Infrastructure.GoogleDrive;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.API.Controllers;

[ApiController]
[Route("api/google-drive")]
public class GoogleDriveController : ControllerBase
{
    private readonly GoogleDriveService _driveService;
    private readonly IConfiguration _configuration;

    public GoogleDriveController(
        GoogleDriveService driveService,
        IConfiguration configuration)
    {
        _driveService = driveService;
        _configuration = configuration;
    }

    [HttpGet("files")]
    public async Task<IActionResult> GetFiles()
    {
        var folderId =
            _configuration["GoogleDrive:RootFolderId"];

        if (string.IsNullOrWhiteSpace(folderId))
            return BadRequest("Chưa cấu hình RootFolderId.");

        try
        {
            var files = await _driveService.GetFilesAsync(folderId);

            return Ok(new
            {
                message = "Kết nối Google Drive thành công!",
                count = files.Count,
                files = files.Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    type = f.MimeType
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Không đọc được dữ liệu Google Drive.",
                error = ex.Message
            });
        }
    }
}