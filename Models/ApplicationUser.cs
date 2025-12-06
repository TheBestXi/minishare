using Microsoft.AspNetCore.Identity;

namespace MiniShare.Models
{
    /// <summary>
    /// 性别枚举
    /// </summary>
    public enum Gender
    {
        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 男性
        /// </summary>
        Male = 1,
        /// <summary>
        /// 女性
        /// </summary>
        Female = 2
    }

    public class ApplicationUser : IdentityUser<int>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; } // 个人简介
        public Gender Gender { get; set; } = Gender.Unknown;
        public DateTime? Birthday { get; set; }
        public string? Major { get; set; } // 专业
        public string? School { get; set; } // 院校
    }
}