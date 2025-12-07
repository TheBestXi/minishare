using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using System.Security.Claims;

namespace MiniShare.Controllers
{
    [Authorize]
    public class UserProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 查看用户自己的商品列表
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 获取当前用户的商品（通过商品申请关联）
            // 1. 先获取用户的所有商品申请
            var userRequests = await _context.ProductRequests
                .Where(pr => pr.RequestedById == userId && pr.Status == ProductRequestStatus.Approved)
                .ToListAsync();

            // 2. 获取对应的商品ID列表
            var productIds = userRequests.Select(pr => pr.Id).ToList();

            // 3. 获取商品详情
            var query = _context.Products
                .Where(p => productIds.Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt);

            // 分页处理
            var total = await query.CountAsync();
            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // 获取每个商品的主图
            var productMainImages = new Dictionary<int, string>();
            foreach (var product in products)
            {
                var mainImage = await _context.ProductImages
                    .Where(img => img.ProductId.HasValue && img.ProductId.Value == product.Id && img.IsMain)
                    .FirstOrDefaultAsync();

                if (mainImage == null)
                {
                    // 如果没有主图，使用第一张图片
                    mainImage = await _context.ProductImages
                        .Where(img => img.ProductId.HasValue && img.ProductId.Value == product.Id)
                        .OrderBy(img => img.SortOrder)
                        .FirstOrDefaultAsync();
                }

                if (mainImage != null)
                {
                    productMainImages[product.Id] = mainImage.ImageUrl;
                }
            }

            ViewBag.ProductMainImages = productMainImages;
            ViewBag.Page = page;
            ViewBag.Total = total;
            ViewBag.PageSize = pageSize;

            return View(products);
        }

        // 编辑商品 - GET
        public async Task<IActionResult> Edit(int id)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 验证商品是否存在且属于当前用户
            // 1. 检查商品是否存在，包含图片信息
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // 2. 检查商品是否属于当前用户（通过商品申请关联）
            var productRequest = await _context.ProductRequests
                .FirstOrDefaultAsync(pr => pr.Id == product.Id && pr.RequestedById == userId && pr.Status == ProductRequestStatus.Approved);

            if (productRequest == null)
            {
                return Forbid();
            }

            // 3. 创建商品修改申请模型
            var model = new ProductRequest
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                ShippingTimeHours = product.ShippingTimeHours,
                ShippingMethod = product.ShippingMethod,
                ShippingFee = product.ShippingFee,
                OriginalProductId = product.Id,
                OriginalProduct = product
            };

            return View(model);
        }

        // 编辑商品 - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductRequest model, List<IFormFile>? productImages, string? productImagesToDelete)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 验证商品是否存在且属于当前用户
            var product = await _context.Products.FindAsync(model.OriginalProductId);
            if (product == null)
            {
                return NotFound();
            }

            // 检查商品是否属于当前用户
            var productRequest = await _context.ProductRequests
                .FirstOrDefaultAsync(pr => pr.Id == product.Id && pr.RequestedById == userId && pr.Status == ProductRequestStatus.Approved);

            if (productRequest == null)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 创建新的商品修改申请
                    var newRequest = new ProductRequest
                    {
                        Name = model.Name,
                        Price = model.Price,
                        Description = model.Description,
                        ShippingTimeHours = model.ShippingTimeHours,
                        ShippingMethod = model.ShippingMethod,
                        ShippingFee = model.ShippingFee,
                        RequestedById = userId,
                        CreatedAt = DateTime.UtcNow,
                        Status = ProductRequestStatus.Pending,
                        OriginalProductId = product.Id
                    };

                    // 保存新申请
                    _context.ProductRequests.Add(newRequest);
                    await _context.SaveChangesAsync();

                    // 处理商品图片
                    var imagesToKeep = new List<ProductImage>();
                    
                    // 如果有要删除的图片，处理删除逻辑
                    if (!string.IsNullOrEmpty(productImagesToDelete))
                    {
                        var imageIdsToDelete = productImagesToDelete.Split(',').Select(int.Parse).ToList();
                        imagesToKeep = product.Images.Where(img => !imageIdsToDelete.Contains(img.Id)).ToList();
                    }
                    else
                    {
                        imagesToKeep = product.Images.ToList();
                    }
                    
                    // 如果有图片上传，保存新图片
                    if (productImages != null && productImages.Any())
                    {
                        // 验证图片数量
                        if (imagesToKeep.Count + productImages.Count > 5)
                        {
                            ModelState.AddModelError("productImages", "最多只能上传5张图片");
                            return View(model);
                        }

                        // 保存图片
                        var savedImages = await SaveProductImagesAsync(newRequest.Id, productImages);
                        _context.ProductImages.AddRange(savedImages);
                    }
                    else if (imagesToKeep.Count > 0)
                    {
                        // 如果没有新图片上传，但有要保留的图片，复制现有图片
                        foreach (var image in imagesToKeep)
                        {
                            var copiedImage = new ProductImage
                            {
                                ProductRequestId = newRequest.Id,
                                ImageUrl = image.ImageUrl,
                                IsMain = image.IsMain,
                                SortOrder = image.SortOrder,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.ProductImages.Add(copiedImage);
                        }
                    }

                    // 保存更改
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "商品修改申请已提交，正在等待管理员审核...";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id))
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

        // 保存商品图片
        private async Task<List<ProductImage>> SaveProductImagesAsync(int productRequestId, List<IFormFile> images)
        {
            var list = new List<ProductImage>();
            var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            Directory.CreateDirectory(imagesDir);
            
            for (int i = 0; i < images.Count; i++)
            {
                var file = images[i];
                if (file.Length == 0) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowed.Contains(ext)) continue;
                if (file.Length > 5 * 1024 * 1024) continue;

                var fileName = $"product_{productRequestId}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(imagesDir, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                var imageUrl = $"/images/products/{fileName}";
                list.Add(new ProductImage 
                {
                    ProductRequestId = productRequestId,
                    ImageUrl = imageUrl,
                    IsMain = i == 0,
                    SortOrder = i
                });
            }
            return list;
        }
    }
}