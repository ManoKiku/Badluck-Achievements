namespace Badluck_Achievements.Components.Models
{
    public class Sesion
    {
        public bool isAuthenticated { get; set; } = false;
        public string steamId { get; set; } = string.Empty;
        public string nameIdentifier { get; set; } = string.Empty;
        public string avatarUrl { get; set; } = string.Empty;

        public Sesion() { }

        public Sesion(bool isAuthenticated, string steamId, string nameIdentifier, string avatarUrl)
        {
            this.isAuthenticated = isAuthenticated;
            this.steamId = steamId;
            this.nameIdentifier = nameIdentifier;
            this.avatarUrl = avatarUrl;
        }
    }
}
