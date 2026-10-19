using System;

namespace PhuThuongStickyRice.Application.AuditLogs.DTOs
{
    /// <summary>
    /// Một bản ghi nhật ký audit: ai (UserId) đã làm gì (Action) trên đối tượng
    /// nào (ObjectId, dạng chuỗi vì có thể là Guid hoặc mã nghiệp vụ) và nội
    /// dung chi tiết (Log, thường là JSON). Dùng ở trang tra cứu audit của admin.
    /// </summary>
    public class AuditLogEntryDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Action { get; set; }

        public string ObjectId { get; set; }

        public string Log { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
    }
}
