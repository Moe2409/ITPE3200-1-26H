namespace Gremlin.Models;

public class History
{
    public int id {get; set;}
    public int user_id {get; set;}
    public int quiz_id {get; set;}
    public DateTime completed_at {get; set;}

    public int score {get; set;}
}