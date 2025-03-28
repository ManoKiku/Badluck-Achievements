namespace Badluck_Achievements.Components.Models
{
    class SteamGame
    {
        //Game name
        public string name { get; set; } = string.Empty;
        //Img url
        public string img { get; set; } = string.Empty;

        public SteamGame(string name, string img)
        {
            this.name = name;
            this.img = img;
        }
    }
}
