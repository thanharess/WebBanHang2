using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Areas.Admin.ViewModels;
using WebBanHang.Helpers;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken]
    public class OrdersController : Controller
    {
        private const int PageSize = 10;

        /// <summary>Luồng trạng thái hợp lệ của một đơn hàng.</summary>
        private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new[] { OrderStatus.Shipping, OrderStatus.Cancelled },
            [OrderStatus.Shipping] = new[] { OrderStatus.Completed, OrderStatus.Cancelled },
            [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(OrderStatus? status, string? q, int page = 1)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payments)
                .AsNoTracking()
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(o => o.OrderCode.Contains(keyword)
                    || (o.User != null && (o.User.FullName.Contains(keyword) || o.User.PhoneNumber.Contains(keyword))));
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

            var model = new OrderListViewModel
            {
                Status = status,
                Keyword = q,
                Page = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                AllCount = await _context.Orders.CountAsync(),
                StatusCounts = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count),
                Orders = await query
                    .OrderByDescending(o => o.Id)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await LoadOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            ViewData["AllowedNextStatuses"] = AllowedTransitions[order.Status];
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            // Phải dùng bản có theo dõi thay đổi thì SaveChanges mới ghi được
            var order = await LoadOrderForUpdateAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == status)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!AllowedTransitions[order.Status].Contains(status))
            {
                TempData["Error"] = $"Không thể chuyển đơn {order.OrderCode} từ \"{order.Status.ToVietnamese()}\" " +
                                    $"sang \"{status.ToVietnamese()}\".";
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            if (status == OrderStatus.Cancelled)
            {
                // Huỷ đơn thì trả lại số lượng đã trừ khỏi kho
                var productIds = order.OrderDetails.Select(d => d.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var detail in order.OrderDetails)
                {
                    if (products.TryGetValue(detail.ProductId, out var product))
                    {
                        product.StockQuantity += detail.Quantity;
                    }
                }

                // Đơn đã thu tiền thì ghi nhận đã hoàn tiền
                foreach (var payment in order.Payments.Where(p => p.Status == PaymentStatus.Paid))
                {
                    payment.Status = PaymentStatus.Refunded;
                }
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đơn {order.OrderCode} đã chuyển sang \"{status.ToVietnamese()}\"." +
                                  (status == OrderStatus.Cancelled ? " Tồn kho đã được hoàn lại." : string.Empty);

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Ghi nhận đã thu được tiền (dùng cho COD và chuyển khoản ngân hàng).</summary>
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var order = await LoadOrderForUpdateAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                TempData["Error"] = $"Đơn {order.OrderCode} đã huỷ nên không thể ghi nhận thanh toán.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var payment = order.Payments.FirstOrDefault();
            if (payment == null)
            {
                TempData["Error"] = $"Đơn {order.OrderCode} chưa có bản ghi thanh toán.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (payment.Status == PaymentStatus.Paid)
            {
                TempData["Error"] = $"Đơn {order.OrderCode} đã được ghi nhận thanh toán trước đó.";
                return RedirectToAction(nameof(Details), new { id });
            }

            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã ghi nhận thanh toán cho đơn {order.OrderCode}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private Task<Order?> LoadOrderAsync(int id) =>
            _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

        /// <summary>Bản có theo dõi thay đổi, dùng cho thao tác cập nhật.</summary>
        private Task<Order?> LoadOrderForUpdateAsync(int id) =>
            _context.Orders
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);
    }
}
