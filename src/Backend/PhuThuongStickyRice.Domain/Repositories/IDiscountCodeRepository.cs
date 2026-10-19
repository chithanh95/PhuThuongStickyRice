using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// ABP repository cho <see cref="DiscountCode"/>.
    /// </summary>
    public interface IDiscountCodeRepository : Volo.Abp.Domain.Repositories.IRepository<DiscountCode, Guid>
    {
        /// <summary>
        /// Tăng <see cref="DiscountCode.UsedCount"/> lên 1 một cách nguyên tử,
        /// chỉ khi mã còn lượt dùng (MaxUses == null hoặc UsedCount &lt;
        /// MaxUses): UPDATE ... WHERE Code = ... AND (MaxUses IS NULL OR
        /// UsedCount &lt; MaxUses). Trả về true nếu tăng thành công, false khi
        /// mã đã hết lượt tại thời điểm UPDATE (2 checkout đồng thời cùng 1
        /// mã dùng 1 lần không thể cùng đốt lượt — chống race check-then-set).
        /// </summary>
        /// <param name="normalizedCode">Mã đã chuẩn hóa (in hoa, trim).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryIncrementUsageAsync(
            string normalizedCode, CancellationToken cancellationToken = default);
    }
}
