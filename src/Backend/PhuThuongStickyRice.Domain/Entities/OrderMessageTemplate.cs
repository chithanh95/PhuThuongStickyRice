using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Item 162: message template per order status. When staff changes an
    /// order's status, they can copy the matching template (with placeholders
    /// filled from the order) and send it manually via Zalo/SMS — the system
    /// does NOT auto-send, so the copy flow stays honest.
    /// Placeholders: {MaDon}, {TenKhach}, {TongTien}, {NgayDat}.
    /// </summary>
    public class OrderMessageTemplate : Entity<Guid>, IAggregateRoot
    {
        public const int StatusMaxLength = 50;
        public const int TemplateMaxLength = 2000;

        /// <summary>OrderStatus name, e.g. "Confirmed", "Preparing", "Completed", "Cancelled".</summary>
        public string Status { get; set; }

        public string Template { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
