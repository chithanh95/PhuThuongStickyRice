using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class OrderRepository : EfCoreRepository<StickyRiceDbContext, Order, System.Guid>, IOrderRepository
    {
        public OrderRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        public async Task<Order> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.OrderNumber == orderNumber), cancellationToken);
        }

        public async Task<bool> ExistsWithOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.AnyAsync(
                queryable.Where(x => x.OrderNumber == orderNumber), cancellationToken);
        }
    }
}
