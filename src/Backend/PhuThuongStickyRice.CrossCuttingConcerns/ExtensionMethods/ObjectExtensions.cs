using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods
{
    public static class ObjectExtensions
    {
        // CA1869: JsonSerializerOptions đắt khi khởi tạo — dùng 1 instance static
        // dùng chung, thread-safe sau khi cấu hình xong.
        private static readonly JsonSerializerOptions PrettyPrintOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        public static string AsJsonString(this object value)
        {
            var content = JsonSerializer.Serialize(value, PrettyPrintOptions);
            return content;
        }
    }
}
