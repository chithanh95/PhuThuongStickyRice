using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IQueueDisplayAppService : IApplicationService
    {
        /// <summary>
        /// Public: tickets for the in-shop waiting-number screen (/cho).
        /// Today's (Vietnam) non-cancelled orders in Confirmed/Preparing
        /// ("Đang chuẩn bị") and Delivering ("Mời lấy món"). Privacy-safe:
        /// only queue number, short code and coarse status.
        /// </summary>
        Task<List<QueueTicketDto>> GetNowServingAsync();
    }

    /// <summary>Item 178: public queue-number display for the waiting area TV.</summary>
    public class QueueDisplayAppService : ApplicationService, IQueueDisplayAppService
    {
        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public QueueDisplayAppService(
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<List<QueueTicketDto>> GetNowServingAsync()
        {
            // Vietnam day bounds (UTC+7, no DST). CreatedAt is stored as UTC.
            var vnNow = DateTime.UtcNow.AddHours(7);
            var dayStartUtc = vnNow.Date.AddHours(-7);
            var dayEndUtc = dayStartUtc.AddDays(1);

            var visibleStatuses = new[]
                { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Delivering };

            var queryable = await _orders.GetQueryableAsync();
            var today = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(x => x.CreatedAt >= dayStartUtc
                        && x.CreatedAt < dayEndUtc
                        && x.Status != OrderStatus.Cancelled)
                    .OrderBy(x => x.CreatedAt));

            var result = new List<QueueTicketDto>();
            for (var i = 0; i < today.Count; i++)
            {
                var order = today[i];
                if (!visibleStatuses.Contains(order.Status))
                {
                    continue;
                }

                var number = order.OrderNumber ?? string.Empty;
                result.Add(new QueueTicketDto
                {
                    QueueNumber = i + 1,
                    ShortCode = number.Length <= 3
                        ? number.ToUpperInvariant()
                        : number.Substring(number.Length - 3).ToUpperInvariant(),
                    Status = order.Status == OrderStatus.Delivering ? "Ready" : "Preparing"
                });
            }

            // "Mời lấy món" first — those customers need to act.
            return result
                .OrderBy(x => x.Status == "Ready" ? 0 : 1)
                .ThenBy(x => x.QueueNumber)
                .ToList();
        }
    }
}
