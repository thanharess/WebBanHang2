
using System.ComponentModel.DataAnnotations;


namespace WebBanHang.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100)]

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // Trạng thái hiển thị của danh mục (false = đang ẩn)
        public bool IsActive { get; set; } = true;

        // Quan hệ 1 - Nhiều: Một danh mục có nhiều sản phẩm
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

