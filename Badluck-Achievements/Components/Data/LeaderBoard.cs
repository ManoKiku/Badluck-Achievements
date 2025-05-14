using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badluck_Achievements.Components.Data
{
    public class LeaderBoard
    {
        [Key]
        public int LeaderBoardId { get; set; }
        public double Score { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
