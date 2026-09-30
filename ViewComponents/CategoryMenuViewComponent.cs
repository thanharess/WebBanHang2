using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebBanHang.ViewComponents
{
    /// <summary>Menu danh mục hiển thị trên thanh điều hướng của mọi trang.</summary>
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly Models.AppDbContext _context;

        public CategoryMenuViewComponent(Models.AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }
    }
}
