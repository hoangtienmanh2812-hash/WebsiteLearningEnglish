using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using EnglishLearning.Application.Features.Admin.DTOs;
using EnglishLearning.Application.Features.Admin.Interfaces;
using EnglishLearning.Domain.Entities;
using EnglishLearning.Infrastructure.Data;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace EnglishLearning.Infrastructure.GoogleDrive;

public class GoogleDriveService : IAdminLessonService, IDisposable
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly SemaphoreSlim _driveInitializationLock = new(1, 1);
    private DriveService? _drive;

    public GoogleDriveService(IConfiguration config, ApplicationDbContext context)
    {
        _configuration = config;
        _context = context;
    }

    public void Dispose()
    {
        _drive?.Dispose();
        _driveInitializationLock.Dispose();
    }

    public async Task<List<DriveFile>> GetFilesAsync(string folderId, CancellationToken cancellationToken = default)
    {
        var drive = await GetDriveServiceAsync(cancellationToken);
        var files = new List<DriveFile>();
        string? pageToken = null;
        do
        {
            var request = drive.Files.List();
            request.Q = $"'{folderId}' in parents and trashed = false";
            request.Fields = "nextPageToken,files(id,name,mimeType,parents,modifiedTime)";
            request.PageSize = 1000;
            request.PageToken = pageToken;
            request.SupportsAllDrives = true;
            request.IncludeItemsFromAllDrives = true;

            var result = await request.ExecuteAsync(cancellationToken);
            if (result.Files is not null)
            {
                files.AddRange(result.Files);
            }

            pageToken = result.NextPageToken;
        } while (pageToken is not null);

        return files;
    }

    public Task<string> DownloadLessonContentAsync(
        string driveUrl,
        CancellationToken cancellationToken = default)
    {
        var fileId = ExtractDriveFileId(driveUrl);
        if (fileId is null)
        {
            throw new InvalidOperationException("DriveUrl của bài học không chứa ID file Google Drive hợp lệ.");
        }

        return DownloadContentAsync(fileId, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminFolderDto>> GetFoldersAsync(CancellationToken cancellationToken = default)
    {
        var folders = await GetManagedFoldersAsync(cancellationToken);
        return folders
            .Select(folder => new AdminFolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                Path = folder.Path
            })
            .OrderBy(folder => folder.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<AdminLessonDto>?> GetLessonsAsync(
        string? folderId,
        CancellationToken cancellationToken = default)
    {
        var folders = await GetManagedFoldersAsync(cancellationToken);
        if (folderId is not null && !folders.Any(folder => folder.Id == folderId))
        {
            return null;
        }

        var selectedFolder = folderId is null ? null : folders.Single(folder => folder.Id == folderId);
        var foldersById = folders.ToDictionary(folder => folder.Id);
        var driveFiles = folders
            .SelectMany(folder => folder.Files
                .Where(IsJsonFile)
                .Select(file => (File: file, Folder: folder)))
            .ToDictionary(item => item.File.Id, item => item);
        var lessons = await _context.Lessons
            .AsNoTracking()
            .OrderBy(lesson => lesson.Id)
            .ToListAsync(cancellationToken);

        return lessons
            .Select(lesson =>
            {
                var fileId = ExtractDriveFileId(lesson.DriveUrl);
                (DriveFile File, ManagedFolder Folder)? driveFile =
                    fileId is not null && driveFiles.TryGetValue(fileId, out var found)
                        ? found
                        : null;
                return new { Lesson = lesson, DriveFile = driveFile };
            })
            .Where(item => selectedFolder is null ||
                (item.DriveFile.HasValue &&
                 IsWithin(item.DriveFile.Value.Folder, selectedFolder.Id, foldersById)))
            .Select(item => new AdminLessonDto
            {
                Id = item.Lesson.Id,
                Title = item.Lesson.Title,
                LessonType = item.Lesson.LessonType,
                FolderId = item.DriveFile?.Folder.Id ?? string.Empty,
                FolderPath = item.DriveFile?.Folder.Path ?? string.Empty,
                ModifiedTime = item.DriveFile?.File.ModifiedTimeDateTimeOffset
            })
            .OrderBy(lesson => lesson.FolderPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(lesson => lesson.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<AdminLessonContentDto?> GetLessonAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons.FindAsync(new object?[] { id }, cancellationToken);
        if (lesson is null)
        {
            return null;
        }

        var fileId = ExtractDriveFileId(lesson.DriveUrl);
        if (fileId is null)
        {
            throw new InvalidOperationException("DriveUrl của bài học không chứa ID file Google Drive hợp lệ.");
        }

        var managedFile = await GetManagedFileAsync(fileId, cancellationToken);
        if (managedFile is null)
        {
            return null;
        }

        var content = await DownloadContentAsync(fileId, cancellationToken);
        return ToContentDto(lesson, managedFile.Folder, content);
    }

    public async Task<AdminLessonContentDto> CreateLessonAsync(
        SaveAdminLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        var folders = await GetManagedFoldersAsync(cancellationToken);
        var folder = folders.SingleOrDefault(item => item.Id == request.FolderId)
            ?? throw new ArgumentException("Thư mục đã chọn không thuộc phạm vi quản lý.");
        var title = NormalizeTitle(request.Title);
        var lessonType = NormalizeLessonType(request.LessonType);
        var name = NormalizeFileName(title);
        ValidateJson(request.Content);

        if (folder.Files.Any(file => string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Đã tồn tại file cùng tên trong thư mục này.");
        }

        var driveFile = new DriveFile
        {
            Name = name,
            MimeType = "application/json",
            Parents = new List<string> { folder.Id }
        };
        var uploaded = await UploadContentAsync(driveFile, request.Content, cancellationToken);
        var lesson = new Lesson
        {
            Title = title,
            LessonType = lessonType,
            DriveUrl = BuildDriveUrl(uploaded.Id)
        };

        _context.Lessons.Add(lesson);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception databaseException)
        {
            try
            {
                var drive = await GetDriveServiceAsync(CancellationToken.None);
                var deleteRequest = drive.Files.Delete(uploaded.Id);
                deleteRequest.SupportsAllDrives = true;
                await deleteRequest.ExecuteAsync(CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                throw new InvalidOperationException(
                    "Không lưu được bài học vào SQL Server và không thể xóa file JSON vừa tạo trên Google Drive.",
                    new AggregateException(databaseException, cleanupException));
            }

            throw;
        }

        return ToContentDto(lesson, folder, request.Content);
    }

    public async Task<AdminLessonContentDto?> UpdateLessonAsync(
        int id,
        UpdateAdminLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons.FindAsync(new object?[] { id }, cancellationToken);
        if (lesson is null)
        {
            return null;
        }

        var fileId = ExtractDriveFileId(lesson.DriveUrl);
        if (fileId is null)
        {
            throw new InvalidOperationException("DriveUrl của bài học không chứa ID file Google Drive hợp lệ.");
        }

        var managedFile = await GetManagedFileAsync(fileId, cancellationToken);
        if (managedFile is null)
        {
            return null;
        }

        var title = NormalizeTitle(request.Title);
        var lessonType = NormalizeLessonType(request.LessonType);
        var name = NormalizeFileName(title);
        ValidateJson(request.Content);
        if (managedFile.Folder.Files.Any(file =>
                file.Id != fileId &&
                string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Đã tồn tại file cùng tên trong thư mục này.");
        }

        var previousContent = await DownloadContentAsync(fileId, cancellationToken);
        var previousName = managedFile.File.Name;
        await UploadContentAsync(
            new DriveFile
            {
                Name = name,
                MimeType = "application/json"
            },
            request.Content,
            cancellationToken,
            fileId);
        lesson.Title = title;
        lesson.LessonType = lessonType;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception databaseException)
        {
            try
            {
                await UploadContentAsync(
                    new DriveFile
                    {
                        Name = previousName,
                        MimeType = "application/json"
                    },
                    previousContent,
                    CancellationToken.None,
                    fileId);
            }
            catch (Exception rollbackException)
            {
                throw new InvalidOperationException(
                    "Không lưu được metadata vào SQL Server và không thể khôi phục nội dung cũ trên Google Drive.",
                    new AggregateException(databaseException, rollbackException));
            }

            throw;
        }

        return ToContentDto(lesson, managedFile.Folder, request.Content);
    }

    public async Task<bool> DeleteLessonAsync(int id, CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons.FindAsync(new object?[] { id }, cancellationToken);
        if (lesson is null)
        {
            return false;
        }

        var fileId = ExtractDriveFileId(lesson.DriveUrl);
        if (fileId is null)
        {
            throw new InvalidOperationException("DriveUrl của bài học không chứa ID file Google Drive hợp lệ.");
        }

        var managedFile = await GetManagedFileAsync(fileId, cancellationToken);
        if (managedFile is null)
        {
            return false;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync(cancellationToken);
        var drive = await GetDriveServiceAsync(cancellationToken);
        var deleteRequest = drive.Files.Delete(fileId);
        deleteRequest.SupportsAllDrives = true;
        await deleteRequest.ExecuteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<List<ManagedFolder>> GetManagedFoldersAsync(CancellationToken cancellationToken)
    {
        var rootFolderId = _configuration["GoogleDrive:RootFolderId"];
        if (string.IsNullOrWhiteSpace(rootFolderId))
        {
            throw new InvalidOperationException("Chưa cấu hình GoogleDrive:RootFolderId.");
        }

        var folders = new List<ManagedFolder>();
        await AddChildFoldersAsync(rootFolderId, string.Empty, null, folders, 0, cancellationToken);
        return folders;
    }

    private async Task AddChildFoldersAsync(
        string parentId,
        string parentPath,
        ManagedFolder? parentFolder,
        List<ManagedFolder> folders,
        int depth,
        CancellationToken cancellationToken)
    {
        if (depth >= 8)
        {
            throw new InvalidOperationException("Cấu trúc thư mục Google Drive vượt quá giới hạn cho phép.");
        }

        var children = await GetFilesAsync(parentId, cancellationToken);
        if (parentFolder is not null)
        {
            parentFolder.Files.AddRange(children.Where(file => file.MimeType != FolderMimeType));
        }

        foreach (var child in children.Where(file => file.MimeType == FolderMimeType))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = parentPath.Length == 0 ? child.Name : $"{parentPath}/{child.Name}";
            var folder = new ManagedFolder(child.Id, child.Name, path, parentId);
            folders.Add(folder);
            await AddChildFoldersAsync(child.Id, path, folder, folders, depth + 1, cancellationToken);
        }
    }

    private static bool IsWithin(
        ManagedFolder folder,
        string selectedFolderId,
        IReadOnlyDictionary<string, ManagedFolder> foldersById)
    {
        var current = folder;
        while (true)
        {
            if (current.Id == selectedFolderId)
            {
                return true;
            }

            if (!foldersById.TryGetValue(current.ParentId, out var parent))
            {
                return false;
            }

            current = parent;
        }
    }

    private async Task<ManagedFile?> GetManagedFileAsync(string fileId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return null;
        }

        var folders = await GetManagedFoldersAsync(cancellationToken);
        foreach (var folder in folders)
        {
            var file = folder.Files.FirstOrDefault(item => item.Id == fileId && IsJsonFile(item));
            if (file is not null)
            {
                return new ManagedFile(file, folder);
            }
        }

        return null;
    }

    private async Task<string> DownloadContentAsync(string fileId, CancellationToken cancellationToken)
    {
        var drive = await GetDriveServiceAsync(cancellationToken);
        using var stream = new MemoryStream();
        var downloadRequest = drive.Files.Get(fileId);
        downloadRequest.SupportsAllDrives = true;
        var progress = await downloadRequest.DownloadAsync(stream, cancellationToken);
        if (progress.Status != DownloadStatus.Completed)
        {
            throw progress.Exception ?? new InvalidOperationException("Không thể tải JSON từ Google Drive.");
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private async Task<DriveFile> UploadContentAsync(
        DriveFile file,
        string content,
        CancellationToken cancellationToken,
        string? fileId = null)
    {
        var drive = await GetDriveServiceAsync(cancellationToken);
        await using var stream = new MemoryStream(new UTF8Encoding(false).GetBytes(content));
        if (fileId is null)
        {
            var createRequest = drive.Files.Create(file, stream, "application/json");
            createRequest.SupportsAllDrives = true;
            EnsureUploadCompleted(await createRequest.UploadAsync(cancellationToken));
            return createRequest.ResponseBody
                ?? throw new InvalidOperationException("Google Drive không trả về thông tin file sau khi lưu.");
        }

        var updateRequest = drive.Files.Update(file, fileId, stream, "application/json");
        updateRequest.SupportsAllDrives = true;
        EnsureUploadCompleted(await updateRequest.UploadAsync(cancellationToken));
        return updateRequest.ResponseBody
            ?? throw new InvalidOperationException("Google Drive không trả về thông tin file sau khi lưu.");
    }

    private async Task<DriveService> GetDriveServiceAsync(CancellationToken cancellationToken)
    {
        if (_drive is not null)
        {
            return _drive;
        }

        await _driveInitializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_drive is not null)
            {
                return _drive;
            }

            var credentialsPath = _configuration["GoogleDrive:OAuthCredentialsPath"];
            if (string.IsNullOrWhiteSpace(credentialsPath) || !System.IO.File.Exists(credentialsPath))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình GoogleDrive:OAuthCredentialsPath hoặc file OAuth client credentials không tồn tại.");
            }

            var tokenPath = _configuration["GoogleDrive:TokenPath"];
            if (string.IsNullOrWhiteSpace(tokenPath))
            {
                tokenPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EnglishLearning",
                    "GoogleDriveTokens");
            }

            var secrets = GoogleClientSecrets.FromFile(credentialsPath).Secrets;
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                new[] { DriveService.Scope.Drive },
                "english-learning-admin",
                cancellationToken,
                new FileDataStore(tokenPath, fullPath: true));

            _drive = new DriveService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "EnglishEdu"
                });
            return _drive;
        }
        finally
        {
            _driveInitializationLock.Release();
        }
    }

    private static void EnsureUploadCompleted(IUploadProgress progress)
    {
        if (progress.Status != UploadStatus.Completed)
        {
            throw progress.Exception ?? new InvalidOperationException("Không thể lưu JSON lên Google Drive.");
        }
    }

    private static void ValidateJson(string content)
    {
        using var document = JsonDocument.Parse(content);
        if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
        {
            throw new ArgumentException("Nội dung JSON phải là object hoặc array.");
        }
    }

    private static string NormalizeFileName(string name)
    {
        var trimmedName = name.Trim();
        if (trimmedName.Length == 0 ||
            trimmedName.Length > 180 ||
            trimmedName.IndexOfAny(new[] { '/', '\\', '\0' }) >= 0 ||
            trimmedName.Any(char.IsControl))
        {
            throw new ArgumentException("Tên file không hợp lệ.");
        }

        if (!trimmedName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            trimmedName += ".json";
        }

        return trimmedName;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Tiêu đề bài học không hợp lệ.");
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length == 0 ||
            trimmedTitle.Length > 180 ||
            trimmedTitle.IndexOfAny(new[] { '/', '\\', '\0' }) >= 0 ||
            trimmedTitle.Any(char.IsControl))
        {
            throw new ArgumentException("Tiêu đề bài học không hợp lệ.");
        }

        return trimmedTitle;
    }

    private static string NormalizeLessonType(string lessonType)
    {
        if (string.IsNullOrWhiteSpace(lessonType))
        {
            throw new ArgumentException("Loại bài học phải là 'lesson' hoặc 'test'.");
        }

        var normalizedType = lessonType.Trim().ToLowerInvariant();
        if (normalizedType is not ("lesson" or "test"))
        {
            throw new ArgumentException("Loại bài học phải là 'lesson' hoặc 'test'.");
        }

        return normalizedType;
    }

    private static string BuildDriveUrl(string fileId) =>
        $"https://drive.google.com/uc?export=download&id={Uri.EscapeDataString(fileId)}";

    private static string? ExtractDriveFileId(string driveUrl)
    {
        if (!Uri.TryCreate(driveUrl, UriKind.Absolute, out var uri) ||
            uri.Host is not ("drive.google.com" or "docs.google.com" or "drive.usercontent.google.com"))
        {
            return null;
        }

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = parameter.Split('=', 2);
            if (pair.Length == 2 && Uri.UnescapeDataString(pair[0]) == "id")
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (segments[index] == "d")
            {
                return Uri.UnescapeDataString(segments[index + 1]);
            }
        }

        return null;
    }

    private static bool IsJsonFile(DriveFile file)
    {
        if (file.MimeType?.StartsWith("application/vnd.google-apps.", StringComparison.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        return file.Name?.EndsWith(".json", StringComparison.OrdinalIgnoreCase) == true ||
            file.MimeType == "application/json";
    }

    private static AdminLessonContentDto ToContentDto(Lesson lesson, ManagedFolder folder, string content) =>
        new()
        {
            Id = lesson.Id,
            Title = lesson.Title,
            LessonType = lesson.LessonType,
            FolderId = folder.Id,
            FolderPath = folder.Path,
            Content = content
        };

    private sealed class ManagedFolder(string id, string name, string path, string parentId)
    {
        public string Id { get; } = id;
        public string Name { get; } = name;
        public string Path { get; } = path;
        public string ParentId { get; } = parentId;
        public List<DriveFile> Files { get; } = [];
    }

    private sealed record ManagedFile(DriveFile File, ManagedFolder Folder);
}