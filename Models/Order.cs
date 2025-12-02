using System.ComponentModel.DataAnnotations.Schema;

namespace MiniShare.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Paid = 1
    }

    public class Order
    {
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}