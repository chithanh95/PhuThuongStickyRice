namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    /// <summary>Item 143: first-order voucher for a new customer.</summary>
    public class FirstOrderVoucherDto
    {
        public bool IsNewCustomer { get; set; }

        public string Code { get; set; }

        public int DiscountPercent { get; set; }
    }
}
