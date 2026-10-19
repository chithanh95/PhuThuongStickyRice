#nullable enable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.RateLimiterPolicies
{
    public class DefaultRateLimiterPolicy : IRateLimiterPolicy<string>
    {
        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; } = (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return default;
        };

        public RateLimitPartition<string> GetPartition(HttpContext httpContext)
        {
            // same policy name and same partition key => will use the same rate limiter instance
            string? partitionKey;

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                partitionKey = httpContext.User.Identity.Name!;
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 200,
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
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                });
        }
    }
}
