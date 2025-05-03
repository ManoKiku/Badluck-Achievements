namespace Badluck_Achievements.Components.Models
{

    public class SteamPlayerGame : SteamGame
    {
        public SteamPlayerGame() { }

        public double playtimeForever { get; set; } = 0; // in hours
        public double? playtime2Weeks { get; set; } = 0; // in hours
        public string iconUrl { get; set; } = string.Empty;
        public ulong completedAchievements { get; set; } = 0;
    }
}
