using Microsoft.AspNetCore.Mvc;
using Gremlin.Models;
using Gremlin.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gremlin.Controllers;

public class QuizController : Controller
{
    private readonly QuizDbContext _quizDbContext;

    public QuizController(QuizDbContext quizDbContext)
    {
        _quizDbContext = quizDbContext;
    }

    public IActionResult Table()
    {
        List<Quiz> quizzes = _quizDbContext.Quizzes
            .Include(q => q.User)
            .Include(q => q.Questions)
            .ToList();
        var quizzesViewModel = new QuizzesViewModel(quizzes, "Table");
        return View(quizzesViewModel);
    }

    public IActionResult Grid()
    {
        List<Quiz> quizzes = _quizDbContext.Quizzes.ToList();
        var quizzesViewModel = new QuizzesViewModel(quizzes, "Table");
        return View(quizzesViewModel);
    }

    public IActionResult Details(int id)
    {
        List<Quiz> quizzes = _quizDbContext.Quizzes.ToList();
        var quiz = quizzes.FirstOrDefault(i => i.id == id);
        if (quiz == null)
            return NotFound();
        return View(quiz);
    }
}