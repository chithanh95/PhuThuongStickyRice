namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Refund state of an order (item 130). The money movement itself is
    /// manual (cash / bank / wallet credit); this only records the state.
    /// </summary>
    public enum RefundStatus
    {
        /// <summary>No refund requested.</summary>
        None = 0,

        /// <summary>Refund requested, not yet paid out.</summary>
        Requested = 1,

        /// <summary>Refund paid out (recorded).</summary>
        Refunded = 2
    }
}
