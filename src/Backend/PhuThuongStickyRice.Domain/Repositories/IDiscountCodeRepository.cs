using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Giảm <see cref="DiscountCode.UsedCount"/> đi 1 một cách nguyên tử,
        /// không bao giờ cho về âm: UPDATE ... WHERE Code = ... AND UsedCount
        /// &gt; 0. Dùng khi đơn hàng bị hủy — hoàn lại lượt dùng mã đã đốt lúc
        /// tạo đơn. Không tự bật lại IsActive: mã có thể bị admin tắt tay sau
        /// khi tự tạm dừng ở trần MaxUses (item 252), admin xem ở trung tâm
        /// thông báo và tự quyết định bật lại.
        /// </summary>
        /// <param name="normalizedCode">Mã đã chuẩn hóa (in hoa, trim).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryDecrementUsageAsync(
            string normalizedCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Giảm <see cref="DiscountCode.UsedCount"/> theo batch, nguyên tử cho
        /// từng mã: mỗi mã 1 câu UPDATE (UsedCount = Max(0, UsedCount - số
        /// lượng)) — tương đương gọi <see cref="TryDecrementUsageAsync"/> N
        /// lần cho cùng 1 mã (kết quả cuối giống hệt, không bao giờ về âm),
        /// nhưng chỉ tốn 1 round-trip cho mỗi mã khác nhau. Dùng ở worker tự
        /// hủy đơn, nơi N đơn bị hủy có thể cùng dùng 1 mã.
        /// </summary>
        /// <param name="decrements">Mã đã chuẩn hóa → số lượt cần hoàn (&gt; 0).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task TryDecrementUsageBatchAsync(
            IReadOnlyDictionary<string, int> decrements,
            CancellationToken cancellationToken = default);
    }
}
