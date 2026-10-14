namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Payment state of an order, independent from the fulfilment <see cref="OrderStatus"/>.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>Not paid yet (COD awaiting delivery, or bank transfer / MoMo not started).</summary>
        Unpaid = 0,

        /// <summary>Customer says the bank transfer was sent; awaiting admin confirmation.</summary>
        PendingConfirmation = 1,

        /// <summary>Payment confirmed (admin confirmed the transfer, or MoMo reported success).</summary>
        Paid = 2,

        /// <summary>Payment failed (MoMo reported failure).</summary>
        Failed = 3
    }
}
