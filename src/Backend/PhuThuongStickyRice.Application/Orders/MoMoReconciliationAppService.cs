
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

        public MoMoReconciliationAppService(
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

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

        public virtual async Task MarkMatchedAsync(Guid orderId)
        {
            var order = await _orders.GetAsync(orderId);
            order.MarkMoMoMatched();
            await _orders.UpdateAsync(order, autoSave: true);
        }
    }
}
