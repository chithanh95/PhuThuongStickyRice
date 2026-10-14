namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// How the customer pays for an order.
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>Cash on delivery — pay when the order arrives.</summary>
        Cod = 0,

        /// <summary>Bank transfer via VietQR — customer transfers, admin confirms.</summary>
        BankTransfer = 1,

        /// <summary>MoMo e-wallet (test/simulated until real merchant credentials exist).</summary>
        MoMo = 2
    }
}
