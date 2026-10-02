using Microsoft.AspNetCore.Mvc;
using Gremlin.Models;
using Gremlin.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Gremlin.Controllers;



//handles quiz related operations like liting, viewing, taking, submitting and creating quizzes 
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

   
    // Displays a table of all available quizzes.
    [HttpGet("all")]
    public IActionResult Table()
    {
        try
        {
            // loads relational User and Question data alongside Quizzes
            List<Quiz> quizzes = _gremlinDbContext.Quizzes
                .Include(q => q.User)
                .Include(q => q.Questions)
                .ToList();
                
            // Wrap quizzes in a ViewModel
            var quizzesViewModel = new QuizzesViewModel(quizzes, "Table");
            return View(quizzesViewModel);
        }
        catch (Exception ex)
        {
            // Log full exception context and display a user-friendly error message via TempData
            _logger.LogError(ex, "An error occurred while fetching the quiz table.");
            TempData["ErrorMessage"] = "Unable to load quizzes at this time. Please try again later.";
            return View(new QuizzesViewModel(new List<Quiz>(), "Table"));
        }
    }

    
    // Displays details for a single specific quiz.
    [HttpGet("{id:int}")]
    public IActionResult Details(int id)
    {
        try
        {
            // Directly query the database for the matching ID using FirstOrDefault to avoid loading all records into memory
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

   
    // Renders the form for an authenticated user to take a specific quiz.
    
    [HttpGet("{id:int}/take")]
    [Authorize] // Restricts access to authenticated users only
    public IActionResult Take(int id)
    {
        try
        {
            // Retrieve quiz and associated questions
            var quiz = _gremlinDbContext.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefault(q => q.id == id);
                
            if (quiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} was not found for taking.", id);
                return NotFound();
            }
            
            // Map the entity model to a presentation ViewModel (TakequizViewModel)
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

    // Processes submitted quiz responses, grades the quiz, and stores the result in user history.
    [HttpPost("{QuizId:int}/submit")]
    [Authorize] // Requires authenticated session
    [ValidateAntiForgeryToken] // Protects against Cross-Site Request Forgery (CSRF)
    public async Task<IActionResult> Submit(TakequizViewModel model)
    {
        try 
        {
            // Asynchronously fetch the matching quiz entity from DB
            var quiz = await _gremlinDbContext.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.id == model.QuizId);

            if (quiz == null)
            {
                return NotFound();
            }

            // Calculate Score
            int score = 0;
            int totalQuestions = quiz.Questions.Count;

            foreach (var submittedQ in model.Questions)
            {
                var dbQuestion = quiz.Questions.FirstOrDefault(q => q.id == submittedQ.QuestionId);

                // Verify answer against database correct answer index
                if (dbQuestion != null && submittedQ.SelectedAnswerIndex.HasValue)
                {   
                    if (dbQuestion.CorrectAnswerIndices != null && 
                        dbQuestion.CorrectAnswerIndices.Contains(submittedQ.SelectedAnswerIndex.Value))
                    {
                        score++;
                    }
                }
            }

            // Get Current Logged-In User ID via ASP.NET Core Identity
            var userId = _userManager.GetUserId(User);

            // Create and Save History Record
            var history = new History
            {
                user_id = userId,
                quiz_id = quiz.id,
                score = score,
                completed_at = DateTime.UtcNow
            };

            _gremlinDbContext.Histories.Add(history);
            
            // Asynchronously persist changes to the database
            await _gremlinDbContext.SaveChangesAsync();

            // Feedback and Redirect
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

    // Renders the view for creating a new quiz.
    [HttpGet("create")]
    [Authorize]
        public IActionResult Create()
    {
        return View(new CreateQuizViewModel());
    }

   // Handles the form submission for creating a new quiz entity
[HttpPost("create")]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateQuizViewModel vm)
{
    // Return view with validation errors if model state validation fails
    if (!ModelState.IsValid)
    {
        return View(vm);
    }

    try
    {
        // 2. Retrieve the ID of the currently authenticated user
        var userId = _userManager.GetUserId(User);

        var quiz = new Quiz
        {
            // 3. Map the view model data to the Quiz entity and its questions
            title = vm.Title,
            user_id = userId,
            Questions = vm.Questions.Select(q => new Question
            {
                title = q.Title,
                // Filter out empty or whitespace-only answer alternatives
                AnswerAlternatives = q.AnswerAlternatives.Where(a => !string.IsNullOrWhiteSpace(a)).ToList(),
                CorrectAnswerIndices = new List<int> { q.CorrectAnswerIndex }
            }).ToList()
        };
        // 4. Add the new quiz to the database context and save changes
        _gremlinDbContext.Quizzes.Add(quiz);
        await _gremlinDbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Quiz created successfully!";
        return RedirectToAction(nameof(Table));
    }
    catch (Exception ex)
    {
        var userId = _userManager.GetUserId(User);

        _logger.LogError(ex, "An error occurred while creating a quiz for user ID: {UserId}", userId);
        ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the quiz. Please try again later.");

        // 3. Return the view with the current view model so the user doesn't lose their input
        return View(vm);
    }
}
}