using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badluck_Achievements.Components.Data
{
    public class UserAchievement
    {
        [Key]
        public int UserAchievementId { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
        [ForeignKey("Achievement")]
        public int AchievementId { get; set; }
        public Achievement Achievement { get; set; }
    }
}
