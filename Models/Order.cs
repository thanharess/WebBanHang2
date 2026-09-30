using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public enum OrderStatus
    {
        Pending = 0,   // Chờ xác nhận
        Confirmed = 1, // Đã xác nhận
        Shipping = 2,  // Đang giao hàng
        Completed = 3, // Hoàn thành
        Cancelled = 4  // Đã huỷ
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Mã đơn hiển thị cho khách, ví dụ: ORD-20260930-0001
        [Required]
        [StringLength(30)]
        public string OrderCode { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền không được là số âm")]
        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [StringLength(300)]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string Note { get; set; } = string.Empty;

        // Quan hệ 1 - Nhiều
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
