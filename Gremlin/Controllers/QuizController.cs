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
                    AnswerAlternatives = q.AnswerAlternatives,
                    IsMultipleChoice = q.CorrectAnswerIndices != null && q.CorrectAnswerIndices.Count > 1
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
            int maxPossibleScore = quiz.Questions.Sum(
                q => q.CorrectAnswerIndices?.Count ?? 0
            );

            foreach (var submittedQ in model.Questions)
            {
                var dbQuestion = quiz.Questions.FirstOrDefault(q => q.id == submittedQ.QuestionId);

                // Verify answer against database correct answer index
                if (dbQuestion?.CorrectAnswerIndices != null && submittedQ.SelectedAnswerIndices != null)
                {   
                    // Give one point for every submitted selection that matches a correct answer
                    foreach (var selectedIndex in submittedQ.SelectedAnswerIndices)
                    {
                        if (dbQuestion.CorrectAnswerIndices.Contains(selectedIndex))
                        {
                            score++;
                        }
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
                quiz_title = quiz.title,
                score = score,
                maxPossibleScore = maxPossibleScore,
                completed_at = DateTime.UtcNow
            };

            _gremlinDbContext.Histories.Add(history);
            
            // Asynchronously persist changes to the database
            await _gremlinDbContext.SaveChangesAsync();

            foreach (var submittedQ in model.Questions)
            {
                var dbQuestion = quiz.Questions.FirstOrDefault(q => q.id == submittedQ.QuestionId);

                if (dbQuestion != null)
                {
                    var historyAnswer = new HistoryAnswer
                    {
                        history_id = history.id,
                        title = dbQuestion.title, // Adjust property name if your Question entity uses 'Text' or similar
                        AnswerAlternatives = dbQuestion.AnswerAlternatives ?? new List<string>(),
                        AnswerIndices = submittedQ.SelectedAnswerIndices ?? new List<int>(),
                        CorrectAnswerIndices = dbQuestion.CorrectAnswerIndices ?? new List<int>()
                    };

                    _gremlinDbContext.historyAnswers.Add(historyAnswer);
                }
            }

            // Persist the history answers
            await _gremlinDbContext.SaveChangesAsync();

            // Feedback and Redirect
            TempData["SuccessMessage"] = $"Quiz submitted! You got {score} out of {totalQuestions} correct.";
            return RedirectToAction("Result", new { id = history.id });
        }
        catch (Exception ex)  
        {
            _logger.LogError(ex, "An error occurred while submitting quiz ID {QuizId}.", model.QuizId);
            ModelState.AddModelError("", "An error occurred while submitting your quiz. Please try again.");
            return View("Take", model);
        }
    }

    // Renders the view for creating a new quiz
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
        // 1. Manuell validering av at hvert spørsmål har minst én riktig svaralternativ
        if (vm.Questions != null)
        {
            for (int i = 0; i < vm.Questions.Count; i++)
            {
                var q = vm.Questions[i];

                // Sjekk om CorrectAnswerIndices er null, eller om listen er tom
                if (q.CorrectAnswerIndices == null || !q.CorrectAnswerIndices.Any())
                {
                    ModelState.AddModelError($"Questions[{i}].CorrectAnswerIndices", $"Question {i + 1} must have at least one correct answer selected.");
                }
            }
        }
        else
        {
            ModelState.AddModelError("", "The quiz must contain at least one question.");
        }

        // Return view with validation errors if model state validation fails (inkludert vår nye feilmelding)
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        try
        {
            // Retrieve the ID of the currently authenticated user
            var userId = _userManager.GetUserId(User);

            var quiz = new Quiz
            {
                // Map the view model data to the Quiz entity and its questions
                title = vm.Title,
                user_id = userId,
                Questions = vm.Questions.Select(q => new Question
                {
                    title = q.Title,
                    // Filter out empty or whitespace-only answer alternatives
                    AnswerAlternatives = q.AnswerAlternatives.Where(a => !string.IsNullOrWhiteSpace(a)).ToList(),
                    CorrectAnswerIndices = q.CorrectAnswerIndices ?? new List<int>()
                }).ToList()
            };
            // Add the new quiz to the database context and save changes
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

            // Return the view with the current view model so the user does not lose their input
            return View(vm);
        }
    }


    // Renders the view for updating a quiz
    [HttpGet("update/{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id)
    {
        try
        {
            // Fetch the quiz and its questions from the database
            var quiz = await _gremlinDbContext.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.id == id);

            if (quiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} was not found for editing.", id);
                return NotFound();
            }

            // Map the Quiz entity to your UpdateQuizViewModel
            var viewModel = new UpdateQuizViewModel
            {
                Id = quiz.id,
                Title = quiz.title,
                Questions = quiz.Questions.Select(q => new UpdateQuestionViewModel
                {
                    Id = q.id,
                    Title = q.title,
                    AnswerAlternatives = q.AnswerAlternatives ?? new List<string>(),
                    CorrectAnswerIndices = q.CorrectAnswerIndices ?? new List<int>()
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while loading quiz ID {QuizId} for update.", id);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    // update a quiz
    [HttpPost("update/{id:int}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, UpdateQuizViewModel vm) // Removed [FromBody]
    {
        if (vm == null || id != vm.Id)
        {
            return BadRequest("Quiz ID mismatch or quiz data is null.");
        }

        // Return the view with validation errors if model state validation fails
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        try
        {
            // Fetch the existing quiz and its questions using the DbContext
            var existingQuiz = await _gremlinDbContext.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.id == id);

            if (existingQuiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} was not found for update.", id);
                return NotFound($"Quiz with ID {id} not found.");
            }

            // Map updated quiz title
            existingQuiz.title = vm.Title;

            var incomingIds = vm.Questions?.Select(q => q.Id).ToList();

            // Remove questions that are no longer present in the update payload
            var questionsToRemove = existingQuiz.Questions.Where(q => !incomingIds.Contains(q.id)).ToList();
            foreach (var q in questionsToRemove)
            {
                existingQuiz.Questions.Remove(q);
                _gremlinDbContext.Questions.Remove(q);
            }

            // Update existing questions or add new ones
            if (vm.Questions != null)
            {
                foreach (var qVm in vm.Questions)
                {
                    var existingQuestion = existingQuiz.Questions.FirstOrDefault(q => q.id == qVm.Id);
                    if (existingQuestion != null)
                    {
                        // Update properties of the tracked question in-place
                        existingQuestion.title = qVm.Title;
                        existingQuestion.AnswerAlternatives = qVm.AnswerAlternatives != null 
                            ? new List<string>(qVm.AnswerAlternatives) 
                            : new List<string>();
                        existingQuestion.CorrectAnswerIndices = qVm.CorrectAnswerIndices != null 
                            ? new List<int>(qVm.CorrectAnswerIndices) 
                            : new List<int>();
                    }
                    else
                    {
                        // Add a brand new question to the tracked collection
                        existingQuiz.Questions.Add(new Question
                        {
                            quiz_id = id,
                            title = qVm.Title,
                            AnswerAlternatives = qVm.AnswerAlternatives != null 
                                ? new List<string>(qVm.AnswerAlternatives) 
                                : new List<string>(),
                            CorrectAnswerIndices = qVm.CorrectAnswerIndices != null 
                                ? new List<int>(qVm.CorrectAnswerIndices) 
                                : new List<int>()
                        });
                    }
                }
            }

            // Persist changes to the database
            await _gremlinDbContext.SaveChangesAsync();

            _logger.LogInformation("Quiz with ID {QuizId} updated successfully.", id);
            TempData["SuccessMessage"] = "Quiz updated successfully!";
            return RedirectToAction(nameof(Table));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating quiz ID {QuizId}.", id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the quiz. Please try again later.");
            return View(vm);
        }
    }

    // Deletes an existing quiz
    [HttpPost("{id:int}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            // Fetch the quiz and its related questions
            var quiz = await _gremlinDbContext.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.id == id);

            if (quiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} was not found for deletion.", id);
                return NotFound($"Quiz with ID {id} not found.");
            }

            _gremlinDbContext.Quizzes.Remove(quiz);
            await _gremlinDbContext.SaveChangesAsync();

            _logger.LogInformation("Quiz with ID {QuizId} deleted successfully.", id);
            return Ok(new { message = "Quiz deleted successfully.", id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting quiz ID {QuizId}.", id);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    [HttpGet("result/{id}")]
    [Authorize]
    public async Task<IActionResult> Result(int id)
    {
        var history = await _gremlinDbContext.Histories
            .Include(h => h.Quiz)
            .ThenInclude(q => q.Questions)
            .Include(h => h.HistoryAnswers)
            .FirstOrDefaultAsync(h => h.id == id);

        if (history == null)
        {
            return NotFound();
        }


        var viewModel = new QuizResultViewModel
        {
            Id = history.id,
            Score = history.score ?? 0,              
            Title = history.quiz_title ?? "",         
            MaxPossibleScore = history.maxPossibleScore ?? 0, 
            CompletedAt = history.completed_at ?? DateTime.Now, 
            Quiz = history.Quiz,
            Answers = history.HistoryAnswers.ToList(),
        };

    return View(viewModel);
}
}