namespace WebBanHang.Services
{
    /// <summary>Thông tin tài khoản ngân hàng dùng để sinh mã VietQR.</summary>
    public class BankTransferSettings
    {
        public string BankName { get; set; } = string.Empty;

        /// <summary>Mã BIN ngân hàng theo chuẩn VietQR, ví dụ Vietcombank = 970436.</summary>
        public string BankBin { get; set; } = string.Empty;

        public string AccountNumber { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        /// <summary>Mẫu ảnh QR của vietqr.io: compact, compact2, qr_only, print.</summary>
        public string Template { get; set; } = "compact2";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BankBin) && !string.IsNullOrWhiteSpace(AccountNumber);
    }
}
