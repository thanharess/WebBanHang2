using WebBanHang.Helpers;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class CartService : ICartService
    {
        private const string CartKey = "Cart";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session =>
            _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("Session chưa sẵn sàng. Hãy gọi app.UseSession() trước.");

        public List<CartItem> GetCart() => Session.Get<List<CartItem>>(CartKey) ?? new List<CartItem>();

        public int GetItemCount() => GetCart().Sum(i => i.Quantity);

        public decimal GetTotal() => GetCart().Sum(i => i.TotalPrice);

        public int GetQuantity(int productId) =>
            GetCart().FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0;

        public void Add(CartItem item)
        {
            var cart = GetCart();
            var existing = cart.FirstOrDefault(i => i.ProductId == item.ProductId);

            if (existing == null)
            {
                cart.Add(item);
            }
            else
            {
                existing.Quantity += item.Quantity;
                // Giữ giá mới nhất phòng trường hợp cửa hàng vừa đổi giá
                existing.Price = item.Price;
            }

            Save(cart);
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item == null)
            {
                return;
            }

            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            Save(cart);
        }

        public void Remove(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(i => i.ProductId == productId);
            Save(cart);
        }

        public void Clear() => Session.Remove(CartKey);

        private void Save(List<CartItem> cart) => Session.Set(CartKey, cart);
    }
}
