using Microsoft.EntityFrameworkCore;

namespace Gremlin.Models;

public static class DBInit
{
    public static void Seed(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        GremlinDbContext context = serviceScope.ServiceProvider.GetRequiredService<GremlinDbContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        
        if (!context.Users.Any())
        {
            var users = new List<User>
            {
                new User {
                    Email = "harry.potter@hogwarts.co.uk",
                    DisplayName = "Harry potter",
                    PasswordHash = "12345"
                    },
                new User {
                    Email = "J.L.Picard@ufp.org",
                    DisplayName = "Jean Luc Picard",
                    PasswordHash = "12345"
                    }
            };
            context.Users.AddRange(users);
            context.SaveChanges();
        }

        var defaultUser = context.Users.First();
        var defaultUser2 = context.Users.OrderBy(u => u.Id).Last();

        if (!context.Quizzes.Any())
        {
            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    title = "Pizza",
                    user_id = defaultUser.Id
                },new Quiz
                {
                    title = "Hamburger",
                    user_id = defaultUser2.Id
                },
            };
            context.Quizzes.AddRange(quizzes);
            context.SaveChanges();
        }        
    }
}