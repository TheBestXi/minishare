using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using MiniShare.Services;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MiniShare.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;

        public CheckoutController(ApplicationDbContext context, ICartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        /// <summary>
        /// 结算页面
        /// </summary>
        /// <returns>结算视图</returns>
        public async Task<IActionResult> Index(int[] selectedIds)
        {
            // 如果没有选择商品，重定向到购物车
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["ErrorMessage"] = "请先选择要结算的商品";
                return RedirectToAction("Cart", "Products");
            }

            // 获取选中的商品
            var products = await _context.Products.Where(p => selectedIds.Contains(p.Id)).ToListAsync();
            
            // 获取每个商品的首图
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
            
            // 补充没有主图的商品
            foreach (var img in allImages)
            {
                if (img.ProductId.HasValue && !imageDict.ContainsKey(img.ProductId.Value))
                    imageDict[img.ProductId.Value] = img.ImageUrl;
            }

            // 计算总金额和总运费
            decimal totalPrice = products.Sum(p => p.Price);
            decimal totalShippingFee = products.Sum(p => p.ShippingFee);
            decimal grandTotal = totalPrice + totalShippingFee;

            // 将数据传递给视图
            ViewBag.ProductImages = imageDict;
            ViewBag.TotalPrice = totalPrice;
            ViewBag.TotalShippingFee = totalShippingFee;
            ViewBag.GrandTotal = grandTotal;
            ViewBag.SelectedIds = selectedIds;

            return View(products);
        }

        /// <summary>
        /// 处理结算请求
        /// </summary>
        /// <param name="selectedIds">选中的商品ID数组</param>
        /// <param name="shippingAddress">收货地址</param>
        /// <returns>订单列表页面</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(int[] selectedIds, string shippingAddress)
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 验证商品
            var products = await _context.Products.Where(p => selectedIds.Contains(p.Id)).ToListAsync();
            if (products == null || products.Count == 0)
            {
                TempData["ErrorMessage"] = "没有找到要结算的商品";
                return RedirectToAction("Cart", "Products");
            }

            // 创建订单
            foreach (var product in products)
            {
                var order = new Order
                {
                    UserId = userId,
                    ProductId = product.Id,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Orders.Add(order);
            }

            // 保存订单
            await _context.SaveChangesAsync();

            // 从购物车中移除已结算的商品
            foreach (var id in selectedIds)
            {
                _cartService.RemoveFromCart(id);
            }

            // 重定向到订单列表
            TempData["SuccessMessage"] = "订单创建成功！";
            return RedirectToAction("MyOrders", "Orders");
        }
    }
}