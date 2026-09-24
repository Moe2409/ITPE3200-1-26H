using Gremlin.Models;

namespace Gremlin.ViewModels;

public class UserViewModel
{
    public User User { get; set; } = null!;
    public IEnumerable<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public IEnumerable<History> Histories { get; set; } = new List<History>();

    public UserViewModel(User user, IEnumerable<Quiz> quizzes, IEnumerable<History> histories)
    {
        User = user;
        Quizzes = quizzes;
        Histories = histories;
    }
}