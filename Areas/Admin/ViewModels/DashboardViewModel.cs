using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.ViewModels
{
    public class DashboardViewModel
    {
        public int ProductCount { get; set; }

        public int CategoryCount { get; set; }

        public int OrderCount { get; set; }

        public int CustomerCount { get; set; }

        public decimal TotalRevenue { get; set; }

        public List<Order> LatestOrders { get; set; } = new List<Order>();
    }
}
