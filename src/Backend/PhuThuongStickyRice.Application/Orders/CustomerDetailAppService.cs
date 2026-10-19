using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface ICustomerDetailAppService : IApplicationService
    {
        /// <summary>
        /// Item 168: purchase-history detail of one customer by phone —
        /// totals, first/last order, top 3 products, 10 most recent orders
        /// and CRM tags. Read-only.
        /// </summary>
        Task<CustomerDetailDto> GetDetailAsync(string phone);
    }

    /// <summary>
    /// Item 168: customer detail. Aggregates orders by exact phone match;
    /// cancelled orders are counted separately and excluded from revenue.
    /// </summary>
    [Authorize(Roles = "Admin,Cashier")]
    public class CustomerDetailAppService : ApplicationService, ICustomerDetailAppService
    {
        private readonly IRepository<Order, Guid> _orders;
        private readonly IRepository<OrderLine, Guid> _orderLines;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly ICustomerTagAppService _customerTags;

        public CustomerDetailAppService(
            IRepository<Order, Guid> orders,
            IRepository<OrderLine, Guid> orderLines,
            IAsyncQueryableExecuter asyncExecuter,
            ICustomerTagAppService customerTags)
        {
            _orders = orders;
            _orderLines = orderLines;
            _asyncExecuter = asyncExecuter;
            _customerTags = customerTags;
        }

        public virtual async Task<CustomerDetailDto> GetDetailAsync(string phone)
        {
            var normalized = (phone ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                throw new ArgumentException("Thiếu số điện thoại.");
            }

            var queryable = await _orders.GetQueryableAsync();
            var byPhone = queryable.Where(x => x.CustomerPhone == normalized);

            // perf-hunt 2026-10-19: mọi thống kê tính trong SQL (GROUP BY),
            // không load toàn bộ đơn của khách vào memory. Trước đây
            // TopProducts luôn rỗng vì Lines không được Include (không có
            // lazy loading) — giờ truy vấn trực tiếp bảng OrderLines.
            var stats = await _asyncExecuter.FirstOrDefaultAsync(
                byPhone
                    .GroupBy(x => x.CustomerPhone)
                    .Select(g => new
                    {
                        TotalCount = g.LongCount(),
                        BillableCount = g.LongCount(x => x.Status != OrderStatus.Cancelled),
                        TotalSpent = g
                            .Where(x => x.Status != OrderStatus.Cancelled)
                            .Sum(x => x.TotalAmount.Amount),
                        FirstOrderAt = g.Min(x => x.CreatedAt),
                        LastOrderAt = g.Max(x => x.CreatedAt)
                    }));

            var customerName = await _asyncExecuter.FirstOrDefaultAsync(
                byPhone
                    .Where(x => x.CustomerName != null && x.CustomerName.Trim() != "")
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => x.CustomerName));

            var recentRows = await _asyncExecuter.ToListAsync(
                byPhone
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(10)
                    .Select(x => new
                    {
                        x.OrderNumber,
                        x.CreatedAt,
                        Total = x.TotalAmount.Amount,
                        x.Status,
                        x.PaymentMethod
                    }));

            var dto = new CustomerDetailDto
            {
                CustomerPhone = normalized,
                CustomerName = customerName,
                TotalOrders = stats?.BillableCount ?? 0,
                CancelledOrders = (stats?.TotalCount ?? 0) - (stats?.BillableCount ?? 0),
                TotalSpent = stats?.TotalSpent ?? 0m,
                FirstOrderAt = stats?.FirstOrderAt,
                LastOrderAt = stats?.LastOrderAt,
                RecentOrders = recentRows.Select(x => new CustomerRecentOrderDto
                {
                    OrderNumber = x.OrderNumber,
                    CreatedAt = x.CreatedAt,
                    TotalAmount = x.Total,
                    Status = x.Status.ToString(),
                    StatusLabel = StatusLabel(x.Status),
                    PaymentMethodLabel = PaymentLabel(x.PaymentMethod)
                }).ToList()
            };

            var linesQueryable = await _orderLines.GetQueryableAsync();
            var billableIds = byPhone
                .Where(x => x.Status != OrderStatus.Cancelled)
                .Select(x => x.Id);
            dto.TopProducts = await _asyncExecuter.ToListAsync(
                linesQueryable
                    .Where(l => billableIds.Contains(l.OrderId))
                    .GroupBy(l => l.ProductName)
                    .Select(g => new CustomerTopProductDto
                    {
                        ProductName = g.Key,
                        Quantity = g.Sum(l => (long)l.Quantity)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(3));

            var tagsByPhone = await _customerTags.GetTagsByPhonesAsync(
                new[] { normalized });
            if (tagsByPhone.TryGetValue(normalized, out var tags))
            {
                dto.Tags = tags;
            }

            return dto;
        }

        private static string StatusLabel(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "Chờ xác nhận",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Preparing => "Đang chuẩn bị",
            OrderStatus.Delivering => "Đang giao",
            OrderStatus.Completed => "Hoàn thành",
            OrderStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };

        private static string PaymentLabel(PaymentMethod method) => method switch
        {
            PaymentMethod.Cod => "COD",
            PaymentMethod.BankTransfer => "Chuyển khoản",
            PaymentMethod.MoMo => "MoMo",
            PaymentMethod.Wallet => "Ví",
            _ => method.ToString()
        };
    }
}
