using System.ComponentModel.DataAnnotations;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.ViewModels
{
    /// <summary>Form thêm/sửa sản phẩm trong khu quản trị.</summary>
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(150)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá bán không được là số âm")]
        [Display(Name = "Giá bán (đ)")]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá khuyến mãi không được là số âm")]
        [Display(Name = "Giá khuyến mãi (đ) - để trống nếu không giảm giá")]
        public decimal? DiscountPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được là số âm")]
        [Display(Name = "Số lượng tồn kho")]
        public int StockQuantity { get; set; }

        [StringLength(2000)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Hoặc nhập đường dẫn ảnh")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Tải ảnh lên (tối đa 2 MB)")]
        public IFormFile? ImageFile { get; set; }

        public string? CurrentImageUrl { get; set; }

        public List<Category> Categories { get; set; } = new List<Category>();
    }
}
