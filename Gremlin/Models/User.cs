using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Gremlin.Models;

public class User: IdentityUser
{
    [Required]
    public string DisplayName {get; set;} = string.Empty;

    // Navigation properties
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<History> Histories { get; set; } = new List<History>();
}