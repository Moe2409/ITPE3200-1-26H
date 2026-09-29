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