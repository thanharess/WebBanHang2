using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Areas.Admin.ViewModels;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken]
    public class ProductsController : Controller
    {
        private const int PageSize = 10;

        private readonly AppDbContext _context;
        private readonly ImageStorage _images;

        public ProductsController(AppDbContext context, ImageStorage images)
        {
            _context = context;
            _images = images;
        }

        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            var query = _context.Products.Include(p => p.Category).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(p => p.Name.Contains(keyword));
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

            ViewData["Keyword"] = q;
            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalItems;

            var products = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            var model = new ProductFormViewModel { StockQuantity = 1 };
            await LoadCategoriesAsync(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            ValidateDiscount(model);

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model);
                return View(model);
            }

            string? imageUrl = model.ImageUrl?.Trim();

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    imageUrl = await _images.SaveAsync(model.ImageFile);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                    await LoadCategoriesAsync(model);
                    return View(model);
                }
            }

            var product = new Product
            {
                Name = model.Name.Trim(),
                CategoryId = model.CategoryId,
                Price = model.Price,
                DiscountPrice = model.DiscountPrice,
                StockQuantity = model.StockQuantity,
                Description = model.Description?.Trim() ?? string.Empty,
                ImageUrl = imageUrl ?? string.Empty
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm sản phẩm \"{product.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                StockQuantity = product.StockQuantity,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                CurrentImageUrl = product.ImageUrl
            };

            await LoadCategoriesAsync(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            ValidateDiscount(model);
            model.CurrentImageUrl = product.ImageUrl;

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model);
                return View(model);
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    var newImageUrl = await _images.SaveAsync(model.ImageFile);
                    _images.Delete(product.ImageUrl); // dọn ảnh cũ nếu là ảnh nội bộ
                    product.ImageUrl = newImageUrl;
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                    await LoadCategoriesAsync(model);
                    return View(model);
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                product.ImageUrl = model.ImageUrl.Trim();
            }

            product.Name = model.Name.Trim();
            product.CategoryId = model.CategoryId;
            product.Price = model.Price;
            product.DiscountPrice = model.DiscountPrice;
            product.StockQuantity = model.StockQuantity;
            product.Description = model.Description?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật sản phẩm \"{product.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewData["UsedInOrders"] = await _context.OrderDetails.AnyAsync(d => d.ProductId == id);
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var imageUrl = product.ImageUrl;

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                _images.Delete(imageUrl);

                TempData["Success"] = $"Đã xoá sản phẩm \"{product.Name}\".";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = $"Không xoá được \"{product.Name}\" vì sản phẩm đã có trong đơn hàng. " +
                                    "Hãy đặt số lượng tồn kho = 0 để ngừng bán thay vì xoá.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void ValidateDiscount(ProductFormViewModel model)
        {
            if (model.DiscountPrice.HasValue && model.DiscountPrice.Value >= model.Price)
            {
                ModelState.AddModelError(nameof(model.DiscountPrice), "Giá khuyến mãi phải nhỏ hơn giá bán.");
            }
        }

        private async Task LoadCategoriesAsync(ProductFormViewModel model)
        {
            model.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
