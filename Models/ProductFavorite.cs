using System.ComponentModel.DataAnnotations;

namespace MiniShare.Models
{
    public class ProductFavorite
    {
        [Key]
        public int Id { get; set; }
        
        // 关联到用户
        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        
        // 关联到商品
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        
        // 收藏时间
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}