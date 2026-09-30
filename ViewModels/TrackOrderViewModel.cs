using System.ComponentModel.DataAnnotations;

namespace WebBanHang.ViewModels
{
    /// <summary>Form tra cứu đơn hàng dành cho khách (không cần đăng nhập).</summary>
    public class TrackOrderViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã đơn hàng")]
        [StringLength(30)]
        [Display(Name = "Mã đơn hàng")]
        public string OrderCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại hoặc email đã dùng khi đặt hàng")]
        [StringLength(150)]
        [Display(Name = "Số điện thoại hoặc email")]
        public string Contact { get; set; } = string.Empty;
    }
}
