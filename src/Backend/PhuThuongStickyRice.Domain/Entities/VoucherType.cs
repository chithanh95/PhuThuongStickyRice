namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>Voucher kind for <see cref="DiscountCode"/> (item 120).</summary>
    public enum VoucherType
    {
        /// <summary>Classic discount (percent or fixed amount).</summary>
        Discount = 0,

        /// <summary>Freeship: sets the delivery fee to 0 at checkout.</summary>
        Freeship = 1
    }
}
