namespace Gremlin.Models;

public class Quiz
{
    public int id {get; set;}
    public int? user_id {get; set;}
    public string? title {get; set;}

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<History> Histories { get; set; } = new List<History>();
}