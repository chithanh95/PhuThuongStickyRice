using PhuThuongStickyRice.Domain.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace PhuThuongStickyRice.Infrastructure.Identity
{
    public class CurrentWebUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _context;

        public CurrentWebUser(IHttpContextAccessor context)
        {
            _context = context;
        }

        public bool IsAuthenticated
        {
            get
            {
                return _context.HttpContext.User.Identity.IsAuthenticated;
            }
        }

        public Guid UserId
        {
            get
            {
                var userId = _context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? _context.HttpContext.User.FindFirst("sub")?.Value;

                // Claim có thể thiếu (null) hoặc không phải GUID nếu token lạ —
                // Guid.Parse khi đó ném ArgumentNullException/FormatException rất khó chẩn đoán.
                // Dùng TryParse và báo lỗi 401 rõ ràng thay vì 500 mập mờ.
                if (!Guid.TryParse(userId, out var id))
                {
                    throw new UnauthorizedAccessException("Không xác định được người dùng từ token đăng nhập.");
                }

                return id;
            }
        }
    }
}
