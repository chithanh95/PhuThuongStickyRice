using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Nhật ký mỗi lần bắn webhook ra ngoài (item 110): lưu kết quả từng
    /// attempt để debug khi đối tác (Zalo OA, shipper...) báo không nhận được
    /// sự kiện — gồm HTTP status và lỗi trả về.
    /// </summary>
    public class WebhookDelivery : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Độ dài tối đa của tên sự kiện.</summary>
        public const int EventMaxLength = 100;

        /// <summary>Thời điểm thực hiện lần bắn webhook này (UTC).</summary>
        public DateTime At { get; set; }

        /// <summary>Id của endpoint nhận webhook (trỏ sang WebhookEndpoint).</summary>
        public Guid EndpointId { get; set; }

        /// <summary>Tên endpoint (lưu dư để xem log không cần join).</summary>
        public string EndpointName { get; set; }

        /// <summary>Tên sự kiện đã bắn (ví dụ "order.created").</summary>
        public string Event { get; set; }

        /// <summary>True = phía nhận trả về thành công (HTTP 2xx).</summary>
        public bool Success { get; set; }

        /// <summary>Mã HTTP trả về; null khi không gọi được tới endpoint (lỗi mạng/DNS).</summary>
        public int? StatusCode { get; set; }

        /// <summary>Thông điệp lỗi (exception message hoặc body lỗi) khi bắn thất bại; null khi thành công.</summary>
        public string Error { get; set; }
    }
}
