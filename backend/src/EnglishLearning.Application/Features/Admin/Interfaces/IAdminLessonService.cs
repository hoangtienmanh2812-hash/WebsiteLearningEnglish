using EnglishLearning.Application.Features.Admin.DTOs;

namespace EnglishLearning.Application.Features.Admin.Interfaces;

public interface IAdminLessonService
{
    Task<IReadOnlyList<AdminFolderDto>> GetFoldersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminLessonDto>?> GetLessonsAsync(string? folderId, CancellationToken cancellationToken = default);
    Task<AdminLessonContentDto?> GetLessonAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminLessonContentDto> CreateLessonAsync(SaveAdminLessonRequest request, CancellationToken cancellationToken = default);
    Task<AdminLessonContentDto?> UpdateLessonAsync(int id, UpdateAdminLessonRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteLessonAsync(int id, CancellationToken cancellationToken = default);
}
