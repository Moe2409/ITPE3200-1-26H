using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gremlin.Models;


public class GremlinDbContext : IdentityDbContext
{
    public GremlinDbContext(DbContextOptions<GremlinDbContext> options) : base(options)
    {
        
    }
    public virtual DbSet<History> Histories { get; set; }
    public virtual DbSet<Question> Questions { get; set; }
    public virtual DbSet<Quiz> Quizzes { get; set; }    
    public virtual DbSet<HistoryAnswer> historyAnswers { get; set; }    

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

        modelBuilder.Entity<History>()
            .HasMany(h => h.HistoryAnswers)
            .WithOne(ha => ha.History)
            .HasForeignKey(ha => ha.history_id)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<HistoryAnswer>()
            .HasOne(ha => ha.History)
            .WithMany(h => h.HistoryAnswers)
            .HasForeignKey(ha => ha.history_id)
            .OnDelete(DeleteBehavior.Cascade);

    }
}