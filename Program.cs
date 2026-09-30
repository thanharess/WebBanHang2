using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

var builder = WebApplication.CreateBuilder(args);

// Kết nối database SQL Server. Khi chạy thật, đặt chuỗi kết nối bằng biến môi trường
// ConnectionStrings__DefaultConnection thay vì lưu trong appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Chưa cấu hình chuỗi kết nối 'DefaultConnection'. " +
        "Khi chạy thật hãy đặt biến môi trường ConnectionStrings__DefaultConnection.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();

// Session dùng để lưu giỏ hàng tạm thời của khách
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian nhớ giỏ hàng
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Giỏ hàng đọc/ghi qua Session nên cần IHttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, CartService>();

// Lưu ảnh sản phẩm do quản trị viên tải lên
builder.Services.AddSingleton<ImageStorage>();

// Cấu hình thanh toán: tài khoản ngân hàng (VietQR) và cổng VNPay
builder.Services.Configure<BankTransferSettings>(builder.Configuration.GetSection("BankTransfer"));
builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection("VnPay"));
builder.Services.AddSingleton<VnPayService>();

// Đăng nhập bằng cookie: khách đăng nhập để quản lý tài khoản, Admin vào được khu quản trị
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "GolfKing.Auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Đứng sau reverse proxy (IIS/nginx) thì phải đọc IP và giao thức gốc từ header
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// HSTS cho môi trường chạy thật
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(180);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Endpoint để dịch vụ hosting / giám sát kiểm tra website còn sống
app.MapGet("/healthz", () => Results.Text("ok"));

// Khu quản trị nằm trong Areas/Admin nên phải khai báo route này trước route mặc định
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Áp dụng migration còn thiếu rồi nạp dữ liệu mẫu khi khởi động
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Migrate() thay cho EnsureCreated() để mọi migration trong thư mục Migrations
    // đều được áp dụng và có ghi lại lịch sử trong bảng __EFMigrationsHistory.
    context.Database.Migrate();

    DbSeeder.Seed(context);
}

app.Run();