using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Areas.Admin.ViewModels
{
    /// <summary>Form thêm/sửa danh mục trong khu quản trị.</summary>
    public class CategoryFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Đang hiển thị")]
        public bool IsActive { get; set; }
    }
}
