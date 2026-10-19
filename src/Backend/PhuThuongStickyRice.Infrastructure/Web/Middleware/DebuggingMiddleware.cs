using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Web.Middleware
{
    public class DebuggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DebuggingMiddleware> _logger;

        public DebuggingMiddleware(RequestDelegate next, ILogger<DebuggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
        }
    }
}
