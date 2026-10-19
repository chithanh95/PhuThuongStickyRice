using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Nhật ký hành động của nhân viên/admin (ai làm gì, trên đối tượng nào, lúc nào).
    /// Dùng để truy vết sau sự cố — vd ai đã yêu cầu/xác nhận hoàn tiền, ai sửa giá món.
    /// Ghi bằng StaffActivityLogger; không cho sửa/xóa qua API để giữ tính toàn vẹn.
    /// </summary>
    public class AuditLogEntry : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Id user (nhân viên/admin) thực hiện hành động.</summary>
        public Guid UserId { get; set; }

        /// <summary>Mã hành động (vd OrderRefundRequest) — xem StaffActivityActions.</summary>
        public string Action { get; set; }

        /// <summary>Id đối tượng bị tác động (vd mã đơn hàng), dạng chuỗi để dùng chung mọi entity.</summary>
        public string ObjectId { get; set; }

        /// <summary>Mô tả chi tiết bằng tiếng Việt (vd "Yêu cầu hoàn tiền đơn PX-20261019-0001").</summary>
        public string Log { get; set; }
    }
}
