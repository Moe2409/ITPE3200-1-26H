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
                new User {display_name = "Harry potter"},
                new User {display_name = "Jean Luc Picard"}
            };
            context.Users.AddRange(users);
            context.SaveChanges();
        }

        var defaultUser = context.Users.First();
        var defaultUser2 = context.Users.OrderBy(u => u.id).Last();

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
                    user_id = defaultUser2.id
                },
            };
            context.Quizzes.AddRange(quizzes);
            context.SaveChanges();
        }        
    }
}