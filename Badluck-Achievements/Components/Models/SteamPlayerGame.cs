namespace Badluck_Achievements.Components.Models
{
    public class SteamPlayerGame
    {
        public uint appID { get; set; }
        public string name { get; set; }
        public int playtimeForever { get; set; } // in minutes
        public int? playtime2Weeks { get; set; } // in minutes
        public string iconUrl { get; set; }
        public string logoUrl { get; set; }
    }
}
