using System;
using System.Linq;
using System.Text.RegularExpressions;

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
    public static partial class PhoneNumber
    {
        /// <summary>
        /// Số chữ số cuối dùng để so khớp "khoan dung" 2 SĐT (bỏ mã vùng đầu số):
        /// "0912 345 678", "+84912345678" và "0912345678" được coi là cùng 1 số.
        /// SĐT ít hơn số này thì so nguyên chuỗi chữ số.
        /// </summary>
        public const int TailDigitCount = 9;

        /// <summary>
        /// Lấy chuỗi chỉ gồm chữ số từ SĐT: bỏ dấu cách, gạch ngang, dấu chấm,
        /// dấu "+" (mã vùng +84...) — null hoặc rỗng trả về chuỗi rỗng.
        /// </summary>
        /// <param name="phone">SĐT đầu vào, có thể null.</param>
        /// <returns>Chuỗi chữ số, rỗng nếu đầu vào null/trống.</returns>
        public static string DigitsOnly(string phone) =>
            new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>
        /// So khớp 2 SĐT theo kiểu "khoan dung": chỉ giữ lại chữ số rồi so
        /// <see cref="TailDigitCount"/> số cuối — "0912 345 678", "+84912345678"
        /// và "0912345678" là cùng 1 số. SĐT ít hơn <see cref="TailDigitCount"/>
        /// số thì so nguyên chuỗi số. Null/trống không bao giờ khớp (trả false)
        /// để không xác thực nhầm khi thiếu SĐT.
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

            if (digits1.Length < TailDigitCount || digits2.Length < TailDigitCount)
            {
                return string.Equals(digits1, digits2, StringComparison.Ordinal);
            }

            return string.Equals(
                digits1.Substring(digits1.Length - TailDigitCount),
                digits2.Substring(digits2.Length - TailDigitCount),
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

        /// <summary>
        /// Khóa chuẩn hóa cho <c>KeyedAsyncLock</c> theo SĐT: chỉ giữ
        /// <see cref="TailDigitCount"/> số cuối (cùng độ chi tiết
        /// <see cref="PhonesMatch"/>), để 2 request cùng 1 SĐT nhưng khác
        /// format ("0912 345 678" vs "+84912345678") vẫn rơi vào cùng 1 khóa.
        /// Null/trống cho khóa rỗng — caller tự đảm bảo SĐT hợp lệ trước.
        /// </summary>
        /// <param name="phone">SĐT đầu vào, có thể null.</param>
        /// <returns>Chuỗi chữ số (tối đa <see cref="TailDigitCount"/> số cuối).</returns>
        public static string PhoneTailKey(string? phone)
        {
            var digits = DigitsOnly(phone);
            return digits.Length > TailDigitCount
                ? digits.Substring(digits.Length - TailDigitCount)
                : digits;
        }

        // Best practice .NET 7+/C# 13: [GeneratedRegex] biên dịch regex ngay
        // lúc build thay vì parse mỗi lần gọi (dùng chung ở IsValidVn).
        [GeneratedRegex(@"^(0)(3|5|7|8|9)\d{8}$")]
        private static partial Regex VnPhoneRegex();

        /// <summary>
        /// Kiểm tra SĐT Việt Nam hợp lệ: bắt đầu bằng 0, đầu số 3/5/7/8/9,
        /// tổng 10 chữ số. Bỏ khoảng trắng 2 đầu trước khi kiểm tra.
        /// Gom 4 bản private IsValidVnPhone/IsValidPhone trước đây nằm rải
        /// rác ở AuthController, LandingPublicController,
        /// CustomerProfileAppService và QuoteRequestAppService — cùng pattern,
        /// mọi call site đều đã Trim() trước nên ngữ nghĩa giữ nguyên.
        /// </summary>
        /// <param name="phone">SĐT đầu vào, có thể null.</param>
        /// <returns>True khi là SĐT VN hợp lệ.</returns>
        public static bool IsValidVn(string? phone) =>
            !string.IsNullOrWhiteSpace(phone) && VnPhoneRegex().IsMatch(phone.Trim());
    }
}
