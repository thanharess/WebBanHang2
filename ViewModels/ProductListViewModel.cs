using WebBanHang.Models;

namespace WebBanHang.ViewModels
{
    /// <summary>Dữ liệu cho trang danh mục sản phẩm (có tìm kiếm, lọc và phân trang).</summary>
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();

        public List<Category> Categories { get; set; } = new List<Category>();

        public string? Keyword { get; set; }

        public int? CategoryId { get; set; }

        public int Page { get; set; } = 1;

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;

        public string? CategoryName => Categories.FirstOrDefault(c => c.Id == CategoryId)?.Name;
    }
}
