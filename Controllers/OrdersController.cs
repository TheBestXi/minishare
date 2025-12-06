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
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 我的订单页面
        /// </summary>
        /// <returns>我的订单视图</returns>
        public async Task<IActionResult> MyOrders()
        {
            // 获取当前用户ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // 获取用户订单，包含商品信息
            var orders = await _context.Orders
                .Include(o => o.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // 获取每个商品的首图
            var productIds = orders.Select(o => o.ProductId).Distinct().ToList();
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

            ViewBag.ProductImages = imageDict;
            return View(orders);
        }

        /// <summary>
        /// 将订单标记为已支付
        /// </summary>
        /// <param name="id">订单ID</param>
        /// <returns>重定向到我的订单页面</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = OrderStatus.Paid;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyOrders));
        }

        /// <summary>
        /// 删除订单
        /// </summary>
        /// <param name="id">订单ID</param>
        /// <returns>重定向到我的订单页面</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyOrders));
        }
    }
}