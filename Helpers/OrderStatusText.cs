using WebBanHang.Models;

namespace WebBanHang.Helpers
{
    /// <summary>Nhãn tiếng Việt và màu badge cho trạng thái đơn hàng / thanh toán.</summary>
    public static class OrderStatusText
    {
        public static string ToVietnamese(this OrderStatus status) => status switch
        {
            OrderStatus.Pending => "Chờ xác nhận",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Shipping => "Đang giao hàng",
            OrderStatus.Completed => "Hoàn thành",
            OrderStatus.Cancelled => "Đã huỷ",
            _ => status.ToString()
        };

        public static string ToBadgeClass(this OrderStatus status) => status switch
        {
            OrderStatus.Pending => "bg-warning text-dark",
            OrderStatus.Confirmed => "bg-info text-dark",
            OrderStatus.Shipping => "bg-primary",
            OrderStatus.Completed => "bg-success",
            OrderStatus.Cancelled => "bg-secondary",
            _ => "bg-secondary"
        };

        public static string ToVietnamese(this PaymentMethod method) => method switch
        {
            PaymentMethod.Cod => "Thanh toán khi nhận hàng (COD)",
            PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
            PaymentMethod.VnPay => "VNPay",
            PaymentMethod.MoMo => "Ví MoMo",
            _ => method.ToString()
        };

        public static string ToVietnamese(this PaymentStatus status) => status switch
        {
            PaymentStatus.Unpaid => "Chưa thanh toán",
            PaymentStatus.Paid => "Đã thanh toán",
            PaymentStatus.Failed => "Thanh toán thất bại",
            PaymentStatus.Refunded => "Đã hoàn tiền",
            _ => status.ToString()
        };
    }
}
