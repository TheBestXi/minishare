using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniShare.Models
{
    public class PostFavorite
    {
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }
        public Post? Post { get; set; }

        [Required]
        public int UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

