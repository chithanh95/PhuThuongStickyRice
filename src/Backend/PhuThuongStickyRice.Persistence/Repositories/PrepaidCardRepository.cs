using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class PrepaidCardRepository
        : EfCoreRepository<StickyRiceDbContext, PrepaidCard, System.Guid>, IPrepaidCardRepository
    {
        public PrepaidCardRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        /// <summary>
        /// Trừ 1 suất nguyên tử bằng ExecuteUpdateAsync (1 câu UPDATE ...
        /// SET UsedSuits = UsedSuits + 1 WHERE CardCode = cardCode AND Status
        /// = Active AND UsedSuits &lt; TotalSuits). Điều kiện UsedSuits &lt;
        /// TotalSuits nằm ngay trong WHERE nên 2 UPDATE đồng thời được DB
        /// serialize: bên thua thấy UsedSuits đã chạm trần → affected = 0 →
        /// không bao giờ trừ lố. Chạy trong DbContext/UoW hiện tại nên tham
        /// gia cùng transaction với phần còn lại của nghiệp vụ.
        /// </summary>
        public async Task<bool> TryRedeemAsync(string cardCode, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<PrepaidCard>()
                .Where(c => c.CardCode == cardCode
                    && c.Status == PrepaidCardStatus.Active
                    && c.UsedSuits < c.TotalSuits)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.UsedSuits, c => c.UsedSuits + 1),
                    cancellationToken);
            return affected > 0;
        }
    }
}
