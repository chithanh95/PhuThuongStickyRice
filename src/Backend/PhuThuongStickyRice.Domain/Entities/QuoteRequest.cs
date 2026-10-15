using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Bulk / event order request (đặt tiệc, đặt cỗ, số lượng lớn).
    /// No payment is taken for quotes — the shop calls the customer back.
    /// </summary>
    public class QuoteRequest : Entity<Guid>, IAggregateRoot
    {
        public const int CustomerNameMaxLength = 200;
        public const int CustomerPhoneMaxLength = 30;
        public const int NotesMaxLength = 1000;
        public const int QuoteTypeMaxLength = 50;
        public const int EventLocationMaxLength = 500;
        public const int BudgetMaxLength = 100;

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>Planned event date (date part is what matters).</summary>
        public DateTime? EventDate { get; set; }

        /// <summary>Where the event takes place (địa điểm tổ chức tiệc).</summary>
        public string EventLocation { get; set; }

        /// <summary>Expected investment budget (vốn dự kiến nhượng quyền).</summary>
        public string Budget { get; set; }

        /// <summary>Source of the quote: "DatTiec" (party page) or "Landing" (home booking form).</summary>
        public string QuoteType { get; set; }

        /// <summary>Estimated number of servings / packages.</summary>
        public int Quantity { get; set; }

        public string Notes { get; set; }

        public QuoteStatus Status { get; set; } = QuoteStatus.New;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
