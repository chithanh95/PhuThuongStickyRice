using PhuThuongStickyRice.Infrastructure.Interceptors;
using PhuThuongStickyRice.Infrastructure.Logging;
using PhuThuongStickyRice.Infrastructure.Monitoring;
using System.Collections.Generic;

namespace PhuThuongStickyRice.WebAPI.ConfigurationOptions
{
    /// <summary>
    /// Root cấu hình của WebAPI — bind trực tiếp từ appsettings.json trong
    /// <c>Startup</c>. Tên property phải khớp tên section trong JSON.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Connection string tới SQL Server.</summary>
        public ConnectionStrings ConnectionStrings { get; set; }

        /// <summary>Cấu hình logging (Serilog/file/console).</summary>
        public LoggingOptions Logging { get; set; }

        /// <summary>Cấu hình monitoring/health-check.</summary>
        public MonitoringOptions Monitoring { get; set; }

        /// <summary>Cấu hình xác thực IdentityServer/JWT.</summary>
        public IdentityServerAuthentication IdentityServerAuthentication { get; set; }

        /// <summary>Host được phép (dùng cho host filtering), phân tách bằng dấu chấm phẩy.</summary>
        public string AllowedHosts { get; set; }

        // Tên property giữ nguyên "CORS" để bind đúng section "CORS" trong appsettings.json.
        public CorsOptions CORS { get; set; }

        /// <summary>
        /// Security headers (vd: X-Content-Type-Options) ghi vào response —
        /// key là tên header, value là giá trị header.
        /// </summary>
        public Dictionary<string, string> SecurityHeaders { get; set; }

        /// <summary>Cấu hình bật/tắt các interceptor (auditing, validation...).</summary>
        public InterceptorsOptions Interceptors { get; set; }

        /// <summary>Chứng thư dùng cho mã hóa setting nhạy cảm.</summary>
        public CertificatesOptions Certificates { get; set; }
    }
}
