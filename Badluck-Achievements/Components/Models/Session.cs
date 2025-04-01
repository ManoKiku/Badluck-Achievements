namespace Badluck_Achievements.Components.Models
{
    public class Sesion
    {
        public bool isAuthenticated = false;
        public string steamId { get; set; } = string.Empty;

        public Sesion() { }

        public Sesion(bool isAuthenticated, string steamId)
        {
            this.isAuthenticated = isAuthenticated;
            this.steamId = steamId;
        }
    }
}
