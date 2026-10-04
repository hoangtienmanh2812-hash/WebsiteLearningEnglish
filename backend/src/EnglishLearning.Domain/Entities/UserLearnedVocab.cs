using System;
using System.ComponentModel.DataAnnotations;

namespace EnglishLearning.Domain.Entities
{
    public class UserLearnedVocab
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; } // Khớp kiểu GUID với bảng Users
        public int DayNumber { get; set; }
        public int VocabId { get; set; }
        public DateTime LearnedDate { get; set; }

        // Navigation property (nếu có)
        public User? User { get; set; }
    }
}