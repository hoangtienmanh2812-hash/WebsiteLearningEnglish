using System.ComponentModel.DataAnnotations;

namespace EnglishLearning.Domain.Entities
{
    public class DailyVocabPack
    {
        [Key]
        public int DayNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string JsonUrl { get; set; } = string.Empty;
    }
}