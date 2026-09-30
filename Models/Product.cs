
using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(150)]

        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán không được là số âm")]
        public decimal Price { get; set; }

        // Giá khuyến mãi. null = sản phẩm không giảm giá
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá khuyến mãi không được là số âm")]
        public decimal? DiscountPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được là số âm")]
        public int StockQuantity { get; set; } // Số lượng tồn kho

        public string ImageUrl { get; set; } = string.Empty; // Đường dẫn ảnh sản phẩm -> ví dụ: /images/gay-driver.jpg

        public string Description { get; set; } = string.Empty;

        // Khóa ngoại liên kết tới bảng Category
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}

