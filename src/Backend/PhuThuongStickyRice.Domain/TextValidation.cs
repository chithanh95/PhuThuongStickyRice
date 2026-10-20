#nullable enable annotations // CS8632: annotation string? ở dưới chỉ hợp lệ trong nullable annotations context
using PhuThuongStickyRice.Domain.Exceptions;

// [codequality-smell 2026-10-20] Guard kiểm tra độ dài text dùng chung.
// Trước đây Order.ValidateCreateInput và OrderLine.Create mỗi nơi tự viết
// lại cùng mẫu if/throw (~10 khối lặp, thông điệp "X must not exceed N
// characters." copy-paste) — gom về một chỗ để thông điệp lỗi nhất quán
// và không còn khối validation trùng lặp.
namespace PhuThuongStickyRice.Domain;

/// <summary>
/// Guard kiểm tra text rỗng/độ dài tối đa dùng chung cho validation ở
/// domain. Thông điệp lỗi giữ nguyên mẫu cũ để không đổi behavior của API.
/// </summary>
public static class TextValidation
{
    /// <summary>
    /// Text bắt buộc: rỗng/whitespace thì ném lỗi bắt buộc, ngược lại không
    /// được vượt quá độ dài tối đa (so theo text đã trim).
    /// </summary>
    /// <param name="value">Giá trị cần kiểm tra.</param>
    /// <param name="maxLength">Độ dài tối đa cho phép.</param>
    /// <param name="fieldLabel">Tên trường trong thông điệp lỗi (tiếng Anh, khớp API cũ).</param>
    /// <param name="requiredMessage">Thông điệp khi text rỗng.</param>
    public static void RequireMaxLength(string? value, int maxLength, string fieldLabel, string requiredMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(requiredMessage);
        }

        if (value.Trim().Length > maxLength)
        {
            throw new DomainException($"{fieldLabel} must not exceed {maxLength} characters.");
        }
    }

    /// <summary>
    /// Chuẩn hoá text tùy chọn: trim khoảng trắng hai đầu, rỗng/whitespace
    /// thì trả về null. Dùng chung để thay mẫu lặp
    /// <c>string.IsNullOrWhiteSpace(x) ? null : x.Trim()</c> rải khắp domain.
    /// </summary>
    /// <param name="value">Giá trị cần chuẩn hoá; null thì trả về null.</param>
    /// <returns>Text đã trim, hoặc null khi rỗng/whitespace.</returns>
    public static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Text tùy chọn: không vượt quá độ dài tối đa.
    /// </summary>
    /// <param name="value">Giá trị cần kiểm tra; null (và whitespace khi trim) thì bỏ qua.</param>
    /// <param name="maxLength">Độ dài tối đa cho phép.</param>
    /// <param name="fieldLabel">Tên trường trong thông điệp lỗi (tiếng Anh, khớp API cũ).</param>
    /// <param name="trimBeforeCheck">
    /// True: trim rồi mới đo độ dài (khớp các chỗ cũ dùng <c>Trim().Length</c>);
    /// false: đo độ dài thô (khớp các chỗ cũ dùng <c>Length</c>).
    /// </param>
    public static void MaxLength(string? value, int maxLength, string fieldLabel, bool trimBeforeCheck = true)
    {
        if (trimBeforeCheck)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (value.Trim().Length > maxLength)
            {
                throw new DomainException($"{fieldLabel} must not exceed {maxLength} characters.");
            }
        }
        else
        {
            if (value == null)
            {
                return;
            }

            if (value.Length > maxLength)
            {
                throw new DomainException($"{fieldLabel} must not exceed {maxLength} characters.");
            }
        }
    }
}
