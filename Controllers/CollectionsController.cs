using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;

namespace MiniShare.Controllers
{
    [Authorize]
    public class CollectionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CollectionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string type = "products")
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            ViewData["ActiveTab"] = type;

            // 同时加载两类收藏，避免切换标签为空
            var productCollections = await _context.ProductFavorites
                .Where(pf => pf.UserId == userId)
                .Include(pf => pf.Product)
                .ThenInclude(p => p.Images)
                .OrderByDescending(pf => pf.CreatedAt)
                .ToListAsync();

            var postCollections = await _context.PostFavorites
                .Where(pf => pf.UserId == userId)
                .Include(pf => pf.Post)
                .ThenInclude(p => p.Author)
                .Include(pf => pf.Post)
                .ThenInclude(p => p.Images)
                .OrderByDescending(pf => pf.CreatedAt)
                .ToListAsync();

            ViewData["ProductCollections"] = productCollections;
            ViewData["PostCollections"] = postCollections;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProductFavorite(int productId)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            // 检查商品是否存在
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            // 检查是否已经收藏
            var existingFavorite = await _context.ProductFavorites
                .FirstOrDefaultAsync(pf => pf.UserId == userId && pf.ProductId == productId);

            if (existingFavorite != null)
            {
                // 已收藏，取消收藏
                _context.ProductFavorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return Json(new { isFavorite = false });
            }
            else
            {
                // 未收藏，添加收藏
                var newFavorite = new ProductFavorite
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductFavorites.Add(newFavorite);
                await _context.SaveChangesAsync();
                return Json(new { isFavorite = true });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckProductFavorite(int productId)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Json(new { isFavorite = false });
            var userId = int.Parse(userIdStr);

            // 检查是否已经收藏
            var isFavorite = await _context.ProductFavorites
                .AnyAsync(pf => pf.UserId == userId && pf.ProductId == productId);

            return Json(new { isFavorite });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckPostFavorite(int postId)
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Json(new { isFavorite = false });
            var userId = int.Parse(userIdStr);

            var isFavorite = await _context.PostFavorites
                .AnyAsync(pf => pf.UserId == userId && pf.PostId == postId);

            return Json(new { isFavorite });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePostFavorite(int postId)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            // 检查帖子是否存在
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound();

            // 检查是否已经收藏
            var existingFavorite = await _context.PostFavorites
                .FirstOrDefaultAsync(pf => pf.UserId == userId && pf.PostId == postId);

            if (existingFavorite != null)
            {
                // 已收藏，取消收藏
                _context.PostFavorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return Json(new { isFavorite = false });
            }
            else
            {
                // 未收藏，添加收藏
                var newFavorite = new PostFavorite
                {
                    UserId = userId,
                    PostId = postId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PostFavorites.Add(newFavorite);
                await _context.SaveChangesAsync();
                return Json(new { isFavorite = true });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites(string type, int id)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            if (type == "posts")
            {
                // 删除帖子收藏
                var favorite = await _context.PostFavorites
                    .FirstOrDefaultAsync(pf => pf.UserId == userId && pf.PostId == id);
                
                if (favorite != null)
                {
                    _context.PostFavorites.Remove(favorite);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // 删除商品收藏
                var favorite = await _context.ProductFavorites
                    .FirstOrDefaultAsync(pf => pf.UserId == userId && pf.ProductId == id);
                
                if (favorite != null)
                {
                    _context.ProductFavorites.Remove(favorite);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index), new { type = type });
        }
    }
}
