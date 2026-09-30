using System.Security.Cryptography;

namespace WebBanHang.Helpers
{
    /// <summary>
    /// Băm và kiểm tra mật khẩu bằng PBKDF2-SHA256 (thuật toán có sẵn trong .NET,
    /// không cần thêm package như BCrypt hay ASP.NET Identity).
    /// Định dạng chuỗi lưu trong database: "số vòng lặp.saltBase64.hashBase64".
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;  // 128-bit
        private const int KeySize = 32;   // 256-bit
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public static string Hash(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            var parts = passwordHash.Split('.', 3);
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations <= 0)
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(parts[1]);
                var expected = Convert.FromBase64String(parts[2]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);

                // So sánh thời gian cố định để tránh tấn công đo thời gian
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
