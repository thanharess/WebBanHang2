using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace WebBanHang.Helpers
{
    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        /// <summary>
        /// Đọc dữ liệu từ Session. Trả về null nếu key chưa tồn tại.
        /// </summary>
        public static T? Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}