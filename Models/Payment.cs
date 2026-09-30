using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public enum PaymentMethod
    {
        Cod = 0,          // Thanh toán khi nhận hàng
        BankTransfer = 1, // Chuyển khoản / VietQR
        VnPay = 2,        // Cổng VNPay
        MoMo = 3          // Ví MoMo
    }

    public enum PaymentStatus
    {
        Unpaid = 0,   // Chưa thanh toán
        Paid = 1,     // Đã thanh toán
        Failed = 2,   // Thất bại
        Refunded = 3  // Đã hoàn tiền
    }

    /// <summary>
    /// Lịch sử thanh toán của một đơn hàng. Một đơn có thể có nhiều lần thử thanh toán.
    /// </summary>
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public PaymentMethod Method { get; set; } = PaymentMethod.Cod;

        public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền không được là số âm")]
        public decimal Amount { get; set; }

        // Mã giao dịch trả về từ cổng thanh toán (VNPay/MoMo), để đối soát
        [StringLength(100)]
        public string TransactionCode { get; set; } = string.Empty;

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
