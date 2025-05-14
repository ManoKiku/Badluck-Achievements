using Badluck_Achievements.Components.Models;
using Microsoft.EntityFrameworkCore;
using SteamWebAPI2.Interfaces;

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
        public DbSet<Discussion> Discussions => Set<Discussion>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<Comment> Comments => Set<Comment>();

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
                  .HasForeignKey(d => d.AchievementId)
                  .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.User)
                  .WithMany(u => u.UserAchievements)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Discussion>(entity =>
            {
                entity.HasOne(d => d.Author)
                  .WithMany(u => u.Discussions)
                  .HasForeignKey(d => d.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Comment>(entity =>
            {
                entity.HasOne(d => d.Author)
                  .WithMany(u => u.Comments)
                  .HasForeignKey(d => d.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Discussion)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(d => d.DiscussionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Like>(entity =>
            {
                entity.HasOne(d => d.User)
                  .WithMany(u => u.Likes)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Discussion)
                  .WithMany(u => u.Likes)
                  .HasForeignKey(d => d.DiscussionId)
                  .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Comment)
                  .WithMany(u => u.Likes)
                  .HasForeignKey(d => d.CommentId)
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

        public async Task CalculateUserScore(ulong steamId)
        {
            var User = await Users
                .Include(d => d.UserAchievements)
                .Include(d => d.LeaderBoard)
                .FirstOrDefaultAsync(x => x.SteamId == steamId)
                ?? throw new InvalidDataException("There is no such user in DB!");

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
                LeaderBoards.Add(new LeaderBoard
                {
                    UserId = User.UserId,
                    Score = finalScore
                });
            }
            else
            {
                leaderboard.Score = finalScore;
            }

            await SaveChangesAsync();
        }

        public async Task UpdateUserAchievements(ulong steamId, UserStats stats)
        {
            var User = await Users
                .Include(d => d.UserAchievements)
                .Include(d => d.LeaderBoard)
                .FirstOrDefaultAsync(x => x.SteamId == steamId) 
                ?? throw new InvalidDataException("There is no such user in DB!");

            List<Achievement> achievements = new List<Achievement>();

            foreach (var achievement in stats.Achievements)
            {
                var a = await Achievements.FirstOrDefaultAsync(x => x.Name == achievement.Name && x.GameId == achievement.AppId && x.Bit == achievement.Bit);
                if (a == null)
                {
                    achievements.Add(new Achievement
                    {
                        GameId = achievement.AppId,
                        Name = achievement.Name,
                        Rarity = achievement.AchievePercentage,
                        Bit = achievement.Bit,
                        IconUrl = achievement.IconUrl
                    });
                }
            }

            await Achievements.AddRangeAsync(achievements);
            await SaveChangesAsync();

            List<UserAchievement> userAchievements = new List<UserAchievement>();
            foreach (var achievement in stats.Achievements)
            {
                var i = await Achievements.FirstOrDefaultAsync(x => x.Name == achievement.Name && x.GameId == achievement.AppId && x.Bit == achievement.Bit);

                userAchievements.Add(new UserAchievement
                {
                    AchievementId = i.AchievementId,
                    UserId = User.UserId
                });
            }

            await UserAchievements.AddRangeAsync(userAchievements);
            await SaveChangesAsync();
            await CalculateUserScore(steamId);
        }
    }
}
