using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Areas.Admin.ViewModels;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                ProductCount = await _context.Products.CountAsync(),
                CategoryCount = await _context.Categories.CountAsync(),
                OrderCount = await _context.Orders.CountAsync(),
                CustomerCount = await _context.Users.CountAsync(u => u.Role == UserRole.Customer),
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,
                LatestOrders = await _context.Orders
                    .Include(o => o.User)
                    .AsNoTracking()
                    .OrderByDescending(o => o.Id)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}
