namespace Badluck_Achievements.Components.Models
{
    public class SteamAchievement
    {
        // Achievment name
        public string name { get; set; } = string.Empty;
        // Is achievment achived
        public bool isAchieved { get; set; } = false;
        // Global achieve percentage
        public double achievePercentage { get; set; } = 0;
        // Unlock time of achievement
        public DateTime? unlockTime { get; set; } = DateTime.MinValue;
        // Url of achievement icon
        public string iconUrl { get; set; } = string.Empty;
    }
}
