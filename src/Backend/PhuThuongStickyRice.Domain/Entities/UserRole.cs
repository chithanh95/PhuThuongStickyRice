using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Bảng liên kết nhiều-nhiều giữa <see cref="User"/> và <see cref="Role"/>:
    /// mỗi bản ghi ghi nhận một user được gán một vai trò.
    /// </summary>
    public class UserRole : Entity<Guid>
    {
        /// <summary>Id của user được gán vai trò.</summary>
        public Guid UserId { get; set; }

        /// <summary>Id của vai trò được gán.</summary>
        public Guid RoleId { get; set; }

        /// <summary>User trong liên kết này.</summary>
        public User User { get; set; }

        /// <summary>Vai trò trong liên kết này.</summary>
        public Role Role { get; set; }
    }
}
