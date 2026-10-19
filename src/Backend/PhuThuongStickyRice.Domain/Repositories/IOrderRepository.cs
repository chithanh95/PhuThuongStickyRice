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
        /// Nạp các đơn theo Id ở chế độ read-only (không track entity, không
        /// nạp Lines) — dùng cho các endpoint public chỉ map sang DTO như
        /// tra cứu đơn theo SĐT, tránh tốn memory/CPU snapshot change-tracker.
        /// </summary>
        Task<List<Order>> GetOrdersByIdsNoTrackingAsync(IEnumerable<Guid> orderIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đánh dấu lượt quay may mắn của đơn đã dùng một cách nguyên tử:
        /// UPDATE ... WHERE Id = orderId AND LuckySpinUsed = false. Trả về
        /// true nếu đánh dấu thành công, false nếu lượt quay đã được dùng
        /// (2 request quay đồng thời cùng đơn chỉ 1 qua được — chống nhận
        /// 2 voucher thật từ 1 lượt quay).
        /// </summary>
        Task<bool> TryMarkSpinUsedAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Chuyển trạng thái đơn một cách nguyên tử: UPDATE ... SET Status
        /// = to WHERE Id = orderId AND Status = expectedFrom. Trả về true
        /// nếu chuyển thành công, false nếu đơn đã đổi trạng thái bởi request
        /// khác (2 staff bấm "Hoàn thành"/"Hủy" đồng thời cùng đơn chỉ 1 qua
        /// được — chống chạy side-effect 2 lần: cộng/hoàn điểm loyalty, hoàn
        /// tiền ví, thưởng giới thiệu).
        /// </summary>
        Task<bool> TryTransitionStatusAsync(
            Guid orderId, OrderStatus expectedFrom, OrderStatus to,
            CancellationToken cancellationToken = default);
    }
}
