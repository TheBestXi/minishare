using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniShare.Models
{
    public class PostImage
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Post))]
        public int PostId { get; set; }
        public Post? Post { get; set; }

        [Required]
        [MaxLength(255)]
        public string Url { get; set; } = string.Empty;

        public bool IsMain { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }
}