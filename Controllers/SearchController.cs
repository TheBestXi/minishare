using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;

namespace MiniShare.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Search
        public IActionResult Index(string q = "", string type = "all")
        {
            ViewData["SearchQuery"] = q;
            ViewData["SearchType"] = type;
            return View();
        }

        // GET: Search/Results
        [HttpGet("Results")]
        public async Task<IActionResult> Results(string q, string type = "all", int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { success = false, message = "搜索关键词不能为空" });
            }

            IQueryable<object> results = null;
            int totalCount = 0;

            switch (type.ToLower())
            {
                case "products":
                    // 搜索商品
                    var productsQuery = _context.Products
                        .Include(p => p.Images)
                        .Where(p => p.Name.Contains(q) || p.Description.Contains(q))
                        .OrderByDescending(p => p.CreatedAt);
                    
                    totalCount = await productsQuery.CountAsync();
                    var products = await productsQuery
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    
                    return Json(new {
                        success = true,
                        type = "products",
                        results = products.Select(p => new {
                            id = p.Id,
                            name = p.Name,
                            price = p.Price,
                            imageUrl = p.Images.FirstOrDefault()?.ImageUrl,
                            createdAt = p.CreatedAt,
                            description = p.Description
                        }),
                        totalCount,
                        page,
                        pageSize
                    });
                    
                case "posts":
                    // 搜索帖子
                    var postsQuery = _context.Posts
                        .Include(p => p.Author)
                        .Include(p => p.Images)
                        .Where(p => p.Title.Contains(q) || p.Content.Contains(q))
                        .OrderByDescending(p => p.CreatedAt);
                    
                    totalCount = await postsQuery.CountAsync();
                    var posts = await postsQuery
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    
                    return Json(new {
                            success = true,
                            type = "posts",
                            results = posts.Select(p => new {
                                id = p.Id,
                                title = p.Title,
                                content = p.Content,
                                authorName = p.Author?.UserName ?? p.Author?.Email,
                                imageUrl = p.Images.FirstOrDefault()?.Url,
                                likeCount = p.LikeCount,
                                commentCount = p.Comments.Count,
                                createdAt = p.CreatedAt
                            }),
                            totalCount,
                            page,
                            pageSize
                        });
                    
                case "all":
                default:
                    // 搜索所有内容
                    var allProducts = await _context.Products
                        .Include(p => p.Images)
                        .Where(p => p.Name.Contains(q) || p.Description.Contains(q))
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(pageSize)
                        .ToListAsync();
                    
                    var allPosts = await _context.Posts
                        .Include(p => p.Author)
                        .Include(p => p.Images)
                        .Where(p => p.Title.Contains(q) || p.Content.Contains(q))
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(pageSize)
                        .ToListAsync();
                    
                    return Json(new {
                        success = true,
                        type = "all",
                        products = allProducts.Select(p => new {
                            id = p.Id,
                            name = p.Name,
                            price = p.Price,
                            imageUrl = p.Images.FirstOrDefault()?.ImageUrl,
                            createdAt = p.CreatedAt,
                            description = p.Description
                        }),
                        posts = allPosts.Select(p => new {
                            id = p.Id,
                            title = p.Title,
                            content = p.Content,
                            authorName = p.Author?.UserName ?? p.Author?.Email,
                            imageUrl = p.Images.FirstOrDefault()?.Url,
                            likeCount = p.LikeCount,
                            commentCount = p.Comments.Count,
                            createdAt = p.CreatedAt
                        }),
                        totalCount = allProducts.Count + allPosts.Count,
                        page,
                        pageSize
                    });
            }
        }
    }
}