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
        /// tạo đơn. Nếu mã đang tắt CHỈ VÌ đã chạm trần MaxUses (tự tắt ở
        /// TryIncrementUsageAsync, item 252) và chưa hết hạn thì hoàn lượt
        /// cũng bật lại IsActive trong cùng câu UPDATE — trước đây mã dùng
        /// 1 lần (kể cả mã sinh nhật) chết vĩnh viễn sau 1 lần hủy đơn dù
        /// lượt đã được hoàn. Mã admin tắt tay khi chưa chạm trần và mã đã
        /// hết hạn không bị bật lại.
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
