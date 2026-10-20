using System.Text.RegularExpressions;

namespace PhuThuongStickyRice.Web.Components.Shop
{
    /// <summary>
    /// Kiểm tra định dạng SĐT Việt Nam dùng chung cho các trang shop.
    /// Trước đây 8 trang Razor (Checkout, CungGio, DangNhap, DatCung, DatSoLuongLon,
    /// DatTiec, NhuongQuyen, QuenMatKhau) tự copy 1 method kiểm tra
    /// giống hệt nhau — giờ chỉ giữ 1 bản duy nhất ở đây.
    /// (Shop.razor giữ bản riêng vì nó strip ký tự không phải số trước khi check —
    /// ngữ nghĩa khác, cố ý không gộp.)
    /// </summary>
    public static class PhoneValidation
    {
        /// <summary>
        /// SĐT VN hợp lệ: 10 chữ số, bắt đầu bằng số 0, đầu số di động
        /// 03/05/07/08/09 (dải đầu số di động VN).
        /// </summary>
        public static bool IsValidVnPhone(string phone) =>
            !string.IsNullOrWhiteSpace(phone)
            && Regex.IsMatch(phone.Trim(), @"^(0)(3|5|7|8|9)\d{8}$");
    }
}
