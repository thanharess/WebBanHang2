using WebBanHang.Models;

namespace WebBanHang.Services
{
    /// <summary>
    /// Giỏ hàng lưu tạm trong Session của khách (không cần đăng nhập).
    /// </summary>
    public interface ICartService
    {
        List<CartItem> GetCart();

        /// <summary>Tổng số lượng sản phẩm trong giỏ (dùng cho badge trên menu).</summary>
        int GetItemCount();

        decimal GetTotal();

        /// <summary>Số lượng của một sản phẩm đang có trong giỏ.</summary>
        int GetQuantity(int productId);

        void Add(CartItem item);

        void UpdateQuantity(int productId, int quantity);

        void Remove(int productId);

        void Clear();
    }
}
