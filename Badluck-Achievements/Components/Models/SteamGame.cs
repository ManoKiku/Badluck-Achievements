namespace Badluck_Achievements.Components.Models
{
    public class SteamGame
    {
        // Game name
        public string name { get; set; } = string.Empty;
        // Img url
        public string img { get; set; } = string.Empty;
        // Achievments count
        public ulong achievementsCount { get; set; } = 0;
        // Current players
        public ulong playersCount { get; set; } = 0;

        public SteamGame() { }
        public SteamGame(string name, string img, uint achievementsCount, uint playersCount)
        {
            this.name = name;
            this.img = img;
            this.achievementsCount = achievementsCount;
            this.playersCount = playersCount;
        }
    }
}
