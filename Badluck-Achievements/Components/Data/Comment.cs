using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Badluck_Achievements.Components.Data
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [ForeignKey("User")]
        public int? AuthorId { get; set; }
        public User? Author { get; set; }

        [ForeignKey("Discussion")]
        public int? DiscussionId { get; set; }
        public Discussion? Discussion { get; set; }

        public ICollection<Like> Likes { get; set; }
    }
}
