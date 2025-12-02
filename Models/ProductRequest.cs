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

