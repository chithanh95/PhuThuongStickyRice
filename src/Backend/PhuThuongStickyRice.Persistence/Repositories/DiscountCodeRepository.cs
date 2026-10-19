using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
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
                    .SetProperty(x => x.UsedCount, x => x.UsedCount + 1),
                    cancellationToken);
            return affected > 0;
        }
    }
}
