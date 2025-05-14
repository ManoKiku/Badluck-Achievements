using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badluck_Achievements.Components.Data
{
    public class Like
    {
        [Key]
        public int LikeId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("Discussion")]
        public int? DiscussionId { get; set; }
        public Discussion? Discussion { get; set; }

        [ForeignKey("Comment")]
        public int? CommentId { get; set; }
        public Comment? Comment { get; set; }
    }
}
