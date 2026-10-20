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
    /// <summary>
    /// Repository EF Core cho <see cref="Product"/>: các truy vấn theo
    /// <see cref="ProductCode"/> (value object) mà generic repository của ABP
    /// không cung cấp sẵn.
    /// </summary>
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

        /// <summary>
        /// Tìm sản phẩm theo mã (không phân biệt hoa/thường tùy chuẩn hóa của
        /// <see cref="ProductCode"/>). Trả về null khi không tồn tại.
        /// </summary>
        /// <param name="code">Mã sản phẩm cần tìm.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        /// <returns>Sản phẩm khớp mã, hoặc null.</returns>
        public async Task<Product> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            // So theo x.Code.Value (thành phần complex type): relational
            // provider (SQL Server) translate được, provider InMemory không —
            // test chức năng phải dùng SQLite, không dùng InMemory.
            return await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code.Value == code.Value), cancellationToken);
        }

        /// <summary>
        /// Kiểm tra đã tồn tại sản phẩm dùng mã này chưa (phục vụ validate
        /// trùng mã khi thêm/sửa sản phẩm).
        /// </summary>
        /// <param name="code">Mã cần kiểm tra.</param>
        /// <param name="excludeId">Id loại trừ — khi sửa sản phẩm, bỏ qua chính nó để không báo trùng giả.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        /// <returns>True nếu có sản phẩm khác đang dùng mã.</returns>
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
