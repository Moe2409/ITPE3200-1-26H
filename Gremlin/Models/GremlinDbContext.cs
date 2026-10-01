using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gremlin.Models;


public class GremlinDbContext : IdentityDbContext
{
    public GremlinDbContext(DbContextOptions<GremlinDbContext> options) : base(options)
    {
        
    }
    public DbSet<History> Histories { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Make sure UserName is unique
        modelBuilder.Entity<Quiz>()
            .HasOne(q => q.User)
            .WithMany()
            .HasForeignKey(q => q.user_id)
            .OnDelete(DeleteBehavior.SetNull);

        // fk_Quiz_id_Question
        modelBuilder.Entity<Question>()
            .HasOne(q => q.Quiz)
            .WithMany(qz => qz.Questions)
            .HasForeignKey(q => q.quiz_id)
            .OnDelete(DeleteBehavior.Cascade);

        // fk_User_id_History
        modelBuilder.Entity<History>()
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.user_id)
            .OnDelete(DeleteBehavior.Restrict);

        // fk_Quiz_id_History
        modelBuilder.Entity<History>()
            .HasOne(h => h.Quiz)
            .WithMany(q => q.Histories)
            .HasForeignKey(h => h.quiz_id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}