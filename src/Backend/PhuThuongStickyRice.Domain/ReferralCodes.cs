// [codequality-smell 2026-10-20] Định dạng mã giới thiệu dùng chung.
// Trước đây tiền tố "PT-" và độ dài 6 ký tự bị hardcode ở 3 nơi
// (LoyaltyAppService.GenerateReferralCodeAsync ×2, CheckInAppService ×1) —
// đổi định dạng mã phải sửa đủ 3 chỗ, sót 1 chỗ là sinh mã không đồng nhất.
namespace PhuThuongStickyRice.Domain;

/// <summary>
/// Định dạng mã giới thiệu thành viên: <c>PT-XXXXXX</c> (6 ký tự).
/// </summary>
public static class ReferralCodes
{
    /// <summary>Tiền tố mọi mã giới thiệu.</summary>
    public const string Prefix = "PT-";

    /// <summary>Số ký tự ngẫu nhiên sau tiền tố.</summary>
    public const int SuffixLength = 6;
}
