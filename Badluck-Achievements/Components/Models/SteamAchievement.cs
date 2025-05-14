namespace Badluck_Achievements.Components.Models
{
    public class SteamAchievement
    {
        // Achievement api name
        public string ApiName { get; set; } = string.Empty;
        // Achievment name
        public string Name { get; set; } = string.Empty;
        // Is achievment achived
        public bool IsAchieved { get; set; } = false;
        // Global achieve percentage
        public double AchievePercentage { get; set; } = 0;
        // Unlock time of achievement
        public DateTime? UnlockTime { get; set; } = DateTime.MinValue;
        // Url of achievement icon
        public string? IconUrl { get; set; } = string.Empty;
        // App id
        public ulong AppId { get; set; } = 0;
        // achievement bit 
        public uint Bit { get; set; } = 0;
    }
}
