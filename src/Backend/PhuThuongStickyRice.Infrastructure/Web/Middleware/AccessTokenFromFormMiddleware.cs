using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Web.Middleware
{
    // CA1852: class internal, không có subtype — sealed để runtime
    // tối ưu devirtualization và báo rõ ý định không cho kế thừa.
    internal sealed class AccessTokenFromFormMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AccessTokenFromFormMiddleware> _logger;

        public AccessTokenFromFormMiddleware(RequestDelegate next, ILogger<AccessTokenFromFormMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // CA1311: dùng HttpMethods.IsPost thay cho so sánh chuỗi "POST" —
            // IsPost dùng Ordinal comparison (giữ nguyên ngữ nghĩa) và
            // analyzer ASP.NET Core nhận diện đúng ý định HTTP method.
            // CA1307: ContentType là media type (RFC 2045 không phân biệt
            // chữ hoa/thường) nên so sánh OrdinalIgnoreCase thay vì ==.
            if (HttpMethods.IsPost(context.Request.Method)
                && string.Equals(context.Request.ContentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(context.Request.Headers["Authorization"]))
            {
                var token = context.Request.Form["access_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    // ASP0019: dùng indexer thay cho IHeaderDictionary.Add —
                    // Add ném ArgumentException nếu header đã tồn tại; guard
                    // Authorization trống ở trên bảo đảm đây là lần set duy nhất.
                    context.Request.Headers["Authorization"] = "Bearer " + token;
                }
            }

            await _next(context);
        }
    }
}
