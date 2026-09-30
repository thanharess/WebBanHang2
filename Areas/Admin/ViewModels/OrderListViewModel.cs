using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.ViewModels
{
    public class OrderListViewModel
    {
        public List<Order> Orders { get; set; } = new List<Order>();

        public OrderStatus? Status { get; set; }

        public string? Keyword { get; set; }

        public int Page { get; set; } = 1;

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }

        public int AllCount { get; set; }

        public Dictionary<OrderStatus, int> StatusCounts { get; set; } = new Dictionary<OrderStatus, int>();

        public int CountOf(OrderStatus status) => StatusCounts.TryGetValue(status, out var count) ? count : 0;

        public static IEnumerable<OrderStatus> AllStatuses => Enum.GetValues<OrderStatus>();
    }
}
