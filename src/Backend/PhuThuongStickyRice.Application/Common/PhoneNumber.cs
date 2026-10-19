using System;
using System.Linq;

namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Tiện ích tĩnh thao tác trên số điện thoại (SĐT): chuẩn hóa, so khớp
    /// và che SĐT để hiển thị công khai.
    /// Gom các phiên bản copy-paste trước đây nằm rải rác ở OrderAppService
    /// (PhonesMatch/DigitsOnly/MaskPhoneForDisplay), LoyaltyAppService
    /// (IsPlaceholderPhone) và LandingPublicController (MaskPhone) về 1 chỗ
    /// để sửa 1 lần là áp dụng khắp nơi, tránh các bản sao lệch nhau dần.
    /// </summary>
    public static class PhoneNumber
    {
        /// <summary>
        /// Lấy chuỗi chỉ gồm chữ số từ SĐT: bỏ dấu cách, gạch ngang, dấu chấm,
        /// dấu "+" (mã vùng +84...) — null hoặc rỗng trả về chuỗi rỗng.
        /// </summary>
        /// <param name="phone">SĐT đầu vào, có thể null.</param>
        /// <returns>Chuỗi chữ số, rỗng nếu đầu vào null/trống.</returns>
        public static string DigitsOnly(string phone) =>
            new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>
        /// So khớp 2 SĐT theo kiểu "khoan dung": chỉ giữ lại chữ số rồi so 9
        /// số cuối — "0912 345 678", "+84912345678" và "0912345678" là cùng 1
        /// số. SĐT ít hơn 9 số thì so nguyên chuỗi số. Null/trống không bao
        /// giờ khớp (trả false) để không xác thực nhầm khi thiếu SĐT.
        /// </summary>
        /// <param name="phone1">SĐT thứ nhất, có thể null.</param>
        /// <param name="phone2">SĐT thứ hai, có thể null.</param>
        /// <returns>True khi 2 SĐT được coi là cùng 1 số.</returns>
        public static bool PhonesMatch(string phone1, string phone2)
        {
            if (string.IsNullOrWhiteSpace(phone1) || string.IsNullOrWhiteSpace(phone2))
            {
                return false;
            }

            var digits1 = DigitsOnly(phone1);
            var digits2 = DigitsOnly(phone2);

            if (digits1.Length < 9 || digits2.Length < 9)
            {
                return string.Equals(digits1, digits2, StringComparison.Ordinal);
            }

            return string.Equals(
                digits1.Substring(digits1.Length - 9),
                digits2.Substring(digits2.Length - 9),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Che SĐT để hiển thị công khai: "0901234567" → "090***67".
        /// SĐT null/trống hoặc ít hơn 5 chữ số thì trả "Khách hàng".
        /// </summary>
        /// <param name="phone">SĐT cần che, có thể null.</param>
        /// <returns>SĐT đã che hoặc "Khách hàng".</returns>
        public static string MaskForPublicDisplay(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return "Khách hàng";
            }

            var digits = DigitsOnly(phone);
            if (digits.Length < 5)
            {
                return "Khách hàng";
            }

            // CA1845: dùng span-based string.Concat thay vì Substring để tránh cấp phát chuỗi trung gian.
            return string.Concat(digits.AsSpan(0, 3), "***", digits.AsSpan(digits.Length - 2));
        }
    }
}
