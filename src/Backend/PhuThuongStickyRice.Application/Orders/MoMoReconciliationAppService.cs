
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Orders;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IMoMoReconciliationAppService : IApplicationService
    {
        /// <summary>Admin: MoMo-paid orders, optionally filtered by matched state.</summary>
        Task<List<MoMoOrderDto>> GetMoMoOrdersAsync(bool? matched);

        /// <summary>Admin: manually flags an order as reconciled.</summary>
        Task MarkMatchedAsync(Guid orderId);
    }

    /// <summary>
    /// Item 128: MoMo reconciliation. Callbacks auto-mark
    /// <c>Order.MoMoMatched</c> when the OrderInfo contains the order number;
    /// anything unmatched is reviewed here manually. Note: full automatic
    /// matching (amounts, settlement reports) requires the MoMo business API —
    /// this covers the payment-link callback we receive today.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class MoMoReconciliationAppService : ApplicationService, IMoMoReconciliationAppService
    {
        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Khởi tạo service đối soát MoMo.</summary>
        public MoMoReconciliationAppService(
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Admin: liệt kê các đơn đã thanh toán qua MoMo (mới nhất trước),
        /// lọc theo trạng thái đối soát. Callback MoMo tự đánh dấu
        /// <c>Order.MoMoMatched</c> khi OrderInfo chứa mã đơn; các đơn chưa khớp
        /// tự động thì kế toán rà soát tay ở đây.
        /// </summary>
        /// <param name="matched">
        /// true: chỉ đơn đã khớp; false: chỉ đơn chưa khớp; null: tất cả.
        /// </param>
        /// <returns>Tối đa 200 đơn (đủ cho màn hình đối soát; không lấy hết lịch sử).</returns>
        public virtual async Task<List<MoMoOrderDto>> GetMoMoOrdersAsync(bool? matched)
        {
            var queryable = await _orders.GetQueryableAsync();
            IQueryable<Order> query = queryable
                .Where(x => x.PaymentMethod == PaymentMethod.MoMo
                    && x.PaymentStatus == PaymentStatus.Paid)
                .OrderByDescending(x => x.CreatedAt);

            if (matched.HasValue)
            {
                query = query.Where(x => x.MoMoMatched == matched.Value);
            }

            var list = await _asyncExecuter.ToListAsync(query.Take(200));
            return list.Select(x => new MoMoOrderDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                CustomerName = x.CustomerName,
                CustomerPhone = x.CustomerPhone,
                TotalAmount = x.TotalAmount.Amount,
                PaymentStatus = x.PaymentStatus.ToString(),
                MoMoMatched = x.MoMoMatched,
                CreatedAt = x.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Admin: đánh dấu tay 1 đơn MoMo là đã đối soát xong (khi đã kiểm tra
        /// ngoài hệ thống và thấy tiền đã về). Chỉ đổi cờ MoMoMatched, không
        /// đụng tới trạng thái đơn hay trạng thái thanh toán.
        /// </summary>
        /// <param name="orderId">Id đơn hàng MoMo cần đánh dấu.</param>
        public virtual async Task MarkMatchedAsync(Guid orderId)
        {
            var order = await _orders.GetAsync(orderId);
            order.MarkMoMoMatched();
            await _orders.UpdateAsync(order, autoSave: true);
        }
    }
}
