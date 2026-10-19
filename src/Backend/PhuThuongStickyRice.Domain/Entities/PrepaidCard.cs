using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Thẻ xôi sáng trả trước (item 164): khách mua gói 20/30 suất,
    /// mỗi sáng đưa mã để nhận 1 suất, admin trừ lượt.
    /// </summary>
    public class PrepaidCard : Entity<Guid>, IAggregateRoot
    {
        public const int CustomerPhoneMaxLength = 30;
        public const int CardCodeMaxLength = 20;

        /// <summary>Họ tên khách mua thẻ (để nhân viên đối chiếu khi nhận suất).</summary>
        public string CustomerName { get; set; }

        /// <summary>SĐT đã đăng ký thẻ (khóa tra cứu thẻ của khách).</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Mã thẻ duy nhất, khách đưa mã để nhận suất.</summary>
        public string CardCode { get; set; }

        /// <summary>Tổng số suất đã mua.</summary>
        public int TotalSuits { get; set; }

        /// <summary>Số suất đã dùng.</summary>
        public int UsedSuits { get; set; }

        /// <summary>Số suất còn lại (tính tự động, không lưu DB).</summary>
        public int RemainingSuits => TotalSuits - UsedSuits;

        /// <summary>Số tiền (VNĐ) khách đã trả khi mua gói (đã chiết khấu theo gói).</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>Trạng thái thẻ: Active = đang dùng, UsedUp = đã dùng hết, Cancelled = đã hủy.</summary>
        public PrepaidCardStatus Status { get; set; } = PrepaidCardStatus.Active;

        /// <summary>Thời điểm tạo thẻ (UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời điểm thẻ dùng hết suất cuối (UTC); null khi thẻ vẫn còn suất.
        /// Dùng để phân biệt thẻ "đã hoàn thành" khỏi thẻ bị hủy.
        /// </summary>
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>Trạng thái thẻ trả trước.</summary>
    public enum PrepaidCardStatus
    {
        /// <summary>Thẻ còn suất, khách có thể nhận thêm.</summary>
        Active = 0,
        /// <summary>Thẻ đã dùng hết suất (không trừ thêm được).</summary>
        UsedUp = 1,
        /// <summary>Thẻ bị hủy thủ công (không còn hiệu lực).</summary>
        Cancelled = 2
    }
}
