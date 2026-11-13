namespace Gremlin.ViewModels;

public class UpdateQuizViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<UpdateQuestionViewModel> Questions { get; set; } = new();
}

public class UpdateQuestionViewModel
{
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? TypeId { get; set; }
    public List<string> AnswerAlternatives { get; set; } = new();
    public List<int> CorrectAnswerIndices { get; set; } = new();
}