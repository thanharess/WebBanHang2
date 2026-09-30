using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    /// <summary>
    /// Đồng bộ tên và giá của các dòng trong giỏ theo dữ liệu mới nhất trong database,
    /// tránh việc khách nhìn thấy giá cũ sau khi cửa hàng đổi giá.
    /// </summary>
    public static class CartPricing
    {
        public static async Task SyncAsync(AppDbContext context, List<CartItem> cart)
        {
            if (cart.Count == 0)
            {
                return;
            }

            var ids = cart.Select(i => i.ProductId).Distinct().ToList();
            var products = await context.Products
                .Where(p => ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var item in cart)
            {
                if (products.TryGetValue(item.ProductId, out var product))
                {
                    item.ProductName = product.Name;
                    item.Price = product.DiscountPrice ?? product.Price;
                    item.ImageUrl = product.ImageUrl;
                }
            }
        }
    }
}
