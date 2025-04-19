namespace Badluck_Achievements.Components.Models
{
    public class GameInfo
    {
        public ulong appId { get; set; } = 0;
        public ulong completedAchievements { get; set; } = 0;
        public ulong totalAhievements { get; set; } = 0;
    }

    public class UserStats
    {
        // Count of achievements
        public long totalAchievements { get; set; } = 0;
        // Count of games
        public long totalGames { get; set; } = 0;
        // Percentage of perfect games
        public double completedAchievements { get; set; } = 0;
        // Hours playes
        public double hoursPlayed { get; set; } = 0;
        // Rarest achievements
        public List<SteamAchievement> achievements = new List<SteamAchievement>();
        // Games
        public List<GameInfo> games = new List<GameInfo>();
        public double CalculateCompetionRate()
        {
            return completedAchievements / totalAchievements;
        }
    }
}
