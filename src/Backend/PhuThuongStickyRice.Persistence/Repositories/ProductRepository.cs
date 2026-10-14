using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class ProductRepository : EfCoreRepository<StickyRiceDbContext, Product, System.Guid>, IProductRepository
    {
        public ProductRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // ABP repositories resolve CurrentTenant via LazyServiceProvider (property
            // injection), which only works with Autofac. This app uses the default
            // MS DI container, so set it explicitly via constructor injection.
            LazyServiceProvider = lazyServiceProvider;
        }

        public async Task<Product> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code.Value == code.Value), cancellationToken);
        }

        public async Task<bool> ExistsWithCodeAsync(ProductCode code, System.Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            var query = queryable.Where(x => x.Code.Value == code.Value);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await AsyncExecuter.AnyAsync(query, cancellationToken);
        }
    }
}
