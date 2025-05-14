namespace Badluck_Achievements.Components.Models
{
    public class GameInfo
    {
        public ulong AppId { get; set; } = 0;
        public ulong CompletedAchievements { get; set; } = 0;
        public ulong TotalAhievements { get; set; } = 0;
    }

    public class UserStats
    {
        // Count of achievements
        public long TotalAchievements { get; set; } = 0;
        // Count of games
        public long TotalGames { get; set; } = 0;
        // Percentage of perfect games
        public double CompletedAchievements { get; set; } = 0;
        // Hours playes
        public double HoursPlayed { get; set; } = 0;
        // Rarest achievements
        public List<SteamAchievement> Achievements = new List<SteamAchievement>();
        // Games
        public List<GameInfo> Games = new List<GameInfo>();

        public double CalculateCompetionRate() => CompletedAchievements / TotalAchievements;
    }
}
