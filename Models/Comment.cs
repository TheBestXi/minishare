using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniShare.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Post))]
        public int PostId { get; set; }
        public Post? Post { get; set; }

        [Required]
        [MaxLength(50)]
        public string AuthorName { get; set; } = string.Empty;

        // 关联到用户（如果登录用户评论）
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}