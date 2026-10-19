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
    public class SubscriptionRepository
        : EfCoreRepository<StickyRiceDbContext, Subscription, System.Guid>, ISubscriptionRepository
    {
        public SubscriptionRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        /// <summary>
        /// Giành quyền sinh đơn định kỳ nguyên tử bằng ExecuteUpdateAsync
        /// (1 câu UPDATE LastOrderDate = today WHERE Id + Active +
        /// (LastOrderDate IS NULL OR LastOrderDate &lt; today)). Điều kiện
        /// "đến hạn" nằm ngay trong WHERE nên 2 UPDATE đồng thời được DB
        /// serialize: chỉ 1 bên thấy affected = 1, bên còn lại affected = 0
        /// và bỏ qua gói đó. Chạy trong DbContext/UoW hiện tại nên tham gia
        /// cùng transaction — nếu InsertAsync đơn phía dưới lỗi, claim cũng
        /// rollback và lần chạy sau làm lại, không mất ngày của khách.
        /// </summary>
        public async Task<bool> TryClaimDueAsync(
            Guid id, DateTime today, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<Subscription>()
                .Where(x => x.Id == id
                    && x.Active
                    && x.StartDate <= today
                    && (x.LastOrderDate == null || x.LastOrderDate < today))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LastOrderDate, today),
                    cancellationToken);
            return affected > 0;
        }
    }
}
