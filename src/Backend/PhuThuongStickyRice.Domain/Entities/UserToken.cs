using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Token lưu cho một <see cref="User"/> theo từng nhà cung cấp đăng nhập
    /// (ví dụ token từ đăng nhập ngoài như Google/Facebook, hoặc refresh token):
    /// mỗi token được định danh bởi cặp <see cref="LoginProvider"/> + <see cref="TokenName"/>.
    /// </summary>
    public class UserToken : Entity<Guid>
    {
        /// <summary>Id của user sở hữu token.</summary>
        public Guid UserId { get; set; }

        /// <summary>Tên nhà cung cấp đăng nhập cấp token (ví dụ "Google", "Facebook").</summary>
        public string LoginProvider { get; set; }

        /// <summary>Tên loại token (ví dụ "access_token", "refresh_token").</summary>
        public string TokenName { get; set; }

        /// <summary>Giá trị token đã lưu.</summary>
        public string TokenValue { get; set; }
    }
}
