using System.Linq;
using System.Security.Cryptography;

namespace PhuThuongStickyRice.Application.Common;

/// <summary>
/// Sinh hậu tố ngẫu nhiên cho các mã đọc được (mã giảm giá, mã giới thiệu,
/// mã quà...).
/// [codequality-smell 2026-10-20] Gom về một chỗ: trước đây
/// DiscountAppService.BuildGeneratedCodeCandidate và
/// LoyaltyAppService.GenerateReferralCodeAsync mỗi nơi viết lại nguyên dòng
/// LINQ sinh chuỗi ngẫu nhiên — đổi thuật toán/RNG giờ chỉ sửa 1 file.
/// </summary>
public static class ReadableCode
{
    /// <summary>
    /// Sinh chuỗi ngẫu nhiên độ dài <paramref name="length"/> từ bảng chữ
    /// cái <see cref="Domain.ReadableCodeAlphabet"/> (đã bỏ ký tự dễ nhầm
    /// I/O/0/1 để khách đọc to không nhầm).
    /// </summary>
    /// <param name="length">Số ký tự ngẫu nhiên cần sinh.</param>
    /// <returns>Chuỗi ngẫu nhiên chỉ gồm ký tự đọc được.</returns>
    /// <remarks>
    /// CA5394: các mã này quy ra tiền/điểm nên phải dùng RNG mật mã
    /// (<see cref="RandomNumberGenerator"/>) — <c>Random.Shared</c> đoán
    /// được (deterministic PRNG), kẻ xấu có thể dò mã tiếp theo.
    /// </remarks>
    public static string GenerateSuffix(int length)
    {
        var alphabet = Domain.ReadableCodeAlphabet.Value;
        return new string(
            Enumerable.Range(0, length)
                .Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)])
                .ToArray());
    }
}
