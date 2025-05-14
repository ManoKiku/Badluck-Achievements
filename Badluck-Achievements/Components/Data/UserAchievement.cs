namespace Badluck_Achievements.Components.Data
{
    public class UserAchievement
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public User User { get; set; }
        public int AchievementId { get; set; }
        public Achievement Achievement { get; set; }
    }
}
