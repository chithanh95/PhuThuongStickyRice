using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Web.Middleware
{
    public partial class LoggingStatusCodeMiddleware
    {
        // CA1848: logging hiệu năng cao bằng LoggerMessage source-generated —
        // tránh boxing params + cấp phát mảng object[] của LoggerExtensions.
        // (Project chưa bật nullable annotations nên signature dùng kiểu
        // không dấu "?" như code gốc — hành vi null của userId/remoteIp giữ nguyên.)
        [LoggerMessage(Level = LogLevel.Warning,
            Message = "StatusCode: {StatusCode}, UserId: {UserId}, Path: {Path}, Method: {Method}, IP: {RemoteIp}")]
        private static partial void LogForbiddenStatus(ILogger logger, int statusCode, string userId, PathString path, string method, object remoteIp);

        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingStatusCodeMiddleware> _logger;

        public LoggingStatusCodeMiddleware(RequestDelegate next, ILogger<LoggingStatusCodeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Log cảnh báo khi response là 401/403. Lưu ý perf: mọi công việc
        /// (lookup claims, nội suy chuỗi log) chỉ chạy khi status là 401/403 —
        /// không cấp phát mảng/LINQ hay đọc claims trên mọi request.
        /// Middleware này phải đăng ký SAU UseAuthentication/UseAuthorization
        /// thì context.User mới có principal để lấy UserId (xem Startup.cs).
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            var statusCode = context.Response.StatusCode;

            // Perf: so sánh trực tiếp thay vì new[] + LINQ Contains trên mọi request.
            if (statusCode == StatusCodes.Status401Unauthorized
                || statusCode == StatusCodes.Status403Forbidden)
            {
                var path = context.Request.Path;
                var method = context.Request.Method;
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value;
                var remoteIp = context.Connection.RemoteIpAddress;

                LogForbiddenStatus(_logger, statusCode, userId, path, method, remoteIp);
            }
        }
    }
}
