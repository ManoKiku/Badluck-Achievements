namespace Badluck_Achievements.Components.Data
{
    public class User
    {
        public ulong Id { get; set; }

        public ICollection<UserAchievement> UserAchievements { get; set; }
        public ICollection<LeaderBoard> LeaderBoard { get; set; }
    }
}
