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
    public class StatisticsController : Controller
    {
        private const int DailyDays = 30;
        private const int MonthlyMonths = 12;
        private const int TopProductCount = 5;

        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var firstDay = today.AddDays(-(DailyDays - 1));
            var firstMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-(MonthlyMonths - 1));

            // Doanh thu chỉ tính các đơn không bị huỷ
            var validOrders = _context.Orders.Where(o => o.Status != OrderStatus.Cancelled);

            var daily = await validOrders
                .Where(o => o.OrderDate >= firstDay)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
                .ToListAsync();

            var monthly = await validOrders
                .Where(o => o.OrderDate >= firstMonth)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            var topProducts = await _context.OrderDetails
                .Where(d => d.Order!.Status != OrderStatus.Cancelled)
                .GroupBy(d => d.Product!.Name)
                .Select(g => new { Name = g.Key, Quantity = g.Sum(d => d.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .Take(TopProductCount)
                .ToListAsync();

            var statusGroups = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var model = new StatisticsViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                CancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled),
                TotalRevenue = await validOrders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,
                CustomerCount = await _context.Users.CountAsync(u => u.Role == UserRole.Customer)
            };

            var validOrderCount = model.TotalOrders - model.CancelledOrders;
            model.AverageOrderValue = validOrderCount > 0 ? model.TotalRevenue / validOrderCount : 0m;

            // Điền đủ 30 ngày, ngày nào không có đơn thì doanh thu bằng 0
            var dailyMap = daily.ToDictionary(x => x.Date, x => x);
            for (var i = 0; i < DailyDays; i++)
            {
                var date = firstDay.AddDays(i);
                model.DailyLabels.Add(date.ToString("dd/MM"));
                model.DailyRevenue.Add(dailyMap.TryGetValue(date, out var day) ? day.Revenue : 0m);
                model.DailyOrders.Add(dailyMap.TryGetValue(date, out var day2) ? day2.Count : 0);
            }

            var monthlyMap = monthly.ToDictionary(x => (x.Year, x.Month), x => x);
            for (var i = 0; i < MonthlyMonths; i++)
            {
                var month = firstMonth.AddMonths(i);
                model.MonthlyLabels.Add($"T{month.Month}/{month.Year}");
                model.MonthlyRevenue.Add(monthlyMap.TryGetValue((month.Year, month.Month), out var m) ? m.Revenue : 0m);
            }

            foreach (var item in topProducts)
            {
                model.TopProductNames.Add(item.Name);
                model.TopProductQuantities.Add(item.Quantity);
            }

            foreach (var status in Enum.GetValues<OrderStatus>())
            {
                model.StatusLabels.Add(status.ToVietnamese());
                model.StatusCounts.Add(statusGroups.FirstOrDefault(g => g.Status == status)?.Count ?? 0);
            }

            return View(model);
        }
    }
}
