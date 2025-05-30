using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badluck_Achievements.Components.Data
{
    public class Game
    {
        [Key]
        public ulong GameId { get; set; }
        public ulong TotalAchievements { get; set; }

        public ICollection<Achievement> Achievements { get; set; }
    }
}
