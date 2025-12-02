using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;

namespace MiniShare.Controllers
{
    public class ProductDetailsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductDetailsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 商品详情页
        public async Task<IActionResult> Index(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (product == null) return NotFound();

            // 获取商品图片（按排序顺序）
            var productImages = await _context.ProductImages
                .Where(img => img.ProductId == id)
                .OrderBy(img => img.SortOrder)
                .ToListAsync();

            // 获取首图（主图）
            var mainImage = productImages.FirstOrDefault(img => img.IsMain) 
                ?? productImages.FirstOrDefault();

            // 获取评论（包含用户信息）
            var comments = await _context.ProductComments
                .Include(c => c.User)
                .Where(c => c.ProductId == id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // 计算平均评分
            var averageRating = comments.Any() 
                ? comments.Average(c => c.Rating) 
                : 0;

            ViewBag.ProductImages = productImages;
            ViewBag.MainImage = mainImage;
            ViewBag.Comments = comments;
            ViewBag.AverageRating = averageRating;
            ViewBag.CommentCount = comments.Count;

            return View(product);
        }

        // 提交评论
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int productId, int rating, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["CommentError"] = "评论内容不能为空";
                return RedirectToAction(nameof(Index), new { id = productId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["CommentError"] = "评分必须在1-5之间";
                return RedirectToAction(nameof(Index), new { id = productId });
            }

            var userIdStr = User.FindFirst("sub")?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var comment = new ProductComment
            {
                ProductId = productId,
                UserId = int.Parse(userIdStr),
                Rating = rating,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "评论提交成功！";
            return RedirectToAction(nameof(Index), new { id = productId });
        }

        // 删除评论（仅评论者本人或管理员）
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.ProductComments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (comment == null) return NotFound();

            var userIdStr = User.FindFirst("sub")?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = int.Parse(userIdStr);
            var isAdmin = User.IsInRole("Admin");

            // 只有评论者本人或管理员可以删除
            if (comment.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            _context.ProductComments.Remove(comment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "评论已删除";
            return RedirectToAction(nameof(Index), new { id = comment.ProductId });
        }
    }
}

