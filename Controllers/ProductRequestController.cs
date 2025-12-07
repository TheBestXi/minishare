using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;

namespace MiniShare.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/ProductRequests")]
    public class ProductRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/ProductRequests
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var requests = await _context.ProductRequests
                .Include(r => r.RequestedBy)
                .Include(r => r.ReviewedBy)
                .Include(r => r.OriginalProduct)
                .Include(r => r.ProductImages)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View("~/Views/Admin/ProductRequest/Index.cshtml", requests);
        }
        
        // GET: Admin/ProductRequests/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.ProductRequests
                .Include(r => r.RequestedBy)
                .Include(r => r.ReviewedBy)
                .Include(r => r.OriginalProduct)
                .Include(r => r.ProductImages)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/ProductRequest/Details.cshtml", request);
        }

        // POST: Admin/ProductRequests/Approve/5
        [HttpPost("Approve/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.ProductRequests
                .Include(r => r.ProductImages)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();
            if (request.Status != ProductRequestStatus.Pending)
            {
                TempData["RequestMessage"] = "该申请已处理。";
                return RedirectToAction(nameof(Index));
            }

            var adminId = GetCurrentUserId();
            request.Status = ProductRequestStatus.Approved;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedById = adminId;

            if (request.OriginalProductId.HasValue)
            {
                // 商品修改申请，更新现有商品
                var existingProduct = await _context.Products.FindAsync(request.OriginalProductId.Value);
                if (existingProduct != null)
                {
                    // 更新商品信息
                    existingProduct.Name = request.Name;
                    existingProduct.Price = request.Price;
                    existingProduct.Description = request.Description;
                    existingProduct.ShippingTimeHours = request.ShippingTimeHours;
                    existingProduct.ShippingMethod = request.ShippingMethod;
                    existingProduct.ShippingFee = request.ShippingFee;

                    // 将商品申请的图片关联到现有商品
                    var requestImages = request.ProductImages;
                    
                    // 删除现有商品图片
                    var existingImages = await _context.ProductImages
                        .Where(img => img.ProductId == existingProduct.Id)
                        .ToListAsync();
                    _context.ProductImages.RemoveRange(existingImages);
                    
                    // 关联新图片到现有商品
                    foreach (var img in requestImages)
                    {
                        img.ProductId = existingProduct.Id;
                        img.ProductRequestId = null; // 清除申请关联
                    }
                    
                    await _context.SaveChangesAsync();
                    TempData["RequestMessage"] = $"已通过 {request.Name} 的修改申请。";
                }
            }
            else
            {
                // 新商品申请，创建新商品
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
                var requestImages = request.ProductImages;
                
                foreach (var img in requestImages)
                {
                    img.ProductId = product.Id;
                    img.ProductRequestId = null; // 清除申请关联
                }
                
                await _context.SaveChangesAsync();
                TempData["RequestMessage"] = $"已通过 {request.Name} 的上架申请。";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/ProductRequests/Reject/5
        [HttpPost("Reject/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reviewComment)
        {
            var request = await _context.ProductRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (request.Status != ProductRequestStatus.Pending)
            {
                TempData["RequestMessage"] = "该申请已处理。";
                return RedirectToAction(nameof(Index));
            }

            var adminId = GetCurrentUserId();
            request.Status = ProductRequestStatus.Rejected;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedById = adminId;
            request.ReviewComment = reviewComment;
            await _context.SaveChangesAsync();

            TempData["RequestMessage"] = $"已驳回 {request.Name} 的上架申请。";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/ProductRequests/Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.ProductRequests
                .Include(r => r.ProductImages)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                return NotFound();
            }

            // 删除关联的图片
            _context.ProductImages.RemoveRange(request.ProductImages);
            
            // 删除申请记录
            _context.ProductRequests.Remove(request);
            await _context.SaveChangesAsync();

            TempData["RequestMessage"] = "商品申请记录已删除。";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) throw new InvalidOperationException("无法获取当前管理员信息");
            return int.Parse(userIdStr);
        }
    }
}