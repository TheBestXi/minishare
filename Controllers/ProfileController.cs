using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

namespace MiniShare.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 个人主页
        /// </summary>
        /// <returns>个人主页视图</returns>
        public async Task<IActionResult> Index()
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 获取当前用户信息
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // 获取当前用户发布的帖子
            var posts = await _context.Posts
                .Where(p => p.AuthorId == userId)
                .Include(p => p.Author)
                .Include(p => p.Images) // 包含帖子图片
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // 获取当前用户发布的商品（通过商品请求）
            var productRequests = await _context.ProductRequests
                .Where(pr => pr.RequestedById == userId)
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();

            // 获取所有已上架的商品
            var products = await _context.Products.ToListAsync();
            var productIds = products.Select(p => p.Id).ToList();

            // 获取当前用户的商品（已上架的）
            var userProducts = await _context.ProductRequests
                .Where(pr => pr.RequestedById == userId && pr.Status == ProductRequestStatus.Approved)
                .Join(_context.Products, pr => pr.Id, p => p.Id, (pr, p) => p)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // 获取每个商品的首图
            var productImages = await _context.ProductImages
                .Where(img => img.ProductId.HasValue && userProducts.Select(p => p.Id).Contains(img.ProductId.Value))
                .OrderBy(img => img.IsMain ? 0 : 1) // 优先主图
                .ThenBy(img => img.SortOrder)
                .GroupBy(img => img.ProductId.Value)
                .Select(g => new { ProductId = g.Key, ImageUrl = g.First().ImageUrl })
                .ToListAsync();

            // 获取每个帖子的首图
            var postIds = posts.Select(p => p.Id).ToList();
            var postImages = await _context.PostImages
                .Where(img => postIds.Contains(img.PostId))
                .OrderBy(img => img.SortOrder)
                .GroupBy(img => img.PostId)
                .Select(g => new { PostId = g.Key, ImageUrl = g.First().Url }) // 使用Url属性
                .ToListAsync();

            // 构建图片字典
            var productImageDict = productImages.ToDictionary(img => img.ProductId, img => img.ImageUrl);
            var postImageDict = postImages.ToDictionary(img => img.PostId, img => img.ImageUrl);

            ViewBag.User = user;
            ViewBag.Posts = posts;
            ViewBag.Products = userProducts;
            ViewBag.ProductImages = productImageDict;
            ViewBag.PostImages = postImageDict;

            return View();
        }

        /// <summary>
        /// 编辑个人资料
        /// </summary>
        /// <returns>编辑个人资料视图</returns>
        public async Task<IActionResult> Edit()
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 获取当前用户信息
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /// <summary>
        /// 保存个人资料
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>重定向到个人主页</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser user)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            if (user.Id != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(user);
        }
    }
}