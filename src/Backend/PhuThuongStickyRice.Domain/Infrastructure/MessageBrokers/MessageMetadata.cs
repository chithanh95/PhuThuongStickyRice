using System;

namespace PhuThuongStickyRice.Domain.Infrastructure.MessageBrokers
{
    /// <summary>
    /// Metadata kèm theo message gửi qua message broker — dùng để truy vết,
    /// tương thích phiên bản và xác thực phía consumer.
    /// </summary>
    public class MessageMetadata
    {
        /// <summary>Id duy nhất của message — dùng để chống xử lý trùng (idempotency).</summary>
        public string MessageId { get; set; }

        /// <summary>Phiên bản schema của message (để tương thích khi contract thay đổi).</summary>
        public string MessageVersion { get; set; }

        /// <summary>Id tương quan — truy vết 1 luồng xử lý xuyên suốt nhiều message.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Thời điểm message được tạo (theo phía gửi).</summary>
        public DateTimeOffset? CreationDateTime { get; set; }

        /// <summary>Thời điểm message được đưa vào hàng đợi.</summary>
        public DateTimeOffset? EnqueuedDateTime { get; set; }

        /// <summary>Token xác thực kèm theo — để consumer gọi ngược API khi cần.</summary>
        public string AccessToken { get; set; }
    }
}
