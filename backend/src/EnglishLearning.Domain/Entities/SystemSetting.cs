namespace EnglishLearning.Domain.Entities;

public sealed class SystemSetting
{
    public int Idsetting { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public Guid? UsersId { get; set; }
}
