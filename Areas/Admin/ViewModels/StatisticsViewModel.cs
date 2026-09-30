namespace WebBanHang.Areas.Admin.ViewModels
{
    public class StatisticsViewModel
    {
        public decimal TotalRevenue { get; set; }

        public int TotalOrders { get; set; }

        public int CancelledOrders { get; set; }

        public decimal AverageOrderValue { get; set; }

        public int CustomerCount { get; set; }

        // Biểu đồ doanh thu 30 ngày gần nhất
        public List<string> DailyLabels { get; set; } = new List<string>();

        public List<decimal> DailyRevenue { get; set; } = new List<decimal>();

        public List<int> DailyOrders { get; set; } = new List<int>();

        // Biểu đồ doanh thu 12 tháng gần nhất
        public List<string> MonthlyLabels { get; set; } = new List<string>();

        public List<decimal> MonthlyRevenue { get; set; } = new List<decimal>();

        // Top sản phẩm bán chạy
        public List<string> TopProductNames { get; set; } = new List<string>();

        public List<int> TopProductQuantities { get; set; } = new List<int>();

        // Tỉ lệ đơn theo trạng thái
        public List<string> StatusLabels { get; set; } = new List<string>();

        public List<int> StatusCounts { get; set; } = new List<int>();
    }
}
