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
        MoMo = 2,

        /// <summary>Customer prepaid wallet balance (item 140).</summary>
        Wallet = 3,

        /// <summary>Pay later — customer pays on next order or via transfer (item 50). Requires >= 5 completed orders.</summary>
        PayLater = 4
    }
}
