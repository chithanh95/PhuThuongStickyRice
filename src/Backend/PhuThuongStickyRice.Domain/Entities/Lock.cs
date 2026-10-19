using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Khóa phân tán (distributed lock) dùng để chống 2 tiến trình/thread
    /// xử lý cùng 1 entity cùng lúc (ví dụ 2 job nền cùng chốt ca).
    /// Khóa có thời hạn (ExpiredDateTime) để tự giải phóng nếu tiến trình
    /// giữ khóa bị treo.
    /// </summary>
    public class Lock
    {
        public string EntityId { get; set; }

        public string EntityName { get; set; }

        public string OwnerId { get; set; }

        public DateTimeOffset? AcquiredDateTime { get; set; }

        public DateTimeOffset? ExpiredDateTime { get; set; }
    }
}
