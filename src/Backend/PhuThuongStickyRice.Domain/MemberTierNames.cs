// [codequality-smell 2026-10-20] Tên các hạng thành viên loyalty dùng chung
// (tiếng Việt, đúng chính tả có dấu). Trước đây chuỗi "Vàng"/"Bạc"/"Đồng"
// bị hardcode rải rác ở LoyaltyAppService.GetTier và VoucherCampaignAppService
// (so sánh tier, giá trị mặc định) — gõ sai 1 ký tự là lệch hạng mà
// compiler không bắt được. Gom về một chỗ, đổi tên hạng chỉ sửa 1 file.
namespace PhuThuongStickyRice.Domain;

/// <summary>
/// Tên các hạng thành viên loyalty (khớp chuỗi hiển thị cho khách và tham
/// số <c>tier</c> mà frontend truyền cho API voucher theo hạng).
/// </summary>
public static class MemberTierNames
{
    /// <summary>Hạng Đồng — mặc định, chưa đạt mốc chi tiêu nào.</summary>
    public const string Bronze = "Đồng";

    /// <summary>Hạng Bạc — đạt mốc chi tiêu TierSilverMin.</summary>
    public const string Silver = "Bạc";

    /// <summary>Hạng Vàng — vượt mốc chi tiêu TierGoldMin.</summary>
    public const string Gold = "Vàng";
}
