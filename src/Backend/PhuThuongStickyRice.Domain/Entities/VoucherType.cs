namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>Loại voucher cho <see cref="DiscountCode"/> (item 120).</summary>
    public enum VoucherType
    {
        /// <summary>Voucher giảm giá thông thường (theo % hoặc số tiền cố định).</summary>
        Discount = 0,

        /// <summary>Voucher freeship: phí giao hàng về 0 khi checkout.</summary>
        Freeship = 1
    }
}
