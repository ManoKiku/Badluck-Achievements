namespace Badluck_Achievements.Components.Data
{
    public class Achievement
    {
        public int Id { get; set; }
        public ulong GameId { get; set; }
        public string Name { get; set; }
        public double Rarity { get; set; }
        public uint Bit { get; set; }

        public ICollection<UserAchievement> UserAchievements { get; set; }
    }
}
