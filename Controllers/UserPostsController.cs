using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using System.Security.Claims;

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

        // 查看用户自己的帖子列表
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 获取当前用户的帖子
            var query = _context.Posts
                .Where(p => p.AuthorId == userId)
                .Include(p => p.Author)
                .Include(p => p.Images.OrderBy(img => img.SortOrder))
                .OrderByDescending(p => p.CreatedAt);

            // 分页处理
            var total = await query.CountAsync();
            var posts = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // 获取每个帖子的主图
            var postMainImages = new Dictionary<int, string>();
            foreach (var post in posts)
            {
                var mainImage = post.Images.FirstOrDefault(img => img.IsMain) 
                    ?? post.Images.FirstOrDefault();
                if (mainImage != null)
                {
                    postMainImages[post.Id] = mainImage.Url;
                }
            }
            ViewBag.PostMainImages = postMainImages;

            // 分页参数
            ViewBag.Page = page;
            ViewBag.Total = total;
            ViewBag.PageSize = pageSize;

            return View(posts);
        }

        // 编辑帖子 - GET
        public async Task<IActionResult> Edit(int id)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 获取帖子，确保是用户自己的帖子
            var post = await _context.Posts
                .Include(p => p.Images.OrderBy(img => img.SortOrder))
                .FirstOrDefaultAsync(p => p.Id == id && p.AuthorId == userId);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // 编辑帖子 - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Post model, List<IFormFile>? images, string? imagesToDelete)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 验证帖子是否存在且属于当前用户
            var existingPost = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.AuthorId == userId);

            if (existingPost == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 更新帖子基本信息
                    existingPost.Title = model.Title;
                    existingPost.Content = model.Content;

                    // 处理图片删除
                    if (!string.IsNullOrEmpty(imagesToDelete))
                    {
                        var imageIdsToDelete = imagesToDelete.Split(',').Select(int.Parse).ToList();
                        var imagesToRemove = existingPost.Images.Where(img => imageIdsToDelete.Contains(img.Id)).ToList();
                        _context.PostImages.RemoveRange(imagesToRemove);
                    }

                    // 处理图片上传
                    if (images != null && images.Count > 0)
                    {
                        // 验证图片数量（现有图片数 - 要删除的图片数 + 新上传的图片数 <= 9）
                        int currentImageCount = existingPost.Images.Count;
                        int newImageCount = images.Count;
                        int maxAllowed = 9;
                        
                        if (currentImageCount + newImageCount > maxAllowed)
                        {
                            ModelState.AddModelError("images", $"最多只能上传{maxAllowed}张图片");
                            return View(model);
                        }

                        // 保存新图片
                        var savedImages = await SaveImagesAsync(model.Id, images);
                        _context.PostImages.AddRange(savedImages);
                    }

                    // 保存更改
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Posts.Any(e => e.Id == model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(model);
        }

        // 保存图片
        private async Task<List<PostImage>> SaveImagesAsync(int postId, List<IFormFile> images)
        {
            var list = new List<PostImage>();
            var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "posts");
            Directory.CreateDirectory(imagesDir);
            
            for (int i = 0; i < images.Count; i++)
            {
                var file = images[i];
                if (file.Length == 0) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowed.Contains(ext)) continue;
                if (file.Length > 5 * 1024 * 1024) continue;

                var fileName = $"post_{postId}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(imagesDir, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                var url = $"/images/posts/{fileName}";
                list.Add(new PostImage 
                {
                    PostId = postId, 
                    Url = url,
                    IsMain = i == 0, // 第一张图片为主图
                    SortOrder = i
                });
            }
            return list;
        }
    }
}