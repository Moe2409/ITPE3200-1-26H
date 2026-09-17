using System.Text.Json;
namespace Gremlin.Models;

public class Question
{
    public int id {get; set;}
    public int quiz_id {get; set;}
    public int type_id {get; set;}
    public string title {get; set;}

    public JsonElement content;
}