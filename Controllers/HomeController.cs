using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using System.Diagnostics;

namespace MiniShare.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 获取最新的产品和文章（各4个）
            var latestProducts = await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();
            
            // 获取每个商品的首图
            var productIds = latestProducts.Select(p => p.Id).ToList();
            var mainImages = await _context.ProductImages
                .Where(img => img.ProductId.HasValue && productIds.Contains(img.ProductId.Value) && img.IsMain)
                .ToListAsync();
            
            // 如果没有主图，则使用第一张图片
            var allImages = await _context.ProductImages
                .Where(img => img.ProductId.HasValue && productIds.Contains(img.ProductId.Value))
                .OrderBy(img => img.SortOrder)
                .GroupBy(img => img.ProductId.Value)
                .Select(g => g.First())
                .ToListAsync();
            
            var imageDict = new Dictionary<int, string>();
            foreach (var img in mainImages)
            {
                if (img.ProductId.HasValue)
                    imageDict[img.ProductId.Value] = img.ImageUrl;
            }
            
            // 补充没有主图的商品
            foreach (var img in allImages)
            {
                if (img.ProductId.HasValue && !imageDict.ContainsKey(img.ProductId.Value))
                    imageDict[img.ProductId.Value] = img.ImageUrl;
            }
            
            ViewBag.ProductMainImages = imageDict;

            var latestPosts = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Images.OrderBy(img => img.SortOrder))
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            // 获取每个帖子的主图
            var postMainImages = new Dictionary<int, string>();
            foreach (var post in latestPosts)
            {
                var mainImage = post.Images.FirstOrDefault(img => img.IsMain) 
                    ?? post.Images.FirstOrDefault();
                if (mainImage != null)
                {
                    postMainImages[post.Id] = mainImage.Url;
                }
            }
            ViewBag.PostMainImages = postMainImages;

            ViewBag.LatestProducts = latestProducts;
            ViewBag.LatestPosts = latestPosts;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}