namespace EnglishLearning.Application.Features.Admin.DTOs;

public sealed record GamificationSettingsDto(
    int VocabularyLearnedXp,
    int LessonCompletedXp,
    int CorrectAnswerXp,
    int DailyStreakXp);

public sealed record UpdateGamificationSettingsRequest(
    int VocabularyLearnedXp,
    int LessonCompletedXp,
    int CorrectAnswerXp,
    int DailyStreakXp);
