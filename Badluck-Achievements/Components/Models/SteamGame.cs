namespace Badluck_Achievements.Components.Models
{
    class SteamGame
    {
        // Game name
        public string name { get; set; } = string.Empty;
        // Img url
        public string img { get; set; } = string.Empty;
        // Achievments count
        public uint achievmentsCount { get; set; } = 0;
        // Current players
        public uint playersCount { get; set; } = 0;


        public SteamGame(string name, string img, uint achievmentsCount, uint playersCount)
        {
            this.name = name;
            this.img = img;
            this.achievmentsCount = achievmentsCount;
            this.playersCount = playersCount;
        }
    }
}
