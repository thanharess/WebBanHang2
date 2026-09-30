using Microsoft.EntityFrameworkCore;
using WebBanHang.Helpers;
using WebBanHang.Models;

namespace WebBanHang.Data
{
    /// <summary>
    /// Nạp dữ liệu mẫu để website có sản phẩm hiển thị ngay sau khi tạo database.
    /// Mỗi phần đều kiểm tra dữ liệu đã tồn tại chưa nên gọi lại nhiều lần vẫn an toàn.
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Mật khẩu mặc định của tài khoản quản trị tạo sẵn.
        /// CHỈ DÙNG CHO MÔI TRƯỜNG PHÁT TRIỂN - hãy đổi ngay khi triển khai thật.
        /// </summary>
        public const string DefaultAdminEmail = "admin@golfking.vn";
        public const string DefaultAdminPassword = "Admin@123";

        public static void Seed(AppDbContext context)
        {
            SeedCatalog(context);
            SeedAdminUser(context);
        }

        private static void SeedCatalog(AppDbContext context)
        {
            if (context.Categories.Any())
            {
                return;
            }

            var category = new Category
            {
                Name = "Dụng cụ ",
                Description = "Gậy, bóng và phụ kiện "
            };

            context.Categories.Add(category);
            context.SaveChanges(); // Lưu trước để lấy được category.Id

            context.Products.AddRange(
                new Product
                {
                    Name = "Gậy",
                    Price = 12500000,
                    StockQuantity = 10,
                    Description = "Gậy chính hãng đời mới",
                    CategoryId = category.Id,
                    ImageUrl = "https://via.placeholder.com/300x200"
                },
                new Product
                {
                    Name = "Túi đựng",
                    Price = 3500000,
                    StockQuantity = 15,
                    Description = "Túi da cao cấp chống thấm nước",
                    CategoryId = category.Id,
                    ImageUrl = "https://via.placeholder.com/300x200"
                }
            );

            context.SaveChanges();
        }

        private static void SeedAdminUser(AppDbContext context)
        {
            if (context.Users.Any(u => u.Email == DefaultAdminEmail))
            {
                return;
            }

            context.Users.Add(new User
            {
                FullName = "Quản trị viên",
                Email = DefaultAdminEmail,
                PasswordHash = PasswordHasher.Hash(DefaultAdminPassword),
                PhoneNumber = "0900000000",
                Address = "Hà Nội",
                Role = UserRole.Admin,
                IsActive = true
            });

            context.SaveChanges();
        }
    }
}
