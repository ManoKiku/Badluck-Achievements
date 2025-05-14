using System.ComponentModel.DataAnnotations;

namespace Badluck_Achievements.Components.Data
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Required]
        public ulong SteamId { get; set; }
        public string Username { get; set; }
        public string IconPath { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsBannded { get; set; } = false;
        public DateTime BanTime { get; set; }

        public ICollection<Discussion> Discussions { get; set; }
        public ICollection<Like> Likes { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<UserAchievement> UserAchievements { get; set; }
        public ICollection<LeaderBoard> LeaderBoard { get; set; }
    }
}
