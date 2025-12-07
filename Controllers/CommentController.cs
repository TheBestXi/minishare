using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;

namespace MiniShare.Controllers
{
    [Authorize]
    [Route("Comments")]
    public class CommentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CommentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: Comments/CreateProductComment
        [HttpPost("CreateProductComment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProductComment(int productId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { message = "评论内容不能为空" });
            }

            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var comment = new ProductComment
            {
                ProductId = productId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductComments.Add(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "评论发布成功" });
        }

        // POST: Comments/CreatePostComment
        [HttpPost("CreatePostComment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePostComment(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { message = "评论内容不能为空" });
            }

            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "评论发布成功" });
        }

        // POST: Comments/DeleteProductComment/5
        [HttpPost("DeleteProductComment/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductComment(int id)
        {
            var comment = await _context.ProductComments
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || int.Parse(userIdStr) != comment.UserId)
            {
                return Unauthorized();
            }

            _context.ProductComments.Remove(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "评论已删除" });
        }

        // POST: Comments/DeletePostComment/5
        [HttpPost("DeletePostComment/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePostComment(int id)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || int.Parse(userIdStr) != comment.UserId)
            {
                return Unauthorized();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "评论已删除" });
        }
    }
}