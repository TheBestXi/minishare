using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;

namespace MiniShare.Controllers
{
    [Authorize]
    [Route("Likes")]
    public class LikesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LikesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: Likes/TogglePostLike/5
        [HttpPost("TogglePostLike/{postId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePostLike(int postId)
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            // 检查帖子是否存在
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return NotFound(new { message = "帖子不存在" });
            }

            // 检查是否已经点赞
            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

            if (existingLike != null)
            {
                // 已点赞，取消点赞
                _context.PostLikes.Remove(existingLike);
                await _context.SaveChangesAsync();
                
                // 更新帖子点赞数
                post.LikeCount--;
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, isLiked = false, likeCount = post.LikeCount });
            }
            else
            {
                // 未点赞，添加点赞
                _context.PostLikes.Add(new PostLike
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                
                // 更新帖子点赞数
                post.LikeCount++;
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, isLiked = true, likeCount = post.LikeCount });
            }
        }

        // POST: Likes/CheckPostLike/5
        [HttpPost("CheckPostLike/{postId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckPostLike(int postId)
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            // 检查是否已经点赞
            var isLiked = await _context.PostLikes
                .AnyAsync(pl => pl.PostId == postId && pl.UserId == userId);

            // 获取点赞数
            var likeCount = await _context.PostLikes
                .CountAsync(pl => pl.PostId == postId);

            return Json(new { success = true, isLiked, likeCount });
        }
    }
}