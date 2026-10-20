#nullable enable
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
        /// Mã quốc gia Việt Nam trong SĐT dạng quốc tế ("+84...").
        /// </summary>
        public const string VietnamCountryCode = "84";

        /// <summary>
        /// Tổng số chữ số của SĐT di động VN dạng quốc tế: mã quốc gia
        /// "84" + 9 số thuê bao.
        /// </summary>
        public const int InternationalMobileDigitCount = 11;

        /// <summary>
        /// Thông báo lỗi khi SĐT nhập vào không chuẩn hóa được (sai định
        /// dạng). Dùng chung cho mọi service validate SĐT để message nhất
        /// quán, sửa 1 chỗ là đổi khắp nơi.
        /// </summary>
        public const string InvalidPhoneMessage = "Số điện thoại không hợp lệ.";

        /// <summary>
        /// Thông báo lỗi khi thiếu SĐT bắt buộc. Dùng chung cho các service
        /// yêu cầu SĐT để message nhất quán.
        /// </summary>
        public const string PhoneRequiredMessage = "Vui lòng nhập số điện thoại.";

        /// <summary>
        /// Lấy chuỗi chỉ gồm chữ số từ SĐT: bỏ dấu cách, gạch ngang, dấu chấm,
        /// dấu "+" (mã vùng +84...) — null hoặc rỗng trả về chuỗi rỗng.
        /// </summary>
        /// <param name="phone">SĐT đầu vào, có thể null.</param>
        /// <returns>Chuỗi chữ số, rỗng nếu đầu vào null/trống.</returns>
        public static string DigitsOnly(string? phone) =>
            new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>
        /// Chuẩn hóa SĐT về dạng nội địa 10 số để lưu/khớp với dữ liệu nội bộ:
        /// tách chữ số, rồi chuyển "84"+9 số thuê bao → "0"+9 số
        /// ("+84912345678" → "0912345678"). Input null/trống → chuỗi rỗng.
        /// Gom 2 bản copy-paste trước đây ở BlacklistAppService và
        /// RewardRuleAppService về 1 chỗ để sửa 1 lần là áp dụng khắp nơi.
        /// </summary>
        /// <param name="phone">SĐT đầu vào, có thể null.</param>
        /// <returns>SĐT nội địa chỉ gồm chữ số; rỗng nếu đầu vào null/trống.</returns>
        public static string NormalizeToLocalDigits(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            // Chuỗi toàn chữ số nên so sánh bằng Ordinal: đúng + nhanh, không
            // phụ thuộc culture (so sánh mặc định là culture-sensitive).
            // CA1845: span-based Concat thay cho Substring — không cấp phát chuỗi trung gian.
            if (digits.StartsWith(VietnamCountryCode, StringComparison.Ordinal)
                && digits.Length == InternationalMobileDigitCount)
            {
                digits = string.Concat("0".AsSpan(), digits.AsSpan(VietnamCountryCode.Length));
            }

            return digits;
        }

        /// <summary>
        /// So khớp 2 SĐT theo kiểu "khoan dung": chỉ giữ lại chữ số rồi so
        /// <see cref="TailDigitCount"/> số cuối — "0912 345 678", "+84912345678"
        /// và "0912345678" là cùng 1 số. SĐT ít hơn <see cref="TailDigitCount"/>
        /// số thì so nguyên chuỗi số. Null/trống không bao giờ khớp (trả false)
        /// để không xác thực nhầm khi thiếu SĐT.
        /// </summary>
        /// <param name="firstPhone">SĐT thứ nhất, có thể null.</param>
        /// <param name="secondPhone">SĐT thứ hai, có thể null.</param>
        /// <returns>True khi 2 SĐT được coi là cùng 1 số.</returns>
        public static bool PhonesMatch(string firstPhone, string secondPhone)
        {
            if (string.IsNullOrWhiteSpace(firstPhone) || string.IsNullOrWhiteSpace(secondPhone))
            {
                return false;
            }

            var firstDigits = DigitsOnly(firstPhone);
            var secondDigits = DigitsOnly(secondPhone);

            if (firstDigits.Length < TailDigitCount || secondDigits.Length < TailDigitCount)
            {
                return string.Equals(firstDigits, secondDigits, StringComparison.Ordinal);
            }

            // codequality-team 2026-10-20 run-0245 Agent3-BestPractices: range
            // operator thay Substring(digitsN.Length - TailDigitCount) — guard Length ở trên.
            return string.Equals(
                firstDigits[^TailDigitCount..],
                secondDigits[^TailDigitCount..],
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Che SĐT để hiển thị công khai: "0901234567" → "090***67".
        /// SĐT null/trống hoặc ít hơn 5 chữ số thì trả "Khách hàng".
        /// </summary>
        /// <param name="phone">SĐT cần che, có thể null.</param>
        /// <returns>SĐT đã che hoặc "Khách hàng".</returns>
        public static string MaskForPublicDisplay(string? phone)
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
            // codequality-team 2026-10-20 run-0245 Agent3-BestPractices: range
            // operator thay Substring(digits.Length - TailDigitCount) — guard Length ở trên.
            return digits.Length > TailDigitCount
                ? digits[^TailDigitCount..]
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
