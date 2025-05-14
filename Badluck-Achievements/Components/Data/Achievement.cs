using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badluck_Achievements.Components.Data
{
    public class Achievement
    {
        [Key]
        public int AchievementId { get; set; }
        public string Name { get; set; }
        public double Rarity { get; set; }
        public uint Bit { get; set; }
        public string IconUrl { get; set; }
        public ulong GameId { get; set; }

        public ICollection<UserAchievement> UserAchievements { get; set; }
    }
}
