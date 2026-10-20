#nullable enable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.RateLimiterPolicies
{
    /// <summary>
    /// Chính sách rate limit mặc định của WebAPI: user đã đăng nhập giới hạn
    /// theo tên tài khoản (200 req/phút), khách ẩn danh theo IP thật của kết
    /// nối TCP (100 req/phút). Phân vùng theo IP thật (không theo Host header
    /// do client tự gửi) để không thể xoay Host mà bypass rate limit.
    /// </summary>
    public class DefaultRateLimiterPolicy : IRateLimiterPolicy<string>
    {
        // [codequality-smell 2026-10-20] Đặt tên cho 2 ngưỡng rate limit thay
        // vì số trần 200/100 — đổi ngưỡng chỉ cần 1 chỗ, đúng ý nghĩa từng loại client.
        /// <summary>Số request/phút cho user đã đăng nhập.</summary>
        private const int AuthenticatedPermitLimit = 200;
        /// <summary>Số request/phút cho khách ẩn danh (theo IP thật).</summary>
        private const int AnonymousPermitLimit = 100;

        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; } = (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return default;
        };

        /// <summary>
        /// Chọn bucket rate limit cho request: tài khoản đã đăng nhập dùng tên
        /// user; khách ẩn danh dùng <c>Connection.RemoteIpAddress</c> (IP thật
        /// của kết nối TCP), chỉ fallback về Host header khi IP không có.
        /// </summary>
        /// <param name="httpContext">HttpContext của request hiện tại.</param>
        /// <returns>Partition fixed-window: 200 req/phút (đã login), 100 req/phút (ẩn danh).</returns>
        public RateLimitPartition<string> GetPartition(HttpContext httpContext)
        {
            // same policy name and same partition key => will use the same rate limiter instance
            string? partitionKey;

            // Gán ra biến cục bộ để compiler suy được non-null sau pattern
            // `?.IsAuthenticated == true` (truy cập member chain
            // httpContext.User.Identity trực tiếp không được narrow).
            var identity = httpContext.User.Identity;
            if (identity?.IsAuthenticated == true)
            {
                // [codequality-practices 2026-10-20] Identity.Name có thể null
                // với scheme xác thực không gắn name claim — trước đây ém
                // warning bằng null-forgiving (!) nhưng Name null thật sẽ làm
                // GetFixedWindowLimiter ném ArgumentNullException (500 mọi
                // request của user đó). Fallback về IP thật như nhánh ẩn danh;
                // mọi token do JwtTokenService cấp đều có Name nên hành vi
                // với user thật giữ nguyên.
                partitionKey = identity.Name
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? httpContext.Request.Headers.Host.ToString();
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = AuthenticatedPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                    });
            }

            // bug-hunt 2026-10-20: partition theo IP thật của client, KHÔNG theo
            // Host header — Host do client tự gửi nên xoay Host là mỗi giá trị
            // được 1 bucket 100 req/phút riêng, vô hiệu hóa toàn bộ rate limit
            // (brute-force login, spam tạo đơn...). RemoteIpAddress là địa chỉ
            // TCP thật của kết nối; chỉ fallback về Host khi IP không có
            // (transport không cung cấp).
            partitionKey = httpContext.Connection.RemoteIpAddress?.ToString()
                ?? httpContext.Request.Headers.Host.ToString();
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = AnonymousPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                });
        }
    }
}
