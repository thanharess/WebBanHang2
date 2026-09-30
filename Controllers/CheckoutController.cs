using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using WebBanHang.Models;
using WebBanHang.Services;
using WebBanHang.ViewModels;

namespace WebBanHang.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class CheckoutController : Controller
    {
        private const string LastOrderSessionKey = "LastOrderId";

        private readonly AppDbContext _context;
        private readonly ICartService _cart;
        private readonly VnPayService _vnPay;
        private readonly BankTransferSettings _bank;

        public CheckoutController(
            AppDbContext context,
            ICartService cart,
            VnPayService vnPay,
            IOptions<BankTransferSettings> bank)
        {
            _context = context;
            _cart = cart;
            _vnPay = vnPay;
            _bank = bank.Value;
        }

        public async Task<IActionResult> Index()
        {
            var cart = _cart.GetCart();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng đang trống, chưa thể đặt hàng.";
                return RedirectToAction("Index", "Cart");
            }

            await CartPricing.SyncAsync(_context, cart);
            return View(BuildViewModel(cart));
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = _cart.GetCart();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng đang trống, chưa thể đặt hàng.";
                return RedirectToAction("Index", "Cart");
            }

            await CartPricing.SyncAsync(_context, cart);

            if (!ModelState.IsValid)
            {
                return View("Index", BuildViewModel(cart, model));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var productIds = cart.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                // Kiểm tra tồn kho lần cuối ngay trước khi ghi đơn
                foreach (var item in cart)
                {
                    if (!products.TryGetValue(item.ProductId, out var product))
                    {
                        throw new InvalidOperationException($"Sản phẩm \"{item.ProductName}\" không còn bán.");
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"\"{product.Name}\" chỉ còn {product.StockQuantity} sản phẩm, không đủ số lượng bạn đặt.");
                    }
                }

                var customer = await GetOrCreateCustomerAsync(model);

                var order = new Order
                {
                    // Mã tạm để thoả ràng buộc duy nhất, sẽ đổi thành mã chính thức sau khi có Id
                    OrderCode = "TMP" + Guid.NewGuid().ToString("N").Substring(0, 20),
                    UserId = customer.Id,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    ShippingAddress = model.ShippingAddress.Trim(),
                    Note = model.Note?.Trim() ?? string.Empty
                };

                foreach (var item in cart)
                {
                    var product = products[item.ProductId];

                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        // Luôn dùng giá trong database, không tin giá lưu ở Session
                        UnitPrice = product.DiscountPrice ?? product.Price
                    });

                    product.StockQuantity -= item.Quantity; // trừ tồn kho
                }

                order.TotalAmount = order.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // đã có order.Id

                order.OrderCode = $"ORD-{order.OrderDate:yyyyMMdd}-{order.Id:D5}";

                _context.Payments.Add(new Payment
                {
                    OrderId = order.Id,
                    Method = model.PaymentMethod,
                    Status = PaymentStatus.Unpaid,
                    Amount = order.TotalAmount,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _cart.Clear();
                HttpContext.Session.SetInt32(LastOrderSessionKey, order.Id);
                TempData["Success"] = $"Đặt hàng thành công. Mã đơn của bạn là {order.OrderCode}.";

                // Tuỳ phương thức thanh toán mà đưa khách sang bước tiếp theo
                return model.PaymentMethod switch
                {
                    PaymentMethod.BankTransfer => RedirectToAction(nameof(BankTransfer), new { id = order.Id }),
                    PaymentMethod.VnPay when _vnPay.IsConfigured => Redirect(_vnPay.CreatePaymentUrl(
                        order,
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                        Url.Action(nameof(VnPayReturn), "Checkout", null, Request.Scheme)!)),
                    _ => RedirectToAction(nameof(Success), new { id = order.Id })
                };
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Không lưu được đơn hàng, vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Success(int id)
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

        /// <summary>Trang hiển thị mã VietQR để khách chuyển khoản.</summary>
        public async Task<IActionResult> BankTransfer(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Payments)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || !CanViewOrder(order))
            {
                return NotFound();
            }

            var model = new BankTransferViewModel
            {
                Order = order,
                BankName = _bank.BankName,
                AccountNumber = _bank.AccountNumber,
                AccountName = _bank.AccountName,
                TransferContent = order.OrderCode,
                QrAvailable = _bank.IsConfigured
            };

            if (_bank.IsConfigured)
            {
                model.QrImageUrl = "https://img.vietqr.io/image/" +
                    $"{_bank.BankBin}-{_bank.AccountNumber}-{_bank.Template}.png" +
                    $"?amount={(long)order.TotalAmount}" +
                    $"&addInfo={Uri.EscapeDataString(order.OrderCode)}" +
                    $"&accountName={Uri.EscapeDataString(_bank.AccountName)}";
            }

            return View(model);
        }

        /// <summary>VNPay chuyển hướng khách về đây kèm kết quả thanh toán.</summary>
        public async Task<IActionResult> VnPayReturn()
        {
            var result = _vnPay.ValidateCallback(Request.Query);

            if (!result.SignatureValid)
            {
                TempData["Error"] = "Chữ ký trả về từ VNPay không hợp lệ nên giao dịch bị từ chối.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderCode == result.OrderCode);

            if (order == null)
            {
                TempData["Error"] = $"Không tìm thấy đơn hàng {result.OrderCode}.";
                return RedirectToAction("Index", "Home");
            }

            var payment = order.Payments.FirstOrDefault();

            if (payment != null && result.Success)
            {
                payment.Status = PaymentStatus.Paid;
                payment.PaidAt = DateTime.Now;
                payment.TransactionCode = result.TransactionCode;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thanh toán đơn {order.OrderCode} qua VNPay.";
            }
            else if (payment != null)
            {
                payment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
                TempData["Error"] = $"Thanh toán VNPay không thành công (mã lỗi {result.ResponseCode}).";
            }

            HttpContext.Session.SetInt32(LastOrderSessionKey, order.Id);
            return RedirectToAction(nameof(Success), new { id = order.Id });
        }

        /// <summary>
        /// Cho xem đơn nếu là đơn vừa đặt trong phiên làm việc này,
        /// hoặc đơn thuộc tài khoản đang đăng nhập.
        /// </summary>
        private bool CanViewOrder(Order order)
        {
            if (HttpContext.Session.GetInt32(LastOrderSessionKey) == order.Id)
            {
                return true;
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return currentUserId != null && order.UserId.ToString() == currentUserId;
        }

        private async Task<User> GetOrCreateCustomerAsync(CheckoutViewModel model)
        {
            var email = model.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    FullName = model.FullName.Trim(),
                    Email = email,
                    PhoneNumber = model.PhoneNumber.Trim(),
                    Address = model.ShippingAddress.Trim(),
                    // Khách mua không cần đăng nhập nên chưa có mật khẩu.
                    // Khi làm chức năng đăng ký, tài khoản này sẽ được đặt mật khẩu.
                    PasswordHash = string.Empty,
                    Role = UserRole.Customer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
            }
            else
            {
                // Cập nhật thông tin giao hàng mới nhất của khách
                user.FullName = model.FullName.Trim();
                user.PhoneNumber = model.PhoneNumber.Trim();
                user.Address = model.ShippingAddress.Trim();
            }

            await _context.SaveChangesAsync();
            return user;
        }

        private CheckoutViewModel BuildViewModel(List<CartItem> cart, CheckoutViewModel? posted = null)
        {
            var model = posted ?? new CheckoutViewModel();
            model.Items = cart;
            model.TotalAmount = cart.Sum(i => i.TotalPrice);
            return model;
        }
    }
}
