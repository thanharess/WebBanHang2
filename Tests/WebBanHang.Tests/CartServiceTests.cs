using Microsoft.AspNetCore.Http;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Tests;

public class CartServiceTests
{
    /// <summary>ISession giả lập lưu trong bộ nhớ để test không cần HTTP thật.</summary>
    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    private static CartService CreateCart(out FakeSession session)
    {
        session = new FakeSession();
        var context = new DefaultHttpContext { Session = session };
        return new CartService(new HttpContextAccessor { HttpContext = context });
    }

    private static CartItem Item(int id, decimal price, int quantity = 1) => new()
    {
        ProductId = id,
        ProductName = $"Sản phẩm {id}",
        Price = price,
        Quantity = quantity,
        ImageUrl = $"/images/products/{id}.png"
    };

    [Fact]
    public void Gio_hang_moi_la_rong()
    {
        var cart = CreateCart(out _);

        Assert.Empty(cart.GetCart());
        Assert.Equal(0, cart.GetItemCount());
        Assert.Equal(0m, cart.GetTotal());
    }

    [Fact]
    public void Them_san_pham_moi_vao_gio()
    {
        var cart = CreateCart(out _);

        cart.Add(Item(1, 100_000m, 2));

        Assert.Single(cart.GetCart());
        Assert.Equal(2, cart.GetItemCount());
        Assert.Equal(200_000m, cart.GetTotal());
    }

    [Fact]
    public void Them_lai_cung_san_pham_thi_cong_don_so_luong()
    {
        var cart = CreateCart(out _);

        cart.Add(Item(1, 100_000m, 2));
        cart.Add(Item(1, 100_000m, 3));

        Assert.Single(cart.GetCart());
        Assert.Equal(5, cart.GetItemCount());
        Assert.Equal(500_000m, cart.GetTotal());
    }

    [Fact]
    public void Sua_so_luong_va_xoa_san_pham()
    {
        var cart = CreateCart(out _);
        cart.Add(Item(1, 100_000m, 2));
        cart.Add(Item(2, 50_000m, 1));

        cart.UpdateQuantity(1, 4);
        Assert.Equal(4, cart.GetQuantity(1));

        cart.Remove(1);
        Assert.Null(cart.GetCart().FirstOrDefault(i => i.ProductId == 1));
        Assert.Equal(50_000m, cart.GetTotal());
    }

    [Fact]
    public void Sua_so_luong_ve_0_thi_xoa_dong_do()
    {
        var cart = CreateCart(out _);
        cart.Add(Item(1, 100_000m, 2));

        cart.UpdateQuantity(1, 0);

        Assert.Empty(cart.GetCart());
    }

    [Fact]
    public void Sua_so_luong_am_cung_xoa_dong_do()
    {
        var cart = CreateCart(out _);
        cart.Add(Item(1, 100_000m, 2));

        cart.UpdateQuantity(1, -5);

        Assert.Empty(cart.GetCart());
    }

    [Fact]
    public void Xoa_toan_bo_gio()
    {
        var cart = CreateCart(out _);
        cart.Add(Item(1, 100_000m, 2));
        cart.Add(Item(2, 50_000m, 1));

        cart.Clear();

        Assert.Empty(cart.GetCart());
        Assert.Equal(0, cart.GetItemCount());
    }

    [Fact]
    public void Gio_hang_duoc_luu_vao_session_nen_doc_lai_van_con()
    {
        var cart = CreateCart(out var session);
        cart.Add(Item(1, 100_000m, 2));

        // Tạo service mới trên cùng session: mô phỏng request tiếp theo của khách
        var context = new DefaultHttpContext { Session = session };
        var cart2 = new CartService(new HttpContextAccessor { HttpContext = context });

        Assert.Single(cart2.GetCart());
        Assert.Equal(200_000m, cart2.GetTotal());
    }

    [Fact]
    public void Thanh_tien_tung_dong_bang_don_gia_nhan_so_luong()
    {
        var item = Item(1, 125_000m, 3);

        Assert.Equal(375_000m, item.TotalPrice);
    }
}
