namespace WebBanHang.Services
{
    /// <summary>
    /// Lưu ảnh do quản trị viên tải lên vào wwwroot/images và xoá ảnh cũ khi cần.
    /// </summary>
    public class ImageStorage
    {
        public const string ProductsFolder = "products";

        private const long MaxFileSize = 2 * 1024 * 1024; // 2 MB

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private readonly IWebHostEnvironment _environment;

        public ImageStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>Lưu file và trả về đường dẫn tương đối, ví dụ /images/products/abc123.jpg</summary>
        public async Task<string> SaveAsync(IFormFile file, string subFolder = ProductsFolder)
        {
            if (file.Length == 0)
            {
                throw new InvalidOperationException("File ảnh rỗng.");
            }

            if (file.Length > MaxFileSize)
            {
                throw new InvalidOperationException("Ảnh không được lớn hơn 2 MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .webp hoặc .gif.");
            }

            var folder = Path.Combine(_environment.WebRootPath, "images", subFolder);
            Directory.CreateDirectory(folder);

            // Tên file do hệ thống sinh ra nên không thể bị lợi dụng để ghi ra ngoài thư mục
            var fileName = Guid.NewGuid().ToString("N") + extension;
            var fullPath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/{subFolder}/{fileName}";
        }

        /// <summary>Xoá ảnh đã lưu. Chỉ xoá file thật sự nằm trong wwwroot/images.</summary>
        public void Delete(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl) ||
                !relativeUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var imagesRoot = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "images"));
            var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relativeUrl.TrimStart('/')));

            if (fullPath.StartsWith(imagesRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
