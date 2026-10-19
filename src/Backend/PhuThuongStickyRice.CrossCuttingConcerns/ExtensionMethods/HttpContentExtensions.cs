using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods
{
    public static class HttpContentExtensions
    {
        // CA1869: JsonSerializerOptions đắt khi khởi tạo — dùng 1 instance static
        // dùng chung, thread-safe sau khi cấu hình xong.
        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public static async Task<T> ReadAs<T>(this HttpContent httpContent)
        {
            var json = await httpContent.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, CaseInsensitiveOptions);
        }

        public static StringContent AsStringContent(this object value, string contentType)
        {
            var content = new StringContent(JsonSerializer.Serialize(value));
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return content;
        }

        public static StringContent AsJsonContent(this object value)
        {
            return value.AsStringContent("application/json");
        }
    }
}
