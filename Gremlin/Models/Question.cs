using System.Text.Json;
namespace Gremlin.Models;

public class Question
{
    public int id {get; set;}
    public int? quiz_id {get; set;}
    public int? type_id {get; set;}
    public string? title {get; set;}

    // Json representaion of question answers
    // public string? content;  

    public List<string> AnswerAlternatives { get; set; } = new List<string>();
    public List<int> CorrectAnswerIndices { get; set; } = new List<int>();

    // Navigation property
    public Quiz? Quiz { get; set; }
}