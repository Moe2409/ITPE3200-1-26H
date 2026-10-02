using System.ComponentModel.DataAnnotations;

namespace Gremlin.ViewModels
{
    public class CreateQuizViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "The title cannot be over 100 words")]
        public string Title { get; set; } = string.Empty;

        public List<CreateQuestionViewModel> Questions { get; set; } = new();
    }

    public class CreateQuestionViewModel
    {
        [Required(ErrorMessage = "Question text is required")]
        public string Title { get; set; } = string.Empty;

        // List over lternatives
        public List<string> AnswerAlternatives { get; set; } = new();

        // Index for correct answer
        public int CorrectAnswerIndex { get; set; }
    }
}