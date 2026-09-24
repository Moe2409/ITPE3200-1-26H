using Microsoft.AspNetCore.Mvc;
using Gremlin.Models;
using Gremlin.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gremlin.Controllers;

public class QuizController : Controller
{
    private readonly GremlinDbContext _gremlinDbContext;

    public QuizController(GremlinDbContext gremlinDbContext)
    {
        _gremlinDbContext = gremlinDbContext;
    }

    [HttpGet("quizzes/all")]
    public IActionResult Table()
    {
        List<Quiz> quizzes = _gremlinDbContext.Quizzes
            .Include(q => q.User)
            .Include(q => q.Questions)
            .ToList();
        var quizzesViewModel = new QuizzesViewModel(quizzes, "Table");
        return View(quizzesViewModel);
    }

    public IActionResult Grid()
    {
        List<Quiz> quizzes = _gremlinDbContext.Quizzes.ToList();
        var quizzesViewModel = new QuizzesViewModel(quizzes, "Table");
        return View(quizzesViewModel);
    }

    [HttpGet("quizzes/{id:int}")]
    public IActionResult Details(int id)
    {
        List<Quiz> quizzes = _gremlinDbContext.Quizzes.ToList();
        var quiz = quizzes.FirstOrDefault(i => i.id == id);
        if (quiz == null)
            return NotFound();
        return View(quiz);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Quiz quiz)
    {
        if (ModelState.IsValid)
        {
            _gremlinDbContext.Quizzes.Add(quiz);
            _gremlinDbContext.SaveChanges();
            return RedirectToAction(nameof(Table));
        }
        return View(quiz);
    }
}