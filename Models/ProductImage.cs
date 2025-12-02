using System.ComponentModel.DataAnnotations;

namespace MiniShare.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        
        // 关联到商品ID（商品审核通过后使用）
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
        
        // 关联到商品申请ID（审核前使用）
        public int? ProductRequestId { get; set; }
        public ProductRequest? ProductRequest { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string ImageUrl { get; set; } = string.Empty;
        
        public bool IsMain { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}