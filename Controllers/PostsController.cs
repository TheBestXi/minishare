using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;

namespace MiniShare.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var query = _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Images.OrderBy(img => img.SortOrder))
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // 获取每个帖子的主图
            var postMainImages = new Dictionary<int, string>();
            foreach (var post in items)
            {
                var mainImage = post.Images.FirstOrDefault(img => img.IsMain) 
                    ?? post.Images.FirstOrDefault();
                if (mainImage != null)
                {
                    postMainImages[post.Id] = mainImage.Url;
                }
            }
            ViewBag.PostMainImages = postMainImages;

            ViewBag.Page = page;
            ViewBag.Total = total;
            ViewBag.PageSize = pageSize;
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Images.OrderBy(img => img.SortOrder))
                .Include(p => p.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();

            // 获取当前用户信息（如果已登录）
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? currentUserId = null;
            if (!string.IsNullOrEmpty(userIdStr))
            {
                currentUserId = int.Parse(userIdStr);
            }

            // 检查当前用户是否已点赞
            bool isLiked = false;
            bool isFavorited = false;
            if (currentUserId.HasValue)
            {
                isLiked = await _context.PostLikes
                    .AnyAsync(l => l.PostId == id && l.UserId == currentUserId.Value);
                isFavorited = await _context.PostFavorites
                    .AnyAsync(f => f.PostId == id && f.UserId == currentUserId.Value);
            }

            ViewBag.IsLiked = isLiked;
            ViewBag.IsFavorited = isFavorited;
            ViewBag.CurrentUserId = currentUserId;

            return View(post);
        }

        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromForm] Post model,
            [FromForm(Name = "images")] List<IFormFile>? images)
        {
            if (!ModelState.IsValid) return View(model);

            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            model.AuthorId = int.Parse(userIdStr);
            model.CreatedAt = DateTime.UtcNow;

            _context.Posts.Add(model);
            await _context.SaveChangesAsync();

            if (images != null && images.Count > 0)
            {
                if (images.Count > 9)
                {
                    ModelState.AddModelError("images", "最多只能上传9张图片");
                    return View(model);
                }
                var saved = await SaveImagesAsync(model.Id, images);
                _context.PostImages.AddRange(saved);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Search(string keyword)
        {
            keyword = keyword ?? string.Empty;
            var results = await _context.Posts
                .Where(p => p.Title.Contains(keyword) || p.Content.Contains(keyword))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            ViewBag.Keyword = keyword;
            return View("Index", results);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Like(int id)
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            // 检查是否已点赞
            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == id && l.UserId == userId);

            if (existingLike != null)
            {
                // 取消点赞
                _context.PostLikes.Remove(existingLike);
                post.LikeCount = Math.Max(0, post.LikeCount - 1);
                await _context.SaveChangesAsync();
                return Json(new { success = true, likeCount = post.LikeCount, liked = false });
            }
            else
            {
                // 添加点赞
                _context.PostLikes.Add(new PostLike
                {
                    PostId = id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
                post.LikeCount += 1;
                await _context.SaveChangesAsync();
                return Json(new { success = true, likeCount = post.LikeCount, liked = true });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Favorite(int id)
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            // 检查是否已收藏
            var existingFavorite = await _context.PostFavorites
                .FirstOrDefaultAsync(f => f.PostId == id && f.UserId == userId);

            if (existingFavorite != null)
            {
                // 取消收藏
                _context.PostFavorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, favorited = false });
            }
            else
            {
                // 添加收藏
                _context.PostFavorites.Add(new PostFavorite
                {
                    PostId = id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return Json(new { success = true, favorited = true });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Comment(int postId, string? authorName, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) 
            {
                TempData["CommentError"] = "评论内容不能为空";
                return RedirectToAction(nameof(Details), new { id = postId });
            }

            var exists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!exists) return NotFound();

            // 获取当前用户信息（如果已登录）
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            string displayName = "匿名";

            if (!string.IsNullOrEmpty(userIdStr))
            {
                userId = int.Parse(userIdStr);
                var user = await _context.Users.FindAsync(userId.Value);
                displayName = user?.UserName ?? user?.Email ?? "匿名";
            }
            else if (!string.IsNullOrWhiteSpace(authorName))
            {
                displayName = authorName.Trim();
            }

            var comment = new Comment
            {
                PostId = postId,
                AuthorName = displayName,
                UserId = userId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "评论发表成功！";
            return RedirectToAction(nameof(Details), new { id = postId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

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

        [Authorize] 
        [HttpPost] 
        [ValidateAntiForgeryToken] // 添加防伪标记验证
        public async Task<IActionResult> DeleteComment(int id) 
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value; 
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(); 
            var userId = int.Parse(userIdStr); 

            var comment = await _context.Comments.FindAsync(id); 
            if (comment == null) return NotFound(); 
            
            // 确保用户只能删除自己的评论
            if (comment.UserId != userId && !User.IsInRole("Admin")) 
            {
                TempData["CommentError"] = "您没有权限删除此评论";
                return Forbid(); 
            } 
            
            _context.Comments.Remove(comment); 
            await _context.SaveChangesAsync(); 
            return RedirectToAction(nameof(Details), new { id = comment.PostId }); 
        }
    }
}