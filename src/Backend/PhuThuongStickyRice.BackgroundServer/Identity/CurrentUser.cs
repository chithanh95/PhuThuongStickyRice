using PhuThuongStickyRice.Domain.Identity;
using System;

namespace PhuThuongStickyRice.BackgroundServer.Identity
{
    /// <summary>
    /// Triển khai <see cref="ICurrentUser"/> cho BackgroundServer: worker chạy nền
    /// (scheduled job) không gắn với request HTTP nào nên không bao giờ có người
    /// dùng đăng nhập — <c>IsAuthenticated</c> luôn false và <c>UserId</c> luôn
    /// <see cref="Guid.Empty"/>. Các log audit do worker tạo vì thế ghi hệ thống
    /// thay vì ghi user cụ thể.
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => false;

        public Guid UserId => Guid.Empty;
    }
}
