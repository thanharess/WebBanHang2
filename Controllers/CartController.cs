using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class CartController : Controller
    {
        private readonly ICartService _cart;
        private readonly AppDbContext _context;

        public CartController(ICartService cart, AppDbContext context)
        {
            _cart = cart;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var cart = _cart.GetCart();
            await CartPricing.SyncAsync(_context, cart);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return NotFound();
            }

            if (quantity < 1)
            {
                quantity = 1;
            }

            var alreadyInCart = _cart.GetQuantity(productId);
            var remaining = product.StockQuantity - alreadyInCart;

            if (remaining <= 0)
            {
                TempData["Error"] = $"Sản phẩm \"{product.Name}\" chỉ còn {product.StockQuantity} trong kho.";
                return RedirectBack(returnUrl, productId);
            }

            if (quantity > remaining)
            {
                quantity = remaining;
                TempData["Error"] = $"\"{product.Name}\" chỉ còn {remaining} sản phẩm có thể thêm vào giỏ.";
            }
            else
            {
                TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng.";
            }

            _cart.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.DiscountPrice ?? product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl
            });

            return RedirectBack(returnUrl, productId);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                _cart.Remove(productId);
                TempData["Error"] = "Sản phẩm không còn bán nên đã được xoá khỏi giỏ.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity > product.StockQuantity)
            {
                quantity = product.StockQuantity;
                TempData["Error"] = $"\"{product.Name}\" chỉ còn {product.StockQuantity} sản phẩm trong kho.";
            }

            _cart.UpdateQuantity(productId, quantity);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            _cart.Remove(productId);
            TempData["Success"] = "Đã xoá sản phẩm khỏi giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Clear()
        {
            _cart.Clear();
            TempData["Success"] = "Đã xoá toàn bộ giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Quay lại trang khách vừa bấm, chỉ chấp nhận đường dẫn nội bộ.</summary>
        private IActionResult RedirectBack(string? returnUrl, int productId) =>
            Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl!)
                : RedirectToAction("Details", "Products", new { id = productId });
    }
}
