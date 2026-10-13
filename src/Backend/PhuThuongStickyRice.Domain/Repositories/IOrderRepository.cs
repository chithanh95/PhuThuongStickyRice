using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    public interface IOrderRepository : IRepository<Order, Guid>
    {
        Task<Order> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

        Task<bool> ExistsWithOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    }
}
