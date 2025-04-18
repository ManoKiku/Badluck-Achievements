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

        public double CalculateCompetionRate()
        {
            return completedAchievements / totalAchievements;
        }
    }
}
