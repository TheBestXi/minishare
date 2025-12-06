using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace MiniShare.Controllers
{
    [Authorize]
    public class UserPostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 我的帖子
        /// </summary>
        /// <returns>我的帖子页面</returns>
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            
            // 获取当前用户发布的帖子
            var posts = await _context.Posts
                .Where(p => p.AuthorId == userId)
                .Include(p => p.Images) // 假设帖子有图片、标题、点赞数、发布时间
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        /// <returns>当前用户ID</returns>
        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdStr);
        }
    }
}