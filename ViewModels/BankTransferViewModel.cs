using WebBanHang.Models;

namespace WebBanHang.ViewModels
{
    public class BankTransferViewModel
    {
        public Order Order { get; set; } = null!;

        public string BankName { get; set; } = string.Empty;

        public string AccountNumber { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        /// <summary>Nội dung khách phải ghi khi chuyển khoản, chính là mã đơn hàng.</summary>
        public string TransferContent { get; set; } = string.Empty;

        public string QrImageUrl { get; set; } = string.Empty;

        public bool QrAvailable { get; set; }
    }
}
