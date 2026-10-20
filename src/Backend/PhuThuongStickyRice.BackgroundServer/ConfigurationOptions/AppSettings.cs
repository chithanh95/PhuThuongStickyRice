using PhuThuongStickyRice.Infrastructure.Logging;
using PhuThuongStickyRice.Infrastructure.Notification;
using Microsoft.Extensions.Options;

namespace PhuThuongStickyRice.BackgroundServer.ConfigurationOptions
{
    /// <summary>
    /// Cấu hình ứng dụng của BackgroundServer, bind từ appsettings.json:
    /// chuỗi kết nối, tùy chọn logging, host cho phép, URL hiện tại và cấu hình thông báo.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Các chuỗi kết nối tới database.</summary>
        public ConnectionStrings ConnectionStrings { get; set; }

        /// <summary>Tùy chọn ghi log (mức độ, nơi lưu log...).</summary>
        public LoggingOptions Logging { get; set; }

        /// <summary>Danh sách host được phép gửi request tới worker (ví dụ "localhost", "*").</summary>
        public string AllowedHosts { get; set; }

        /// <summary>URL gốc của web, dùng để tạo link tuyệt đối trong email/thông báo.</summary>
        public string CurrentUrl { get; set; }

        /// <summary>Cấu hình kênh thông báo (email, Telegram...) của worker.</summary>
        public NotificationOptions Notification { get; set; }

        // CA1822: không đọc dữ liệu instance → static (gọi qua AppSettings.Validate()).
        /// <summary>
        /// Kiểm tra tính hợp lệ của cấu hình khi khởi động.
        /// Hiện tại không có ràng buộc bắt buộc nên luôn trả thành công (giữ lại
        /// để sau này thêm kiểm tra bắt buộc mà không phải đổi contract).
        /// </summary>
        /// <returns>ValidateOptionsResult.Success khi cấu hình hợp lệ.</returns>
        public static ValidateOptionsResult Validate()
        {
            return ValidateOptionsResult.Success;
        }
    }

    /// <summary>
    /// Adapter IValidateOptions để hệ thống Options của .NET gọi AppSettings.Validate()
    /// trong quá trình khởi động worker.
    /// </summary>
    public class AppSettingsValidation : IValidateOptions<AppSettings>
    {
        public ValidateOptionsResult Validate(string name, AppSettings options)
        {
            return AppSettings.Validate();
        }
    }
}
