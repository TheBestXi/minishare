using System.ComponentModel.DataAnnotations;

namespace MiniShare.Models
{
    /// <summary>
    /// 配送方式枚举
    /// </summary>
    public enum ShippingMethod
    {
        /// <summary>
        /// 快递配送
        /// </summary>
        Express = 0,
        /// <summary>
        /// 面交
        /// </summary>
        Meetup = 1,
        /// <summary>
        /// 免运费
        /// </summary>
        FreeShipping = 2
    }

    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 999999999)]
        public decimal Price { get; set; }

        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 发货时间（小时）
        /// </summary>
        [Range(0, 999)]
        public int ShippingTimeHours { get; set; } = 24;

        /// <summary>
        /// 配送方式
        /// </summary>
        public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Express;

        /// <summary>
        /// 运费
        /// </summary>
        [Range(0, 999999999)]
        public decimal ShippingFee { get; set; } = 0;

        // 导航属性
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductComment> Comments { get; set; } = new List<ProductComment>();
    }
}