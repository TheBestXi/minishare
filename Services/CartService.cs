using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MiniShare.Services
{
    /// <summary>
    /// 购物车服务实现
    /// </summary>
    public class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartKeyPrefix = "cart_";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor">HTTP上下文访问器</param>
        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取当前用户的购物车键
        /// </summary>
        private string GetCartKey()
        {
            // 获取当前用户ID
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // 如果用户未登录，使用会话ID作为区分
            if (string.IsNullOrEmpty(userId))
            {
                userId = _httpContextAccessor.HttpContext?.Request.Cookies["anon_id"];
                if (string.IsNullOrEmpty(userId))
                {
                    userId = Guid.NewGuid().ToString("N");
                    _httpContextAccessor.HttpContext?.Response.Cookies.Append("anon_id", userId, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(30),
                        MaxAge = TimeSpan.FromDays(30),
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                    });
                }
            }
            return $"{CartKeyPrefix}{userId}";
        }

        /// <summary>
        /// 获取购物车中的商品数量
        /// </summary>
        public int GetCartCount()
        {
            var cartKey = GetCartKey();
            var cart = _httpContextAccessor.HttpContext?.Request.Cookies[cartKey] ?? string.Empty;
            if (string.IsNullOrEmpty(cart))
            {
                return 0;
            }
            return cart.Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .Count();
        }

        /// <summary>
        /// 获取购物车中的所有商品
        /// </summary>
        public List<int> GetCartItems()
        {
            var cartKey = GetCartKey();
            var cart = _httpContextAccessor.HttpContext?.Request.Cookies[cartKey] ?? string.Empty;
            if (string.IsNullOrEmpty(cart))
            {
                return new List<int>();
            }
            return cart.Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(int.Parse)
                .ToList();
        }

        /// <summary>
        /// 添加商品到购物车
        /// </summary>
        /// <param name="productId">商品ID</param>
        public void AddToCart(int productId)
        {
            var items = GetCartItems();
            items.Add(productId);
            SaveCart(items);
        }

        /// <summary>
        /// 从购物车移除商品
        /// </summary>
        /// <param name="productId">商品ID</param>
        public void RemoveFromCart(int productId)
        {
            var items = GetCartItems();
            items.RemoveAll(id => id == productId);
            SaveCart(items);
        }

        /// <summary>
        /// 清空购物车
        /// </summary>
        public void ClearCart()
        {
            var cartKey = GetCartKey();
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(cartKey);
        }

        /// <summary>
        /// 检查商品是否在购物车中
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <returns>是否在购物车中</returns>
        public bool IsInCart(int productId)
        {
            var items = GetCartItems();
            return items.Contains(productId);
        }

        /// <summary>
        /// 保存购物车到会话
        /// </summary>
        /// <param name="items">商品ID列表</param>
        private void SaveCart(List<int> items)
        {
            var cartKey = GetCartKey();
            if (items.Count == 0)
            {
                _httpContextAccessor.HttpContext?.Response.Cookies.Delete(cartKey);
            }
            else
            {
                _httpContextAccessor.HttpContext?.Response.Cookies.Append(cartKey, string.Join(',', items), new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    MaxAge = TimeSpan.FromDays(30),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                });
            }
        }
    }
}
