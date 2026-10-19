using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Sự kiện domain chưa được publish (mẫu Outbox).
    /// Lưu vào DB trong cùng transaction với thay đổi nghiệp vụ để đảm bảo
    /// không mất sự kiện khi gửi message ra ngoài thất bại; một job nền
    /// sẽ đọc các bản ghi chưa publish và gửi đi.
    /// </summary>
    public class OutboxEvent : Entity<long>, IAggregateRoot
    {
        public string EventType { get; set; }

        public Guid TriggeredById { get; set; }

        public string ObjectId { get; set; }

        public string Message { get; set; }

        public bool Published { get; set; }
    }
}
