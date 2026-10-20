using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Trạng thái xử lý phiếu hỗ trợ: Mới tiếp nhận → Đang xử lý → Đã xong.
    /// </summary>
    public enum SupportTicketStatus
    {
        /// <summary>Phiếu mới, admin chưa xem.</summary>
        New = 0,
        /// <summary>Admin đang xử lý.</summary>
        InProgress = 1,
        /// <summary>Đã trả lời/xử lý xong.</summary>
        Done = 2
    }

    /// <summary>
    /// Phiếu hỗ trợ của khách gửi từ form public /ho-tro (không cần đăng nhập).
    /// </summary>
    public class SupportTicket : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int PhoneMaxLength = 30;
        public const int OrderNumberMaxLength = 30;
        public const int MessageMaxLength = 2000;
        public const int ReplyNoteMaxLength = 2000;

        public string Name { get; set; }

        public string Phone { get; set; }

        /// <summary>Mã đơn liên quan (nếu khách gửi kèm); không bắt buộc.</summary>
        public string OrderNumber { get; set; }

        public string Message { get; set; }

        public SupportTicketStatus Status { get; set; }

        /// <summary>Nội dung admin trả lời (lưu nội bộ, có thể hiện cho khách).</summary>
        public string ReplyNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RepliedAt { get; set; }
    }
}
