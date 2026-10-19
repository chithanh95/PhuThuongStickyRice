using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// ABP repository for <see cref="Order"/>.
    /// </summary>
    public interface IOrderRepository : Volo.Abp.Domain.Repositories.IRepository<Order, Guid>
    {
        Task<Order> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

        Task<bool> ExistsWithOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads the given orders with their <see cref="Order.Lines"/> populated
        /// in one query (Lines are not auto-included — see OrderConfiguration).
        /// </summary>
        Task<List<Order>> GetListWithLinesAsync(IEnumerable<Guid> orderIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đánh dấu lượt quay may mắn của đơn đã dùng một cách nguyên tử:
        /// UPDATE ... WHERE Id = orderId AND LuckySpinUsed = false. Trả về
        /// true nếu đánh dấu thành công, false nếu lượt quay đã được dùng
        /// (2 request quay đồng thời cùng đơn chỉ 1 qua được — chống nhận
        /// 2 voucher thật từ 1 lượt quay).
        /// </summary>
        Task<bool> TryMarkSpinUsedAsync(Guid orderId, CancellationToken cancellationToken = default);
    }
}
