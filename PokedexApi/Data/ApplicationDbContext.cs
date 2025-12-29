using Microsoft.EntityFrameworkCore;
using PokedexApi.Models;

namespace PokedexApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Team> Teams { get; set; } = null!;
        public DbSet<BattleHistory> BattleHistories { get; set; } = null!;
        public DbSet<BattleSession> BattleSessions { get; set; } = null!;
        public DbSet<UserRating> UserRatings { get; set; } = null!;
        public DbSet<BattleReport> BattleReports { get; set; } = null!;
        public DbSet<MatchmakingQueue> MatchmakingQueues { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Teams)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<BattleHistory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Player1)
                      .WithMany()
                      .HasForeignKey(e => e.Player1Id)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Player2)
                      .WithMany()
                      .HasForeignKey(e => e.Player2Id)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Winner)
                      .WithMany()
                      .HasForeignKey(e => e.WinnerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Player1Team)
                      .WithMany()
                      .HasForeignKey(e => e.Player1TeamId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Player2Team)
                      .WithMany()
                      .HasForeignKey(e => e.Player2TeamId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<BattleSession>(entity =>
            {
                entity.HasKey(e => e.BattleId);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<UserRating>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<BattleReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Battle)
                      .WithMany()
                      .HasForeignKey(e => e.BattleId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ReportedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ReportedById)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<MatchmakingQueue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Team)
                      .WithMany()
                      .HasForeignKey(e => e.TeamId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.JoinedAt).HasDefaultValueSql("GETDATE()");
            });
        }
    }
}