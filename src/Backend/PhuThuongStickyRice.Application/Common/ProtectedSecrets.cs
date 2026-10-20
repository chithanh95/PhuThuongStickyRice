#nullable enable annotations
using System;
using Microsoft.AspNetCore.DataProtection;

namespace PhuThuongStickyRice.Application.Common;

/// <summary>
/// Mã hóa/giải mã các secret tích hợp (token Zalo, webhook secret...) lưu ở
/// ShopSetting bằng ASP.NET Core DataProtection.
/// [codequality-smell 2026-10-20] Gom về một chỗ: trước đây
/// ShopSettingAppService (ProtectSecret/UnprotectSecret) và
/// WeeklyReportSender (Unprotect) mỗi nơi copy nguyên khối logic — tiền tố
/// "DPv1:", fallback plaintext cho dữ liệu cũ, try/catch không ném khi key
/// ring lỗi. Đổi định dạng mã hóa giờ chỉ sửa 1 file.
/// </summary>
public static class ProtectedSecrets
{
    /// <summary>
    /// Tiền tố đánh dấu giá trị đã mã hóa. Dữ liệu cũ lưu plaintext (không
    /// có tiền tố) được đọc nguyên khi giải mã để tương thích ngược.
    /// </summary>
    public const string EncryptedPrefix = "DPv1:";

    /// <summary>
    /// Purpose string của DataProtection — mọi nơi mã hóa/giải mã secret
    /// shop phải dùng chung chuỗi này, đổi chuỗi là dữ liệu cũ không giải
    /// được nữa.
    /// </summary>
    public const string Purpose = "PhuThuongStickyRice.ShopSettings";

    /// <summary>
    /// Mã hóa secret trước khi lưu DB: gắn tiền tố <see cref="EncryptedPrefix"/>.
    /// </summary>
    /// <param name="protector">DataProtector đã tạo với <see cref="Purpose"/>.</param>
    /// <param name="value">Secret plaintext; null/rỗng thì trả về null (không mã hóa).</param>
    /// <returns>Chuỗi đã mã hóa có tiền tố, hoặc null khi <paramref name="value"/> rỗng.</returns>
    public static string? Protect(IDataProtector protector, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return EncryptedPrefix + protector.Protect(value.Trim());
    }

    /// <summary>
    /// Giải mã secret đã lưu: giá trị không có tiền tố là plaintext cũ, trả
    /// về nguyên; giải mã lỗi (key ring không có) thì gọi
    /// <paramref name="onDecryptFailed"/> rồi trả về null — không bao giờ ném.
    /// </summary>
    /// <param name="protector">DataProtector đã tạo với <see cref="Purpose"/>.</param>
    /// <param name="stored">Chuỗi đã lưu (mã hóa hoặc plaintext cũ).</param>
    /// <param name="onDecryptFailed">Callback ghi log khi giải mã thất bại.</param>
    /// <returns>Secret plaintext, hoặc null khi rỗng/không giải được.</returns>
    public static string? Unprotect(IDataProtector protector, string? stored, Action<Exception>? onDecryptFailed = null)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        if (!stored.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return stored;
        }

        try
        {
            return protector.Unprotect(stored[EncryptedPrefix.Length..]);
        }
        catch (Exception ex)
        {
            onDecryptFailed?.Invoke(ex);
            return null;
        }
    }
}
