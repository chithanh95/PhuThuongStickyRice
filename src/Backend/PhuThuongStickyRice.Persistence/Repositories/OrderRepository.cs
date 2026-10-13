using Microsoft.EntityFrameworkCore;
using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class OrderRepository : Repository<Order, System.Guid>, IOrderRepository
    {
        public OrderRepository(StickyRiceDbContext dbContext, IDateTimeProvider dateTimeProvider)
            : base(dbContext, dateTimeProvider)
        {
        }

        public Task<Order> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            return SingleOrDefaultAsync(DbSet.Where(x => x.OrderNumber == orderNumber));
        }

        public async Task<bool> ExistsWithOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            return await DbSet.AnyAsync(x => x.OrderNumber == orderNumber, cancellationToken);
        }
    }
}
