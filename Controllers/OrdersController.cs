using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBanHang.Helpers;
using WebBanHang.Models;
using WebBanHang.ViewModels;

namespace WebBanHang.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class OrdersController : Controller
    {
        /// <summary>
        /// Danh sách mã đơn mà khách đã chứng minh được quyền xem trong phiên này
        /// (nhờ tra cứu đúng mã đơn kèm số điện thoại/email).
        /// </summary>
        private const string TrackedOrdersKey = "TrackedOrderIds";

        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Danh sách đơn hàng của tài khoản đang đăng nhập.</summary>
        [Authorize]
        public async Task<IActionResult> Index()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index)) });
            }

            var orders = await _context.Orders
                .Include(o => o.Payments)
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(orders);
        }

        /// <summary>Tra cứu đơn hàng không cần đăng nhập.</summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Track() => View(new TrackOrderViewModel());

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Track(TrackOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var code = model.OrderCode.Trim();
            var contact = model.Contact.Trim();

            var order = await _context.Orders
                .Include(o => o.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderCode == code);

            var matched = order?.User != null
                && (string.Equals(order.User.PhoneNumber, contact, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(order.User.Email, contact, StringComparison.OrdinalIgnoreCase));

            if (!matched)
            {
                ModelState.AddModelError(string.Empty,
                    "Không tìm thấy đơn hàng khớp với mã đơn và số điện thoại/email đã nhập.");
                return View(model);
            }

            RememberTrackedOrder(order!.Id);
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || !CanViewOrder(order))
            {
                return NotFound();
            }

            return View(order);
        }

        private bool CanViewOrder(Order order)
        {
            if (GetTrackedOrderIds().Contains(order.Id))
            {
                return true;
            }

            return TryGetCurrentUserId(out var userId) && order.UserId == userId;
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out userId);
        }

        private List<int> GetTrackedOrderIds() =>
            HttpContext.Session.Get<List<int>>(TrackedOrdersKey) ?? new List<int>();

        private void RememberTrackedOrder(int orderId)
        {
            var ids = GetTrackedOrderIds();
            if (!ids.Contains(orderId))
            {
                ids.Add(orderId);
                HttpContext.Session.Set(TrackedOrdersKey, ids);
            }
        }
    }
}
