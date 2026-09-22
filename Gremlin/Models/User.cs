namespace Gremlin.Models;

public class User
{
    public int id {get; set;}
    public string? display_name {get; set;}
    public string? password_hash {get; set;}

    // Navigation properties
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<History> Histories { get; set; } = new List<History>();
}