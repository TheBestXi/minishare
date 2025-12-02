using Microsoft.AspNetCore.Identity;

namespace MiniShare.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}