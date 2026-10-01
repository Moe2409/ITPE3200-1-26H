using Microsoft.AspNetCore.Mvc;
using Gremlin.Models;
using Gremlin.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gremlin.Controllers;

public class QuizController : Controller
{
    private readonly ILogger<QuizController> _logger;
    private readonly GremlinDbContext _gremlinDbContext;

    public QuizController(GremlinDbContext gremlinDbContext, ILogger<QuizController> logger)
    {
        _gremlinDbContext = gremlinDbContext;
        _logger = logger;
    }
    
    

    [HttpGet("quizzes/all")]
    public IActionResult Table()
    {
        try
        {
            List<Quiz> quizzes = _gremlinDbContext.Quizzes
                .Include(q => q.User)
                .Include(q => q.Questions)
                .ToList();
                
            var quizzesViewModel = new QuizzesViewModel(quizzes, "Table");
            return View(quizzesViewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching the quiz table.");
            TempData["ErrorMessage"] = "Unable to load quizzes at this time. Please try again later.";
            return View(new QuizzesViewModel(new List<Quiz>(), "Table"));
        }
    }

   [HttpGet("quizzes/{id:int}")]
    public IActionResult Details(int id)
    {
        try
        {
            // Fixed performance issue: querying by id directly instead of loading all quizzes into memory
            var quiz = _gremlinDbContext.Quizzes
                .Include(q => q.User)
                .Include(q => q.Questions)
                .FirstOrDefault(i => i.id == id);
                
            if (quiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} was not found.", id);
                return NotFound();
            }
            
            return View(quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching details for quiz ID {QuizId}.", id);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

     [HttpGet("quiz/{id:int}")]
    public IActionResult Take(int id)
    {
        try
        {
            var quiz=_gremlinDbContext.Quizzes
                .Include(q=> q.Questions)
                .FirstOrDefault(q=> q.id==id);
            if (quiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} was not found for taking.", id);
                return NotFound();
            }
            var viewModel = new TakequizViewModel
            {
                QuizId = quiz.id,
                QuizTitle = quiz.title,
                Questions = quiz.Questions.Select(q => new QuestionViewModel
                {
                    QuestionId = q.id,
                    Title = q.title,
                    AnswerAlternatives = q.AnswerAlternatives
                }).ToList()
            };

            return View(viewModel);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while loading quiz ID {QuizId} for taking.", id);
            return StatusCode(500, "An internal server error occurred.");
        }
        
    }

   [HttpPost("quizzes/submit")]
    [ValidateAntiForgeryToken]
    public IActionResult Submit(TakequizViewModel model)
    {
        try //errorhandling
        {
            var quiz = _gremlinDbContext.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefault(q => q.id == model.QuizId);

            if (quiz == null)
            {
                return NotFound();
            }
            //score counter and score count
            int score = 0;
            int totalQuestions = quiz.Questions.Count;
            //loopthat loops through the questions and checks correct answers.
            foreach (var submittedQ in model.Questions)
            {
                var dbQuestion = quiz.Questions.FirstOrDefault(q => q.id == submittedQ.QuestionId);

                if (dbQuestion != null && submittedQ.SelectedAnswerIndex.HasValue)
                {   //if that checks if the answer is correct and increases score based on that.
                    if (dbQuestion.CorrectAnswerIndices != null && 
                        dbQuestion.CorrectAnswerIndices.Contains(submittedQ.SelectedAnswerIndex.Value))
                    {
                        score++;
                    }
                }
            }
            //gives u a message with score and total score possible. 
            TempData["SuccessMessage"] = $"Quiz submitted! You got {score} out of {totalQuestions} correct.";
            return RedirectToAction(nameof(Table));
        }
        catch (Exception ex)  //catcher feil og kaster error melding
        {
            _logger.LogError(ex, "An error occurred while submitting quiz ID {QuizId}.", model.QuizId);
            ModelState.AddModelError("", "An error occurred while submitting your quiz. Please try again.");
            return View("Take", model);
        }
    }

   
    
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Quiz quiz)
    {
        if (!ModelState.IsValid)
        {
            return View(quiz);
        }

        try
        {
            _gremlinDbContext.Quizzes.Add(quiz);
            _gremlinDbContext.SaveChanges();
            TempData["SuccessMessage"] = "Quiz created successfully!";
            return RedirectToAction(nameof(Table));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "A database error occurred while creating a new quiz.");
            ModelState.AddModelError("", "A database error occurred while saving. Please check your inputs and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while creating a new quiz.");
            ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
        }

        return View(quiz);
    }
}