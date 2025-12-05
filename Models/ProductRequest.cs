using System.ComponentModel.DataAnnotations;

namespace MiniShare.Models
{
    public enum ProductRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public class ProductRequest
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 999999999)]
        public decimal Price { get; set; }

        public string? Description { get; set; }

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

        public ProductRequestStatus Status { get; set; } = ProductRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public string? ReviewComment { get; set; }

        public int RequestedById { get; set; }
        public ApplicationUser? RequestedBy { get; set; }

        public int? ReviewedById { get; set; }
        public ApplicationUser? ReviewedBy { get; set; }
    }
}

