using System.ComponentModel.DataAnnotations;

namespace Gremlin.Models;

public class User
{
    public int id {get; set;}
    [Required]
    public string display_name {get; set;} = string.Empty;
    [Required]
    public string password_hash {get; set;} = string.Empty;

    // Navigation properties
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<History> Histories { get; set; } = new List<History>();
}