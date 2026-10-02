using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gremlin.ViewModels;
using Gremlin.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Transactions;

namespace Gremlin.Controllers;


// Controller handling user account operations like login/logout, update, delete and publick userprofiles
[Route("account")]
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly GremlinDbContext _context;
    private readonly ILogger<AccountController> _logger;

    
    public AccountController(
        UserManager<IdentityUser> userManager, 
        SignInManager<IdentityUser> signInManager, 
        GremlinDbContext context, 
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
    }

    
    // Displays the user registration view.
    [HttpGet("register")]
    public IActionResult Register() => View();

    
    // Processes the user registration submission.

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = new IdentityUser { UserName = model.UserName, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");
                // Automatically sign in the user upon successful registration
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            // Map identity errors to the ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while registering user {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred during registration. Please try again later.");
        }

        return View(model);
    }

   
    // Displays the login view, optionally capturing a return URL for post-login redirection.
    
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    
    // Processes the user login form submission and validates credentials.
   
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        // Safely determine target redirection URL to prevent open redirect vulnerabilities
        string targetUrl = !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) 
            ? model.ReturnUrl 
            : Url.Action("Index", "Home") ?? "~/";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // Attempt password sign-in with lockout functionality enabled on failure
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in successfully.");
                return Redirect(targetUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                ModelState.AddModelError(string.Empty, "This account has been locked out, please try again later.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while logging in user {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred during login. Please try again later.");
        }

        return View(model);
    }

    
    // Signs out the currently user.
    
    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while logging out user.");
        }

        return RedirectToAction("Index", "Home");
    }

    
    // Displays the account update/settings view for the logged in user.
   
    [HttpGet("update")]
    [Authorize]
    public async Task<IActionResult> Update()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var model = new UpdateAccountViewModel
            {
                UserName = user.UserName ?? string.Empty,
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while loading the update profile page.");
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    // Processes updates to the users account details both password and username.
    [HttpPost("update")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateAccountViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Update username if it has changed
            if (user.UserName != model.UserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }
            
            // Update password if a new one is provided
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (removeResult.Succeeded)
                {
                    var addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
                    if (!addPasswordResult.Succeeded)
                    {
                        foreach (var error in addPasswordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                }
                else
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            // Refresh authentication cookie to reflect updated identity data
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User account updated successfully.");

            TempData["StatusMessage"] = "Profilen din har blitt oppdatert.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating user account.");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving changes.");
        }

        return View(model);
    }

    // Displays the account deletion confirmation view.
    [HttpGet("delete")]
    [Authorize]
    public async Task<IActionResult> Delete()
    {
        var user = await _userManager.GetUserAsync(User) 
                   ?? (User.Identity?.Name != null ? await _userManager.FindByNameAsync(User.Identity.Name) : null);
        
        if (user == null)
        {
            return NotFound("User not found.");
        }

        return View();
    }

    // Permanently deletes the user account along with everything else connected with the user.
    [HttpPost("delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null && User.Identity?.Name != null)
            {
                user = await _userManager.FindByNameAsync(User.Identity.Name);
            }

            if (user == null)
            { 
                _logger.LogWarning("Kunne ikke finne brukeren som forsøker å slette kontoen.");
                return NotFound("User not found.");
            }

            // Clean up related quizzes created by the user
            var quizzes = await _context.Quizzes.Where(q => q.user_id == user.Id).ToListAsync();
            if (quizzes.Any())
            { 
                _context.Quizzes.RemoveRange(quizzes);
            }

            // Clean up related history entries for the user
            var histories = await _context.Histories.Where(h => h.user_id == user.Id).ToListAsync();
            if (histories.Any())
            {
                _context.Histories.RemoveRange(histories);
            }

            await _context.SaveChangesAsync();

            // Delete the user account
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            { 
                // Sign out only after successful deletion
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User account deleted successfully.");
                return RedirectToAction("Index", "Home");
            }
        
            foreach (var error in result.Errors)
            { 
                _logger.LogWarning("Failed to delete user: {Error}", error.Description);    
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting user account.");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while attempting to delete your account.");
        }

        TempData["ErrorMessage"] = "Kunne ikke slette kontoen. Vennligst prøv igjen.";
        return RedirectToAction("Update");
    }

    // Displays a profile page for a specific user based on their username
    [HttpGet("{UserName}")]
    public async Task<IActionResult> Profile(string UserName)
    {
        if (string.IsNullOrEmpty(UserName))
        {
            return NotFound();
        }

        var user = await _userManager.FindByNameAsync(UserName);

        if (user == null)
        {
            return NotFound($"User with username '{UserName}' was not found.");
        }

        // Fetch users quizzes including questions
        var quizzes = await _context.Quizzes
            .Include(q => q.Questions)
            .Where(q => q.user_id == user.Id)
            .ToListAsync();

        // Fetch users history including associated quizzes
        var histories = await _context.Histories
            .Include(h => h.Quiz)
            .Where(h => h.user_id == user.Id)
            .ToListAsync();

        var viewModel = new UserViewModel(user, quizzes, histories);

        return View("UserDetails", viewModel);
    }
}