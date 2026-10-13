using Microsoft.EntityFrameworkCore;
using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class ProductRepository : Repository<Product, Guid>, IProductRepository
    {
        public ProductRepository(StickyRiceDbContext dbContext, IDateTimeProvider dateTimeProvider)
            : base(dbContext, dateTimeProvider)
        {
        }

        public Task<Product> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default)
        {
            return SingleOrDefaultAsync(DbSet.Where(x => x.Code.Value == code.Value));
        }

        public async Task<bool> ExistsWithCodeAsync(ProductCode code, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = DbSet.Where(x => x.Code.Value == code.Value);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
    }
}
