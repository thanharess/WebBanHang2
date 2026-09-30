using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Areas.Admin.ViewModels;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken]
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        public IActionResult Create() => View(new CategoryFormViewModel { IsActive = true });

        [HttpPost]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await EnsureNameIsUniqueAsync(model.Name, null);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var category = new Category
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim() ?? string.Empty,
                IsActive = model.IsActive
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm danh mục \"{category.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await EnsureNameIsUniqueAsync(model.Name, id);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            category.Name = model.Name.Trim();
            category.Description = model.Description?.Trim() ?? string.Empty;
            category.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật danh mục \"{category.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            if (await _context.Products.AnyAsync(p => p.CategoryId == id))
            {
                TempData["Error"] = $"Danh mục \"{category.Name}\" vẫn còn sản phẩm nên không thể xoá. " +
                                    "Hãy chuyển sản phẩm sang danh mục khác trước.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xoá danh mục \"{category.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        private async Task EnsureNameIsUniqueAsync(string name, int? ignoreId)
        {
            var trimmed = name.Trim();
            var exists = await _context.Categories
                .AnyAsync(c => c.Name == trimmed && (ignoreId == null || c.Id != ignoreId));

            if (exists)
            {
                ModelState.AddModelError(nameof(CategoryFormViewModel.Name), "Tên danh mục này đã tồn tại.");
            }
        }
    }
}
