using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.ViewModels;

namespace WebBanHang.Controllers
{
    public class ProductsController : Controller
    {
        private const int PageSize = 8;

        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Danh mục sản phẩm: tìm theo tên/mô tả, lọc theo danh mục, có phân trang.</summary>
        public async Task<IActionResult> Index(string? q, int? categoryId, int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            if (page < 1)
            {
                page = 1;
            }
            else if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var model = new ProductListViewModel
            {
                Keyword = q,
                CategoryId = categoryId,
                Page = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Categories = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                Products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
