using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Vai trò người dùng trong hệ thống phân quyền (ví dụ: Admin, Staff).
    /// Là aggregate root: gom các <see cref="RoleClaim"/> (claim của vai trò)
    /// và các <see cref="UserRole"/> (liên kết user được gán vai trò này).
    /// </summary>
    public class Role : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Tên hiển thị của vai trò.</summary>
        public virtual string Name { get; set; }

        /// <summary>Tên đã chuẩn hóa (chữ hoa) dùng để tra cứu và so sánh vai trò.</summary>
        public virtual string NormalizedName { get; set; }

        /// <summary>Mã chống ghi đè đồng thời (optimistic concurrency).</summary>
        public virtual string ConcurrencyStamp { get; set; }

        /// <summary>Các claim (quyền) thuộc về vai trò này.</summary>
        public IList<RoleClaim> Claims { get; set; }

        /// <summary>Các liên kết tới user đang được gán vai trò này.</summary>
        public IList<UserRole> UserRoles { get; set; }
    }
}
