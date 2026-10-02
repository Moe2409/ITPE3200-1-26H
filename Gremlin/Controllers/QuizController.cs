using Microsoft.AspNetCore.Mvc;
using Gremlin.Models;
using Gremlin.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Gremlin.Controllers;

[Route("quiz")]
public class QuizController : Controller
{
    private readonly ILogger<QuizController> _logger;
    private readonly GremlinDbContext _gremlinDbContext;
    private readonly UserManager<IdentityUser> _userManager;

    public QuizController(
        GremlinDbContext gremlinDbContext, 
        ILogger<QuizController> logger,
        UserManager<IdentityUser> userManager)
    {
        _gremlinDbContext = gremlinDbContext;
        _logger = logger;
        _userManager = userManager;
    }
    
    

    [HttpGet("all")]
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

   [HttpGet("{id:int}")]
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

     [HttpGet("{id:int}/take")]
    [Authorize] 
    public IActionResult Take(int id)
    {
        try
        {
            var quiz = _gremlinDbContext.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefault(q => q.id == id);
                
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

[HttpPost("{QuizId:int}/submit")]
[Authorize] // Ensures the user is signed in
[ValidateAntiForgeryToken]
public async Task<IActionResult> Submit(TakequizViewModel model)
{
    try 
    {
        // Line 118: await is now valid inside async Task<IActionResult>
        var quiz = await _gremlinDbContext.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.id == model.QuizId);

        if (quiz == null)
        {
            return NotFound();
        }

        // 1. Calculate Score
        int score = 0;
        int totalQuestions = quiz.Questions.Count;

        foreach (var submittedQ in model.Questions)
        {
            var dbQuestion = quiz.Questions.FirstOrDefault(q => q.id == submittedQ.QuestionId);

            if (dbQuestion != null && submittedQ.SelectedAnswerIndex.HasValue)
            {   
                if (dbQuestion.CorrectAnswerIndices != null && 
                    dbQuestion.CorrectAnswerIndices.Contains(submittedQ.SelectedAnswerIndex.Value))
                {
                    score++;
                }
            }
        }

        // 2. Get Current Logged-In User ID
        var userId = _userManager.GetUserId(User);

        // 3. Create & Save History Record
        var history = new History
        {
            user_id = userId,
            quiz_id = quiz.id,
            score = score,
            completed_at = DateTime.UtcNow
        };

        _gremlinDbContext.Histories.Add(history);
        
        // Line 158: await is now valid
        await _gremlinDbContext.SaveChangesAsync();

        // 4. Feedback & Redirect
        TempData["SuccessMessage"] = $"Quiz submitted! You got {score} out of {totalQuestions} correct.";
        return RedirectToAction(nameof(Table));
    }
    catch (Exception ex)  
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