using Microsoft.EntityFrameworkCore;

namespace Gremlin.Models;

public static class DBInit
{
    public static void Seed(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        QuizDbContext context = serviceScope.ServiceProvider.GetRequiredService<QuizDbContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        
        if (!context.Users.Any())
        {
            var users = new List<User>
            {
                new User {display_name = "Harry potter"}
            };
            context.Users.AddRange(users);
            context.SaveChanges();
        }

        var defaultUser = context.Users.First();

        if (!context.Quizzes.Any())
        {
            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    title = "Pizza",
                    user_id = defaultUser.id
                },new Quiz
                {
                    title = "Hamburger",
                    user_id = defaultUser.id
                },
            };
            context.Quizzes.AddRange(quizzes);
            context.SaveChanges();
        }        
    }
}