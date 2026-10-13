using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    public interface IProductRepository : IRepository<Product, Guid>
    {
        Task<Product> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default);

        Task<bool> ExistsWithCodeAsync(ProductCode code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    }
}
