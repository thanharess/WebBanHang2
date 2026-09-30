using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class VnPayCallbackResult
    {
        public bool SignatureValid { get; set; }

        public bool Success { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public string TransactionCode { get; set; } = string.Empty;

        public string ResponseCode { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Tạo URL thanh toán VNPay và kiểm tra chữ ký khi VNPay trả kết quả về.
    /// Cách ký theo đúng thuật toán mẫu của VNPay: sắp xếp tham số theo tên,
    /// nối thành chuỗi key=value đã URL-encode, rồi ký HMAC-SHA512.
    /// </summary>
    public class VnPayService
    {
        private readonly VnPaySettings _settings;

        public VnPayService(IOptions<VnPaySettings> options)
        {
            _settings = options.Value;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.TmnCode) && !string.IsNullOrWhiteSpace(_settings.HashSecret);

        public string CreatePaymentUrl(Order order, string clientIp, string fallbackReturnUrl)
        {
            // VNPay yêu cầu giờ Việt Nam (GMT+7)
            var now = DateTime.Now;

            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = _settings.Version,
                ["vnp_Command"] = _settings.Command,
                ["vnp_TmnCode"] = _settings.TmnCode,
                ["vnp_Locale"] = _settings.Locale,
                ["vnp_CurrCode"] = _settings.CurrCode,
                ["vnp_TxnRef"] = order.OrderCode,
                ["vnp_OrderInfo"] = "Thanh toan don hang " + order.OrderCode,
                ["vnp_OrderType"] = _settings.OrderType,
                // VNPay tính theo đơn vị nhỏ nhất (đồng × 100)
                ["vnp_Amount"] = ((long)(order.TotalAmount * 100)).ToString(CultureInfo.InvariantCulture),
                ["vnp_ReturnUrl"] = string.IsNullOrWhiteSpace(_settings.ReturnUrl) ? fallbackReturnUrl : _settings.ReturnUrl,
                ["vnp_IpAddr"] = clientIp,
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss")
            };

            var signData = BuildSignData(parameters);
            var secureHash = HmacSha512(_settings.HashSecret, signData);

            return $"{_settings.BaseUrl}?{signData}&vnp_SecureHash={secureHash}";
        }

        public VnPayCallbackResult ValidateCallback(IQueryCollection query)
        {
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (var item in query)
            {
                if (item.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)
                    && item.Key is not "vnp_SecureHash" and not "vnp_SecureHashType")
                {
                    parameters[item.Key] = item.Value.ToString();
                }
            }

            var receivedHash = query["vnp_SecureHash"].ToString();
            var expectedHash = HmacSha512(_settings.HashSecret, BuildSignData(parameters));

            var result = new VnPayCallbackResult
            {
                SignatureValid = !string.IsNullOrEmpty(receivedHash)
                    && string.Equals(expectedHash, receivedHash, StringComparison.OrdinalIgnoreCase),
                OrderCode = query["vnp_TxnRef"].ToString(),
                TransactionCode = query["vnp_TransactionNo"].ToString(),
                ResponseCode = query["vnp_ResponseCode"].ToString()
            };

            if (long.TryParse(query["vnp_Amount"].ToString(), out var amount))
            {
                result.Amount = amount / 100m;
            }

            result.Success = result.SignatureValid && result.ResponseCode == "00";
            return result;
        }

        private static string BuildSignData(SortedDictionary<string, string> parameters) =>
            string.Join("&", parameters
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));

        private static string HmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
