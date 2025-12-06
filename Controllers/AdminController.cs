using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;

namespace MiniShare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Posts(string searchTerm = null, string sortBy = "createdAt", string sortOrder = "desc")
        {
            var postsQuery = _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments) // 包含评论，用于统计评论数
                .Include(p => p.Images) // 包含帖子图片
                .AsQueryable();
            
            // 搜索功能
            if (!string.IsNullOrEmpty(searchTerm))
            {
                postsQuery = postsQuery.Where(p => p.Title.Contains(searchTerm) || p.Content.Contains(searchTerm) || p.Author.UserName.Contains(searchTerm));
            }
            
            // 排序功能
            switch (sortBy)
            {
                case "title":
                    postsQuery = sortOrder == "asc" ? postsQuery.OrderBy(p => p.Title) : postsQuery.OrderByDescending(p => p.Title);
                    break;
                case "author":
                    postsQuery = sortOrder == "asc" ? postsQuery.OrderBy(p => p.Author.UserName) : postsQuery.OrderByDescending(p => p.Author.UserName);
                    break;
                case "likes":
                    postsQuery = sortOrder == "asc" ? postsQuery.OrderBy(p => p.LikeCount) : postsQuery.OrderByDescending(p => p.LikeCount);
                    break;
                case "comments":
                    postsQuery = sortOrder == "asc" ? postsQuery.OrderBy(p => p.Comments.Count) : postsQuery.OrderByDescending(p => p.Comments.Count);
                    break;
                case "createdAt":
                default:
                    postsQuery = sortOrder == "asc" ? postsQuery.OrderBy(p => p.CreatedAt) : postsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
            }
            
            var posts = await postsQuery.ToListAsync();
            
            // 将搜索和排序参数传递给视图
            ViewData["SearchTerm"] = searchTerm;
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            
            return View(posts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Posts));
        }

        public async Task<IActionResult> Products(string searchTerm = null, string sortBy = "createdAt", string sortOrder = "desc")
        {
            var productsQuery = _context.Products
                .Include(p => p.Images) // 包含商品图片
                .AsQueryable();
            
            // 搜索功能
            if (!string.IsNullOrEmpty(searchTerm))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }
            
            // 排序功能
            switch (sortBy)
            {
                case "name":
                    productsQuery = sortOrder == "asc" ? productsQuery.OrderBy(p => p.Name) : productsQuery.OrderByDescending(p => p.Name);
                    break;
                case "price":
                    productsQuery = sortOrder == "asc" ? productsQuery.OrderBy(p => p.Price) : productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "createdAt":
                default:
                    productsQuery = sortOrder == "asc" ? productsQuery.OrderBy(p => p.CreatedAt) : productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
            }
            
            var products = await productsQuery.ToListAsync();
            
            // 将搜索和排序参数传递给视图
            ViewData["SearchTerm"] = searchTerm;
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            
            return View(products);
        }

        public IActionResult CreateProduct()
        {
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            if (!ModelState.IsValid) return View(product);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        public async Task<IActionResult> ProductRequests()
        {
            var requests = await _context.ProductRequests
                .Include(r => r.RequestedBy)
                .Include(r => r.ReviewedBy)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await _context.ProductRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (request.Status != ProductRequestStatus.Pending)
            {
                TempData["RequestMessage"] = "该申请已处理。";
                return RedirectToAction(nameof(ProductRequests));
            }

            var adminId = GetCurrentUserId();
            request.Status = ProductRequestStatus.Approved;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedById = adminId;

            // 创建商品
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price,
                Description = request.Description,
                ShippingTimeHours = request.ShippingTimeHours,
                ShippingMethod = request.ShippingMethod,
                ShippingFee = request.ShippingFee,
                CreatedAt = DateTime.UtcNow
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // 将商品申请的图片关联到新创建的商品
            var requestImages = await _context.ProductImages
                .Where(img => img.ProductRequestId == request.Id)
                .ToListAsync();
            
            foreach (var img in requestImages)
            {
                img.ProductId = product.Id;
                img.ProductRequestId = null; // 清除申请关联
            }
            
            await _context.SaveChangesAsync();
            TempData["RequestMessage"] = $"已通过 {request.Name} 的上架申请。";
            return RedirectToAction(nameof(ProductRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id, string? reviewComment)
        {
            var request = await _context.ProductRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (request.Status != ProductRequestStatus.Pending)
            {
                TempData["RequestMessage"] = "该申请已处理。";
                return RedirectToAction(nameof(ProductRequests));
            }

            var adminId = GetCurrentUserId();
            request.Status = ProductRequestStatus.Rejected;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedById = adminId;
            request.ReviewComment = reviewComment;
            await _context.SaveChangesAsync();

            TempData["RequestMessage"] = $"已驳回 {request.Name} 的上架申请。";
            return RedirectToAction(nameof(ProductRequests));
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) throw new InvalidOperationException("无法获取当前管理员信息");
            return int.Parse(userIdStr);
        }
    }
}