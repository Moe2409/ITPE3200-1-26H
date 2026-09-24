using Microsoft.AspNetCore.Mvc;
using Gremlin.Models;
using Gremlin.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gremlin.Controllers;

public class UserController : Controller
{
    private readonly GremlinDbContext _gremlinDbContext;

    public UserController(GremlinDbContext gremlinDbContext)
    {
        _gremlinDbContext = gremlinDbContext;
    }
    

    [HttpGet("user/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _gremlinDbContext.Users
            .FirstOrDefaultAsync(u => u.id == id);

        if (user == null)
        {
            return NotFound();
        }

        var quizzes = await _gremlinDbContext.Quizzes
            .Where(q => q.user_id == id)
            .Include(q => q.Questions)
            .ToListAsync();

        var histories = await _gremlinDbContext.Histories
            .Where(h => h.user_id == id)
            .Include(h => h.Quiz)
            .OrderByDescending(h => h.completed_at)
            .ToListAsync();

        var viewModel = new UserViewModel(user, quizzes, histories);

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(User user)
    {
        if (ModelState.IsValid)
        {
            _gremlinDbContext.Users.Add(user);
            _gremlinDbContext.SaveChanges();
            return RedirectToAction(nameof(Details), new { id = user.id });
        }
        
        return View(user);
    }
}