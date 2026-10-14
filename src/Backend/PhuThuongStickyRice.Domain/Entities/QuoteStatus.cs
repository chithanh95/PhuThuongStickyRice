namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>Lifecycle of a bulk/event quote request (đặt tiệc).</summary>
    public enum QuoteStatus
    {
        /// <summary>Mới — just submitted.</summary>
        New = 0,

        /// <summary>Đã liên hệ — shop has called the customer back.</summary>
        Contacted = 1,

        /// <summary>Đã chốt — the quote turned into a confirmed deal.</summary>
        Confirmed = 2,

        /// <summary>Hủy — cancelled.</summary>
        Cancelled = 3
    }
}
