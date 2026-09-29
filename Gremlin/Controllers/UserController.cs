using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Gremlin.Models;
using Gremlin.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gremlin.Controllers;

public class UserController : Controller
{
    //logger for the user
    private readonly ILogger<UserController> _logger;
    private readonly GremlinDbContext _gremlinDbContext;
    
    public UserController(GremlinDbContext gremlinDbContext, ILogger<UserController> logger)
    {
        _gremlinDbContext = gremlinDbContext;
        _logger = logger;
    }


    [HttpGet("user/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        var user = await _gremlinDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id);

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

    [HttpGet("user/create")]
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
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }
        
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username)
    {
        var user = await _gremlinDbContext.Users
            .FirstOrDefaultAsync(u => u.DisplayName == username);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid login credentials.");
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.DisplayName ?? "")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet("user/update")]
    public IActionResult Update()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Update(string id)
    {
        var user = _gremlinDbContext.Users.Find(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    [HttpPost]
    public IActionResult Update(User user)
    {
        if (ModelState.IsValid)
        {
            _gremlinDbContext.Users.Update(user);
            _gremlinDbContext.SaveChanges();
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }
        return View(user);
    }
}