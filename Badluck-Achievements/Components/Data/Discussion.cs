using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badluck_Achievements.Components.Data
{
    public class Discussion
    {
        [Key]
        public int DiscussionId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; }
        [MaxLength(1000)]
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("User")]
        public int AuthorId { get; set; }
        public User Author { get; set; }

        public ICollection<Like> Likes { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }
}
