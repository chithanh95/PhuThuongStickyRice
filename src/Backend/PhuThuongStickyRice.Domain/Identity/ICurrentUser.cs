using System;

namespace PhuThuongStickyRice.Domain.Identity
{
    /// <summary>
    /// Thông tin người dùng đang đăng nhập (được resolve từ HttpContext ở
    /// WebAPI, hoặc từ context test ở unit test). Dùng ở domain để gắn
    /// người tạo/sửa mà không phụ thuộc web.
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// Đã đăng nhập chưa; false thì <see cref="UserId"/> không hợp lệ
        /// và không được dùng.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Id của user đang đăng nhập; chỉ hợp lệ khi
        /// <see cref="IsAuthenticated"/> = true.
        /// </summary>
        Guid UserId { get; }
    }
}
