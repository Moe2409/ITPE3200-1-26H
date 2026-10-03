using Gremlin.Models;
namespace Gremlin.ViewModels;


public class TakequizViewModel
{
    public int QuizId { get; set; }
    public string? QuizTitle { get; set; }
    
    public List<QuestionViewModel> Questions { get; set; } = new();
}
public class QuestionViewModel
{
    public int QuestionId { get; set; }
    public string? Title { get; set; }
    public List<string> AnswerAlternatives { get; set; } = new();
    public bool IsMultipleChoice { get; set; }
    // Tracks what the user clicks
    public List<int> SelectedAnswerIndices { get; set; } = new();
}