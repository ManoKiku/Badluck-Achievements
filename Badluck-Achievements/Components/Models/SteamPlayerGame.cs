namespace Badluck_Achievements.Components.Models
{

    public class SteamPlayerGame : SteamGame
    {
        public SteamPlayerGame() { }

        public double PlaytimeForever { get; set; } = 0; // in hours
        public double? Playtime2Weeks { get; set; } = 0; // in hours
        public string IconUrl { get; set; } = string.Empty;
        public ulong CompletedAchievements { get; set; } = 0;
    }
}
