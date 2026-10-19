using PhuThuongStickyRice.Domain.Notification;
using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Email chờ gửi trong hệ thống (hàng đợi gửi mail).
    /// Gửi mail không đồng bộ: email được lưu vào DB trước, BackgroundServer
    /// lấy ra gửi dần, đếm số lần thử (AttemptCount) và lên lịch thử lại
    /// (NextAttemptDateTime) thay vì gửi trực tiếp từ request web.
    /// </summary>
    public class EmailMessage : Entity<Guid>, IAggregateRoot, IEmailMessage
    {
        public string From { get; set; }

        public string Tos { get; set; }

        public string CCs { get; set; }

        public string BCCs { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public int AttemptCount { get; set; }

        public int MaxAttemptCount { get; set; }

        public DateTimeOffset? NextAttemptDateTime { get; set; }

        public DateTimeOffset? ExpiredDateTime { get; set; }

        public string Log { get; set; }

        public DateTimeOffset? SentDateTime { get; set; }

        public Guid? CopyFromId { get; set; }
    }
}
