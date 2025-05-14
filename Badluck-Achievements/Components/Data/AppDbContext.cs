using Badluck_Achievements.Components.Models;
using Microsoft.EntityFrameworkCore;

namespace Badluck_Achievements.Components.Data
{
    public class AppDbContext : DbContext
    {

        private readonly double minimumScore = 10;
        private readonly double scoreMultiplayer = 3;

        public DbSet<User> Users => Set<User>();
        public DbSet<LeaderBoard> LeaderBoards => Set<LeaderBoard>();
        public DbSet<Achievement> Achievements => Set<Achievement>();
        public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

        public async Task CalculateUserScore(ulong steamId)
        {
            var User = await this.Users
                .Include(d => d.UserAchievements)
                .Include(d => d.LeaderBoard)
                .FirstOrDefaultAsync(x => x.Id == steamId);

            if (User == null)
            {
                throw new InvalidDataException("There is no such user in DB!");
            }

            var achievedAchievements = User.UserAchievements
                .Select(x => x.Achievement);

            double finalScore = 0;
            foreach (var a in achievedAchievements)
            {
                for (int i = 7; i >= 0; i--)
                {
                    if (a.Rarity <= 100 / Math.Pow(2, i))
                    {
                        finalScore += minimumScore * Math.Pow(scoreMultiplayer, i - 1);
                        break;
                    }
                }
            }

            var leaderboard = User.LeaderBoard.FirstOrDefault();

            if (leaderboard == null)
            {
                this.LeaderBoards.Add(new LeaderBoard
                {
                    UserId = User.Id,
                    Score = finalScore
                });
            }
            else
            {
                leaderboard.Score = finalScore;
            }

            await this.SaveChangesAsync();
        }

        public async Task UpdateUserAchievements(ulong steamId, UserStats stats)
        {
            var User = await this.Users
                .Include(d => d.UserAchievements)
                .Include(d => d.LeaderBoard)
                .FirstOrDefaultAsync(x => x.Id == steamId);

            if (User == null)
            {
                throw new InvalidDataException("There is no such user in DB!");
            }
                 
            foreach (var achievement in stats.achievements)
            {
                var i = await this.Achievements.FirstOrDefaultAsync(x => x.Name == achievement.name && x.GameId == achievement.appId);
                if (i == null)
                {
                    var entity = this.Achievements.Add(new Achievement
                    {
                        GameId = achievement.appId,
                        Name = achievement.name,
                        Rarity = achievement.achievePercentage,
                    }).Entity;

                    await this.UserAchievements.AddAsync(
                       new UserAchievement
                       {
                           UserId = User.Id,
                           AchievementId = entity.Id
                       }
                    );
                }
            }

            await this.SaveChangesAsync();
            await CalculateUserScore(steamId);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<LeaderBoard>(entity =>
            {
                entity.HasOne(d => d.User)
                  .WithMany(u => u.LeaderBoard)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserAchievement>(entity =>
            {
                entity.HasOne(d => d.Achievement)
                  .WithMany(u => u.UserAchievements)
                  .HasForeignKey(d => d.Id)
                  .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<string>()
                .HaveMaxLength(200);

            base.ConfigureConventions(configurationBuilder);
        }
    }
}
