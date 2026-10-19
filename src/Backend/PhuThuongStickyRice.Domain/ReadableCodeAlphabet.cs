// [codequality-smell 2026-10-19] Hằng số dùng chung cho việc sinh mã đọc được
// (mã giới thiệu, mã quà, mã voucher): bảng chữ cái cố ý bỏ I/O/0/1 để mã
// đọc qua điện thoại cho khách không bị nhầm. Trước đây mỗi service tự
// hardcode chuỗi này (DiscountAppService, LoyaltyAppService,
// CustomerProfileAppService) — gom về một chỗ để đổi một lần cho tất cả.
namespace PhuThuongStickyRice.Domain;

/// <summary>
/// Bảng chữ cái dùng khi sinh các mã đọc được (mã giới thiệu, mã quà, mã
/// voucher): chỉ gồm chữ HOA và số, cố ý loại ký tự dễ nhầm I/O/0/1.
/// </summary>
public static class ReadableCodeAlphabet
{
    /// <summary>Bảng chữ cái sinh mã, không chứa ký tự dễ nhầm I/O/0/1.</summary>
    public const string Value = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
}
