using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class DiscountCodeRepository
        : EfCoreRepository<StickyRiceDbContext, DiscountCode, System.Guid>, IDiscountCodeRepository
    {
        public DiscountCodeRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        /// <summary>
        /// Tăng UsedCount nguyên tử bằng ExecuteUpdateAsync (1 câu UPDATE ...
        /// WHERE Code = ... AND (MaxUses IS NULL OR UsedCount &lt; MaxUses)).
        /// Item 252: cùng câu UPDATE tự tắt mã (IsActive = false) khi lượt
        /// tăng này chạm trần MaxUses — chống "vỡ ngân sách KM" khi mã bị
        /// share lan, admin xem mã tự tạm dừng ở trung tâm thông báo.
        /// Chạy trong DbContext/UoW hiện tại nên tham gia cùng transaction với
        /// phần còn lại của nghiệp vụ (đơn hàng).
        /// </summary>
        public async Task<bool> TryIncrementUsageAsync(
            string normalizedCode, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<DiscountCode>()
                .Where(x => x.Code == normalizedCode
                    && (!x.MaxUses.HasValue || x.UsedCount < x.MaxUses.Value))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.UsedCount, x => x.UsedCount + 1)
                    .SetProperty(x => x.IsActive,
                        x => x.MaxUses.HasValue && x.UsedCount + 1 >= x.MaxUses.Value
                            ? false
                            : x.IsActive),
                    cancellationToken);
            return affected > 0;
        }

        /// <summary>
        /// Giảm UsedCount đi 1 nguyên tử bằng ExecuteUpdateAsync (1 câu
        /// UPDATE ... SET UsedCount = UsedCount - 1 WHERE Code = ... AND
        /// UsedCount &gt; 0) — không bao giờ về âm. Chạy trong DbContext/UoW
        /// hiện tại nên tham gia cùng transaction với nghiệp vụ hủy đơn.
        /// Không tự bật lại IsActive (xem interface).
        /// </summary>
        public async Task<bool> TryDecrementUsageAsync(
            string normalizedCode, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<DiscountCode>()
                .Where(x => x.Code == normalizedCode && x.UsedCount > 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.UsedCount, x => x.UsedCount - 1),
                    cancellationToken);
            return affected > 0;
        }

        /// <summary>
        /// Giảm UsedCount theo batch bằng ExecuteUpdateAsync: 1 câu UPDATE
        /// cho mỗi mã (UsedCount = Max(0, UsedCount - số lượng)) — kết quả
        /// cuối giống hệt N lần gọi TryDecrementUsageAsync cho cùng 1 mã
        /// (mỗi lần chỉ giảm khi UsedCount &gt; 0, không bao giờ về âm), nhưng
        /// chỉ tốn 1 round-trip cho mỗi mã khác nhau. Math.Max dịch thành
        /// CASE WHEN trên SQL Server. Chạy trong DbContext/UoW hiện tại nên
        /// tham gia cùng transaction với nghiệp vụ hủy đơn.
        /// </summary>
        public async Task TryDecrementUsageBatchAsync(
            IReadOnlyDictionary<string, int> decrements,
            CancellationToken cancellationToken = default)
        {
            if (decrements == null || decrements.Count == 0)
            {
                return;
            }

            var dbContext = await GetDbContextAsync();
            foreach (var (code, count) in decrements)
            {
                if (count <= 0)
                {
                    continue;
                }

                await dbContext.Set<DiscountCode>()
                    .Where(x => x.Code == code)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.UsedCount, x => Math.Max(0, x.UsedCount - count)),
                        cancellationToken);
            }
        }
    }
}
