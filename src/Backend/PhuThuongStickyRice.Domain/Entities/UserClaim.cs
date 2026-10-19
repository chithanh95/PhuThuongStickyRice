using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Claim (cặp loại-giá trị) gắn trực tiếp với một <see cref="User"/>,
    /// dùng cho quyền/đặc tính riêng của từng user ngoài các claim từ vai trò.
    /// </summary>
    public class UserClaim : Entity<Guid>
    {
        /// <summary>Loại claim.</summary>
        public string Type { get; set; }

        /// <summary>Giá trị claim.</summary>
        public string Value { get; set; }

        /// <summary>User sở hữu claim này.</summary>
        public User User { get; set; }
    }
}
