using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using MiniShare.Services;
using System.ComponentModel.DataAnnotations;

namespace MiniShare.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;

        public ProductsController(ApplicationDbContext context, IFileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();
            
            // 获取每个商品的首图
            var productIds = products.Select(p => p.Id).ToList();
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
            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            // 重定向到新的商品详情页
            return RedirectToAction("Index", "ProductDetails", new { id = id });
        }

        [Authorize]
        public IActionResult Create()
        {
            return View(new ProductRequest());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Price,Description")] ProductRequest request,
            List<IFormFile> productImages)
        {
            // 验证表单数据
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            // 验证图片数量
            if (productImages == null || productImages.Count == 0)
            {
                ModelState.AddModelError("productImages", "请至少上传一张商品图片");
                return View(request);
            }

            if (productImages.Count > 5)
            {
                ModelState.AddModelError("productImages", "最多只能上传5张商品图片");
                return View(request);
            }

            try
            {
                // 获取当前用户ID
                var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr))
                {
                    return Unauthorized();
                }

                // 设置商品申请基本信息
                request.RequestedById = int.Parse(userIdStr);
                request.Status = ProductRequestStatus.Pending;
                request.CreatedAt = DateTime.UtcNow;

                // 保存商品申请
                _context.ProductRequests.Add(request);
                await _context.SaveChangesAsync();

                // 上传图片
                var imageUrls = await _fileUploadService.UploadProductImagesAsync(productImages, request.Id);

                // 保存图片信息到数据库
                var productImageEntities = new List<ProductImage>();
                for (int i = 0; i < imageUrls.Count; i++)
                {
                    var productImage = new ProductImage
                    {
                        ProductRequestId = request.Id,
                        ImageUrl = imageUrls[i],
                        IsMain = i == 0, // 第一张图片为主图
                        SortOrder = i,
                        CreatedAt = DateTime.UtcNow
                    };
                    productImageEntities.Add(productImage);
                }

                _context.ProductImages.AddRange(productImageEntities);
                await _context.SaveChangesAsync();

                TempData["ProductRequestSubmitted"] = "您的商品申请已提交，管理员审核通过后将自动上架。";
                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError("productImages", ex.Message);
                return View(request);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "上传图片时发生错误，请稍后重试。");
                return View(request);
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult AddToCart(int id)
        {
            var cart = HttpContext.Session.GetString("cart") ?? string.Empty;
            var items = new List<int>();
            if (!string.IsNullOrEmpty(cart))
            {
                items = cart.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse).ToList();
            }
            if (!items.Contains(id)) items.Add(id);
            HttpContext.Session.SetString("cart", string.Join(',', items));
            return RedirectToAction(nameof(Cart));
        }

        [Authorize]
        public async Task<IActionResult> Cart()
        {
            var cart = HttpContext.Session.GetString("cart") ?? string.Empty;
            var ids = string.IsNullOrEmpty(cart) ? Array.Empty<int>() : cart.Split(',').Select(int.Parse).ToArray();
            var products = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

            // 获取每个商品的首图并传给视图
            var productIds = products.Select(p => p.Id).ToList();
            var mainImages = await _context.ProductImages
                .Where(img => img.ProductId.HasValue && productIds.Contains(img.ProductId.Value) && img.IsMain)
                .ToListAsync();
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
            foreach (var img in allImages)
            {
                if (img.ProductId.HasValue && !imageDict.ContainsKey(img.ProductId.Value))
                    imageDict[img.ProductId.Value] = img.ImageUrl;
            }
            ViewBag.ProductMainImages = imageDict;

            return View(products);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromForm] int[]? selectedIds)
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var cart = HttpContext.Session.GetString("cart") ?? string.Empty;
            var ids = string.IsNullOrEmpty(cart) ? new List<int>() : cart.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse).ToList();

            var toProcess = (selectedIds != null && selectedIds.Length > 0) ? selectedIds.ToList() : ids;

            foreach (var pid in toProcess)
            {
                _context.Orders.Add(new Order
                {
                    UserId = userId,
                    ProductId = pid,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            // 从购物车中移除已结账的商品a
            ids = ids.Except(toProcess).ToList();
            if (ids.Any())
                HttpContext.Session.SetString("cart", string.Join(',', ids));
            else
                HttpContext.Session.Remove("cart");

            return RedirectToAction(nameof(MyOrders));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session.GetString("cart") ?? string.Empty;
            var items = new List<int>();
            if (!string.IsNullOrEmpty(cart))
            {
                items = cart.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse).ToList();
            }
            if (items.Contains(id)) items.Remove(id);
            if (items.Any())
                HttpContext.Session.SetString("cart", string.Join(',', items));
            else
                HttpContext.Session.Remove("cart");
            return RedirectToAction(nameof(Cart));
        }

        [Authorize]
        [HttpPost]
        public IActionResult AddToFavorites(int id)
        {
            // 简单使用 Session 保存收藏列表（无需数据库迁移）
            var fav = HttpContext.Session.GetString("favorites") ?? string.Empty;
            var items = new List<int>();
            if (!string.IsNullOrEmpty(fav))
            {
                items = fav.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse).ToList();
            }
            bool added = false;
            if (!items.Contains(id))
            {
                items.Add(id);
                added = true;
                HttpContext.Session.SetString("favorites", string.Join(',', items));
            }
            return Json(new { success = true, added = added, count = items.Count });
        }

        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var orders = await _context.Orders.Include(o => o.Product).Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(orders);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            order.Status = OrderStatus.Paid;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyOrders));
        }
    }
}