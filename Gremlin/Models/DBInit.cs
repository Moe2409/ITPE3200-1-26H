using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gremlin.Models;

public static class DBInit
{
    public static void Seed(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var services = serviceScope.ServiceProvider;

        var context = services.GetRequiredService<GremlinDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        if (!context.Users.Any())
        {
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

        var defaultUser = context.Users.OrderBy(u => u.Id).First();
        var defaultUser2 = context.Users.OrderBy(u => u.Id).Last();

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