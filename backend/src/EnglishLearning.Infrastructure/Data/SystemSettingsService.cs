using System.Globalization;
using EnglishLearning.Application.Features.Admin.DTOs;
using EnglishLearning.Application.Features.Admin.Interfaces;
using EnglishLearning.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Data;

public sealed class SystemSettingsService : ISystemSettingsService
{
    public const string VocabularyLearnedXpKey = "Gamification.VocabularyLearnedXp";
    public const string LessonCompletedXpKey = "Gamification.LessonCompletedXp";
    public const string CorrectAnswerXpKey = "Gamification.CorrectAnswerXp";
    public const string DailyStreakXpKey = "Gamification.DailyStreakXp";

    private static readonly string[] RequiredKeys =
    [
        VocabularyLearnedXpKey,
        LessonCompletedXpKey,
        CorrectAnswerXpKey,
        DailyStreakXpKey
    ];

    private readonly ApplicationDbContext _context;

    public SystemSettingsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GamificationSettingsDto> GetGamificationSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await ReadSettingsAsync(cancellationToken);
        return new GamificationSettingsDto(
            settings[VocabularyLearnedXpKey],
            settings[LessonCompletedXpKey],
            settings[CorrectAnswerXpKey],
            settings[DailyStreakXpKey]);
    }

    public async Task UpdateGamificationSettingsAsync(
        UpdateGamificationSettingsRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [VocabularyLearnedXpKey] = request.VocabularyLearnedXp,
            [LessonCompletedXpKey] = request.LessonCompletedXp,
            [CorrectAnswerXpKey] = request.CorrectAnswerXp,
            [DailyStreakXpKey] = request.DailyStreakXp
        };
        var settings = await _context.SystemSettings
            .Where(setting => RequiredKeys.Contains(setting.SettingKey))
            .ToListAsync(cancellationToken);
        var settingsByKey = settings.ToDictionary(setting => setting.SettingKey, StringComparer.Ordinal);
        var now = DateTime.UtcNow;

        foreach (var (key, value) in values)
        {
            if (!settingsByKey.TryGetValue(key, out var setting))
            {
                setting = new SystemSetting { SettingKey = key };
                _context.SystemSettings.Add(setting);
                settingsByKey.Add(key, setting);
            }

            setting.SettingValue = value.ToString(CultureInfo.InvariantCulture);
            setting.UpdatedAt = now;
            setting.UsersId = adminUserId;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetXpRewardAsync(
        string settingKey,
        CancellationToken cancellationToken = default)
    {
        var value = await _context.SystemSettings
            .AsNoTracking()
            .Where(setting => setting.SettingKey == settingKey)
            .Select(setting => setting.SettingValue)
            .SingleOrDefaultAsync(cancellationToken);

        if (value is null ||
            !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var xp) ||
            xp < 0)
        {
            throw new InvalidOperationException(
                $"System setting '{settingKey}' is missing or has an invalid XP value.");
        }

        return xp;
    }

    private async Task<Dictionary<string, int>> ReadSettingsAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.SystemSettings
            .AsNoTracking()
            .Where(setting => RequiredKeys.Contains(setting.SettingKey))
            .ToListAsync(cancellationToken);
        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            if (!int.TryParse(row.SettingValue, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ||
                value < 0)
            {
                throw new InvalidOperationException(
                    $"System setting '{row.SettingKey}' has an invalid XP value.");
            }
            result[row.SettingKey] = value;
        }

        var missingKey = RequiredKeys.FirstOrDefault(key => !result.ContainsKey(key));
        if (missingKey is not null)
        {
            throw new InvalidOperationException($"System setting '{missingKey}' is missing.");
        }

        return result;
    }
}
