namespace EnglishLearning.Domain.Entities;

public class Category
{
    public int Idcategories { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
