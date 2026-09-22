using Gremlin.Models;

namespace Gremlin.ViewModels
{   
    public class QuizzesViewModel
    {
        public IEnumerable<Quiz> Quizzes;
        public string? CurrentViewName;

        public QuizzesViewModel(IEnumerable<Quiz> quizzes, string? currentViewName)
        {
            Quizzes = quizzes;
            CurrentViewName = currentViewName;
        }
    }
}