using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Tests;

public class VnPayServiceTests
{
    private const string Secret = "SECRETKEY_DEMO_123456";

    private static VnPayService CreateService(string tmnCode = "TESTTMN01", string secret = Secret) =>
        new(Options.Create(new VnPaySettings
        {
            TmnCode = tmnCode,
            HashSecret = secret,
            BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
        }));

    private static Order CreateOrder(decimal total = 12_500_000m) => new()
    {
        Id = 1,
        OrderCode = "ORD-20260930-00001",
        TotalAmount = total,
        OrderDate = new DateTime(2026, 9, 30, 10, 0, 0)
    };

    /// <summary>
    /// Bộ ký độc lập viết theo đúng tài liệu VNPay (sắp xếp theo tên tham số,
    /// URL-encode từng key/value rồi ký HMAC-SHA512), dùng để kiểm tra bộ xác thực.
    /// </summary>
    private static Dictionary<string, string> SignManually(Dictionary<string, string> parameters, string secret)
    {
        var sorted = new SortedDictionary<string, string>(parameters, StringComparer.Ordinal);

        var signData = string.Join("&", sorted
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signData))).ToLowerInvariant();

        var result = new Dictionary<string, string>(parameters) { ["vnp_SecureHash"] = hash };
        return result;
    }

    private static QueryCollection ToQuery(Dictionary<string, string> parameters) =>
        new(parameters.ToDictionary(p => p.Key, p => new StringValues(p.Value)));

    [Fact]
    public void IsConfigured_false_khi_thieu_thong_tin()
    {
        Assert.False(CreateService(tmnCode: "").IsConfigured);
        Assert.False(CreateService(secret: "").IsConfigured);
        Assert.True(CreateService().IsConfigured);
    }

    [Fact]
    public void CreatePaymentUrl_chua_du_tham_so_va_chu_ky_128_hex()
    {
        var url = CreateService().CreatePaymentUrl(CreateOrder(), "127.0.0.1", "http://localhost/Checkout/VnPayReturn");

        Assert.StartsWith("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?", url);
        Assert.Contains("vnp_TxnRef=ORD-20260930-00001", url);
        Assert.Contains("vnp_Amount=1250000000", url); // 12.500.000 đ × 100
        Assert.Contains("vnp_TmnCode=TESTTMN01", url);
        Assert.Contains("vnp_IpAddr=127.0.0.1", url);
        Assert.Matches("vnp_SecureHash=[0-9a-f]{128}", url);
    }

    [Fact]
    public void CreatePaymentUrl_dung_ReturnUrl_du_phong_khi_chua_cau_hinh()
    {
        var url = CreateService().CreatePaymentUrl(CreateOrder(), "127.0.0.1", "http://shop.local/Checkout/VnPayReturn");

        Assert.Contains("vnp_ReturnUrl=" + WebUtility.UrlEncode("http://shop.local/Checkout/VnPayReturn"), url);
    }

    [Fact]
    public void ValidateCallback_chap_nhan_du_lieu_do_chinh_minh_tao_ra()
    {
        var service = CreateService();
        var url = service.CreatePaymentUrl(CreateOrder(), "127.0.0.1", "http://localhost/Checkout/VnPayReturn");

        var query = QueryHelpers.ParseQuery(new Uri(url).Query);
        var result = service.ValidateCallback(new QueryCollection(query));

        Assert.True(result.SignatureValid);
        Assert.Equal("ORD-20260930-00001", result.OrderCode);
        Assert.Equal(12_500_000m, result.Amount);
    }

    [Fact]
    public void ValidateCallback_tu_choi_khi_so_tien_bi_sua()
    {
        var service = CreateService();
        var url = service.CreatePaymentUrl(CreateOrder(), "127.0.0.1", "http://localhost/Checkout/VnPayReturn");

        var query = QueryHelpers.ParseQuery(new Uri(url).Query)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        query["vnp_Amount"] = new StringValues("100"); // gian lận: sửa số tiền

        var result = service.ValidateCallback(new QueryCollection(query));

        Assert.False(result.SignatureValid);
        Assert.False(result.Success);
    }

    [Fact]
    public void ValidateCallback_tu_choi_khi_ma_don_bi_sua()
    {
        var service = CreateService();
        var url = service.CreatePaymentUrl(CreateOrder(), "127.0.0.1", "http://localhost/Checkout/VnPayReturn");

        var query = QueryHelpers.ParseQuery(new Uri(url).Query)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        query["vnp_TxnRef"] = new StringValues("ORD-20260930-99999");

        Assert.False(service.ValidateCallback(new QueryCollection(query)).SignatureValid);
    }

    [Fact]
    public void ValidateCallback_tu_choi_khi_thieu_chu_ky()
    {
        var service = CreateService();
        var url = service.CreatePaymentUrl(CreateOrder(), "127.0.0.1", "http://localhost/Checkout/VnPayReturn");

        var query = QueryHelpers.ParseQuery(new Uri(url).Query)
            .Where(pair => pair.Key != "vnp_SecureHash")
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        Assert.False(service.ValidateCallback(new QueryCollection(query)).SignatureValid);
    }

    [Fact]
    public void ValidateCallback_tu_choi_khi_ky_bang_secret_khac()
    {
        var parameters = SignManually(new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1250000000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TmnCode"] = "TESTTMN01",
            ["vnp_TxnRef"] = "ORD-20260930-00001"
        }, "SECRET_KHAC_CUA_KE_TAN_CONG");

        Assert.False(CreateService().ValidateCallback(ToQuery(parameters)).SignatureValid);
    }

    [Fact]
    public void ValidateCallback_thanh_cong_khi_ma_phan_hoi_la_00()
    {
        var parameters = SignManually(new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1250000000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TmnCode"] = "TESTTMN01",
            ["vnp_TransactionNo"] = "14240001",
            ["vnp_TxnRef"] = "ORD-20260930-00001"
        }, Secret);

        var result = CreateService().ValidateCallback(ToQuery(parameters));

        Assert.True(result.SignatureValid);
        Assert.True(result.Success);
        Assert.Equal("14240001", result.TransactionCode);
        Assert.Equal(12_500_000m, result.Amount);
    }

    [Fact]
    public void ValidateCallback_chu_ky_dung_nhung_giao_dich_that_bai_thi_khong_tinh_la_thanh_cong()
    {
        var parameters = SignManually(new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1250000000",
            ["vnp_ResponseCode"] = "24", // khách huỷ giao dịch
            ["vnp_TmnCode"] = "TESTTMN01",
            ["vnp_TxnRef"] = "ORD-20260930-00001"
        }, Secret);

        var result = CreateService().ValidateCallback(ToQuery(parameters));

        Assert.True(result.SignatureValid);
        Assert.False(result.Success);
        Assert.Equal("24", result.ResponseCode);
    }
}
