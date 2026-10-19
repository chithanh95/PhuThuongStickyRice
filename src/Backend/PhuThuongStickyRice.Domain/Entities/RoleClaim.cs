using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Claim (cặp loại-giá trị) gắn với một <see cref="Role"/>. User được gán
    /// vai trò sẽ thừa hưởng các claim này khi kiểm tra quyền.
    /// </summary>
    public class RoleClaim : Entity<Guid>
    {
        /// <summary>Loại claim (ví dụ: "Permission", "Role").</summary>
        public string Type { get; set; }

        /// <summary>Giá trị claim (ví dụ tên quyền cụ thể).</summary>
        public string Value { get; set; }

        /// <summary>Vai trò sở hữu claim này.</summary>
        public Role Role { get; set; }
    }
}
