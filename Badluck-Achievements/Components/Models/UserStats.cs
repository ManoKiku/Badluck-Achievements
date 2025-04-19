namespace Badluck_Achievements.Components.Models
{
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

        public double CalculateCompetionRate()
        {
            return completedAchievements / totalAchievements;
        }
    }
}
