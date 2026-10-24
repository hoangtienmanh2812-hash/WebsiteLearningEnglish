using EnglishLearning.Application.Features.Admin.DTOs;

namespace EnglishLearning.Application.Features.Admin.Interfaces;

public interface ISystemSettingsService
{
    Task<GamificationSettingsDto> GetGamificationSettingsAsync(CancellationToken cancellationToken = default);

    Task UpdateGamificationSettingsAsync(
        UpdateGamificationSettingsRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task<int> GetXpRewardAsync(string settingKey, CancellationToken cancellationToken = default);
}
