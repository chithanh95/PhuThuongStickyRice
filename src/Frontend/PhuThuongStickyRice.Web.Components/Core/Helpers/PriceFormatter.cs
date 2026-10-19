namespace PhuThuongStickyRice.Web.Components.Core.Helpers
{
    /// <summary>
    /// Format tiền tệ dùng chung cho toàn bộ web (trước đây mỗi trang Razor
    /// tự copy 1 method <c>FormatPrice</c> giống hệt nhau). Định dạng "1.234.567 ₫".
    /// </summary>
    public static class PriceFormatter
    {
        /// <summary>Format số tiền VNĐ: nhóm nghìn, không thập phân, kèm ký hiệu ₫.</summary>
        public static string FormatPrice(decimal price) => $"{price:N0} ₫";
    }
}
