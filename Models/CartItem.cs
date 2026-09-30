
namespace WebBanHang.Models
{
    /// <summary>
    /// Một dòng trong giỏ hàng. Được lưu tạm vào Session dưới dạng JSON.
    /// </summary>
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public decimal TotalPrice => Price * Quantity; // Thành tiền tự động tính
    }
}

