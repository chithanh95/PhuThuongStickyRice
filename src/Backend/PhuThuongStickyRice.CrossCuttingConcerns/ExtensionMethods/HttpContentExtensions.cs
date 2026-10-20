using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods
{
    /// <summary>
    /// Extension đọc/ghi nội dung HTTP: deserialize JSON từ response và
    /// tạo body JSON cho request khi gọi API bên ngoài (MoMo, Zalo...).
    /// </summary>
    public static class HttpContentExtensions
    {
        // CA1869: JsonSerializerOptions đắt khi khởi tạo — dùng 1 instance static
        // dùng chung, thread-safe sau khi cấu hình xong.
        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// Đọc body response rồi deserialize JSON thành kiểu <typeparamref name="T"/>
        /// (so tên property không phân biệt hoa/thường). Body rỗng thì trả về default.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cần deserialize.</typeparam>
        /// <param name="httpContent">Nội dung response HTTP.</param>
        /// <returns>Object đã deserialize, hoặc default(T) khi body rỗng.</returns>
        public static async Task<T> ReadAs<T>(this HttpContent httpContent)
        {
            var json = await httpContent.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, CaseInsensitiveOptions);
        }

        /// <summary>
        /// Serialize <paramref name="value"/> thành JSON rồi gói thành
        /// <see cref="StringContent"/> với content-type chỉ định — dùng làm
        /// body khi POST tới API bên ngoài.
        /// </summary>
        /// <param name="value">Object cần gửi đi.</param>
        /// <param name="contentType">Content-Type của body (vd "application/json").</param>
        /// <returns>StringContent chứa JSON đã serialize.</returns>
        public static StringContent AsStringContent(this object value, string contentType)
        {
            var content = new StringContent(JsonSerializer.Serialize(value));
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return content;
        }

        /// <summary>
        /// Như <see cref="AsStringContent"/> nhưng content-type mặc định là
        /// <c>application/json</c> — lối tắt cho các POST JSON thông thường.
        /// </summary>
        /// <param name="value">Object cần gửi đi.</param>
        /// <returns>StringContent JSON với content-type application/json.</returns>
        public static StringContent AsJsonContent(this object value)
        {
            return value.AsStringContent("application/json");
        }
    }
}
