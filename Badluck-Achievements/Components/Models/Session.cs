using Badluck_Achievements.Components.Data;

namespace Badluck_Achievements.Components.Models
{
    public class Sesion
    {
        public bool IsAuthenticated { get; set; } = false;
        public string SteamId { get; set; } = string.Empty;
        public string NameIdentifier { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;

        public Sesion() { }

        public Sesion(bool isAuthenticated, string steamId, string nameIdentifier, string avatarUrl)
        {
            this.IsAuthenticated = isAuthenticated;
            this.SteamId = steamId;
            this.NameIdentifier = nameIdentifier;
            this.AvatarUrl = avatarUrl;
        }
    }
}
