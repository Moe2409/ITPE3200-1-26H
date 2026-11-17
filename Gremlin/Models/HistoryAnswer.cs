using Microsoft.AspNetCore.Identity;
namespace Gremlin.Models;

public class HistoryAnswer
{
    public int id {get; set;}
    public int history_id {get; set;}
    public string? title {get; set;}
    public List<string> AnswerAlternatives { get; set; } = new List<string>();
    public List<int> AnswerIndices { get; set; } = new List<int>();
    public List<int> CorrectAnswerIndices { get; set; } = new List<int>();

    //navigation
    public History? History { get; set; }
}