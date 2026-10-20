using PhuThuongStickyRice.Infrastructure.Web.Middleware;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Các extension đăng ký middleware hạ tầng của hệ thống vào pipeline
    /// ASP.NET Core. Đặt trong namespace <c>Microsoft.AspNetCore.Builder</c>
    /// để gọi trực tiếp trên <c>IApplicationBuilder</c> trong Startup như
    /// middleware có sẵn của framework.
    /// </summary>
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        /// Ghi log địa chỉ IP nguồn của mỗi request — hạ tầng phục vụ việc
        /// lọc/chặn IP khi cần mở rộng sau này.
        /// </summary>
        public static IApplicationBuilder UseIPFiltering(this IApplicationBuilder app)
        {
            app.UseMiddleware<IPFilteringMiddleware>();
            return app;
        }

        /// <summary>
        /// Gắn các security header (CSP, X-Frame-Options...) vào mọi response.
        /// </summary>
        /// <param name="headers">Danh sách tên header và giá trị cần gắn.</param>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, Dictionary<string, string> headers)
        {
            app.UseMiddleware<SecurityHeadersMiddleware>(headers);
            return app;
        }

        /// <summary>
        /// Điểm móc middleware gỡ lỗi trong pipeline — nơi gắn logic
        /// chẩn đoán tạm thời mà không phải sửa code xử lý chính.
        /// </summary>
        public static IApplicationBuilder UseDebuggingMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<DebuggingMiddleware>();
            return app;
        }

        /// <summary>
        /// Bắt mọi exception chưa được xử lý và chuyển thành response JSON lỗi
        /// chuẩn — tránh lộ stack trace chi tiết ra ngoài production.
        /// </summary>
        /// <param name="options">Tùy chọn xử lý lỗi; null thì dùng cấu hình mặc định.</param>
        public static IApplicationBuilder UseGlobalExceptionHandlerMiddleware(this IApplicationBuilder app, GlobalExceptionHandlerMiddlewareOptions options = default)
        {
            options ??= new GlobalExceptionHandlerMiddlewareOptions();
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>(options);
            return app;
        }

        /// <summary>
        /// Bắt mọi exception chưa được xử lý và chuyển thành response JSON lỗi
        /// chuẩn — tránh lộ stack trace chi tiết ra ngoài production.
        /// </summary>
        /// <param name="configureOptions">Delegate tùy chỉnh cấu hình xử lý lỗi.</param>
        public static IApplicationBuilder UseGlobalExceptionHandlerMiddleware(this IApplicationBuilder app, Action<GlobalExceptionHandlerMiddlewareOptions> configureOptions)
        {
            var options = new GlobalExceptionHandlerMiddlewareOptions();
            configureOptions(options);
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>(options);
            return app;
        }

        /// <summary>
        /// Ghi log cảnh báo khi response trả về 401/403 — phục vụ phát hiện
        /// truy cập trái phép hoặc token hết hạn/hỏng.
        /// </summary>
        public static IApplicationBuilder UseLoggingStatusCodeMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<LoggingStatusCodeMiddleware>();
            return app;
        }

        /// <summary>
        /// Với request POST dạng form-urlencoded thiếu header Authorization,
        /// lấy <c>access_token</c> trong form gắn thành header Bearer — phục vụ
        /// các client form POST (ví dụ form đăng nhập) không tự gắn header được.
        /// </summary>
        public static IApplicationBuilder UseAccessTokenFromFormMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<AccessTokenFromFormMiddleware>();
            return app;
        }
    }
}
