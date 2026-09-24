using Gremlin.Models;

namespace Gremlin.ViewModels
{   
    public class QuizzesViewModel
    {
        public IEnumerable<Quiz> Quizzes;
        public string? CurrentViewName;

        public int? UserId { get; set; }

        public QuizzesViewModel(IEnumerable<Quiz> quizzes, string? currentViewName, int? userId = null)
        {
            Quizzes = quizzes;
            CurrentViewName = currentViewName;
            UserId = userId;
        }
    }
}