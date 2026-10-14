using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// ABP repository for <see cref="Product"/>.
    /// </summary>
    public interface IProductRepository : Volo.Abp.Domain.Repositories.IRepository<Product, Guid>
    {
        Task<Product> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default);

        Task<bool> ExistsWithCodeAsync(ProductCode code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    }
}
