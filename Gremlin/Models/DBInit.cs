using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gremlin.Models;

// Class for initializing and filling the database with test data (seeding)
public static class DBInit
{   
    public static void Seed(IApplicationBuilder app)
    {
        // Making a scope to resolve scoped services
        using var serviceScope = app.ApplicationServices.CreateScope();
        var services = serviceScope.ServiceProvider;

        // Getting database context and UserManager from ServiceProvider
        var context = services.GetRequiredService<GremlinDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Deleting and creating the database each time the application runs 
        // Good for testing and development phase
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        if (!context.Users.Any())
        {
            // List with users and their passwords
            var usersWithPasswords = new List<(IdentityUser User, string Password)>
            {
                (
                    new IdentityUser
                    {
                        Email = "harry.potter@hogwarts.co.uk",
                        UserName = "Harry_potter",
                        EmailConfirmed = true
                    },
                    "Password123!"
                ),
                (
                    new IdentityUser
                    {
                        Email = "J.L.Picard@ufp.org",
                        UserName = "Jean_Luc_Picard",
                        EmailConfirmed = true
                    },
                    "Password123!"
                )
            };  

            // Loop to create each user using UserManager, which handles password-hashing
            foreach (var (user, password) in usersWithPasswords)
            {
                var result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to seed user '{user.UserName}': {errors}");
                }
            }
        }

        // Getting the first and last user from the database to connect quizzes 
        var defaultUser = context.Users.OrderBy(u => u.Id).First();
        var defaultUser2 = context.Users.OrderBy(u => u.Id).Last();

        // Creating test quizzes
        if (!context.Quizzes.Any())
        {
            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    title = "HarryPotterFood",
                    user_id = defaultUser.Id
                },
                new Quiz
                {
                    title = "Hamburger",
                    user_id = defaultUser2.Id
                }
            };
            context.Quizzes.AddRange(quizzes);
            context.SaveChanges();
        }

        // Create a set of sample questions for the sample quiz
        if (!context.Questions.Any())
        {
            var harryPotterFoodQuiz = context.Quizzes.FirstOrDefault(q => q.title == "HarryPotterFood");

            if (harryPotterFoodQuiz != null)
            {
                var questions = new List<Question>
                {
                    new Question
                    {
                        quiz_id = harryPotterFoodQuiz.id,
                        type_id = 1,
                        title = "What is Harry Potter's favorite flavor of Bertie Bott's Every Bottle Bean?",
                        AnswerAlternatives = new List<string> { "Earwax", "Sprout", "Chocolate", "Every flavor means *every* flavor" },
                        CorrectAnswerIndices = new List<int> { 3 }
                    },
                    new Question
                    {
                        quiz_id = harryPotterFoodQuiz.id,
                        type_id = 1,
                        title = "Which Hogwarts house has a ghost known as the Fat Friar?",
                        AnswerAlternatives = new List<string> { "Gryffindor", "Slytherin", "Hufflepuff", "Ravenclaw" },
                        CorrectAnswerIndices = new List<int> { 2 }
                    },
                    new Question
                    {
                        quiz_id = harryPotterFoodQuiz.id,
                        type_id = 1,
                        title = "What type of creature is Aragog, Hagrid's pet spider?",
                        AnswerAlternatives = new List<string> { "Acromantula", "Basilisk", "Thestral", "Hippogriff" },
                        CorrectAnswerIndices = new List<int> { 0 }
                    },
                    new Question
                    {
                        quiz_id = harryPotterFoodQuiz.id,
                        type_id = 1,
                        title = "What is the core of Harry Potter's wand?",
                        AnswerAlternatives = new List<string> { "Dragon heartstring", "Phoenix feather", "Unicorn hair", "Thestral tail hair" },
                        CorrectAnswerIndices = new List<int> { 1 }
                    }
                };

                context.Questions.AddRange(questions);
                context.SaveChanges();
            }
        }
    }
}