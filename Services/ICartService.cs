using MiniShare.Models;

namespace MiniShare.Services
{
    /// <summary>
    /// 购物车服务接口
    /// </summary>
    public interface ICartService
    {
        /// <summary>
        /// 获取购物车中的商品数量
        /// </summary>
        int GetCartCount();

        /// <summary>
        /// 获取购物车中的所有商品
        /// </summary>
        List<int> GetCartItems();

        /// <summary>
        /// 添加商品到购物车
        /// </summary>
        void AddToCart(int productId);

        /// <summary>
        /// 从购物车移除商品
        /// </summary>
        void RemoveFromCart(int productId);

        /// <summary>
        /// 清空购物车
        /// </summary>
        void ClearCart();

        /// <summary>
        /// 检查商品是否在购物车中
        /// </summary>
        bool IsInCart(int productId);
    }
}