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
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly ICustomerTagAppService _customerTags;

        public CustomerDetailAppService(
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter,
            ICustomerTagAppService customerTags)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
            _customerTags = customerTags;
        }

        public async Task<CustomerDetailDto> GetDetailAsync(string phone)
        {
            var normalized = (phone ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                throw new ArgumentException("Thiếu số điện thoại.");
            }

            var queryable = await _orders.GetQueryableAsync();
            var orders = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.CustomerPhone == normalized)
                         .OrderByDescending(x => x.CreatedAt));

            var billable = orders
                .Where(x => x.Status != OrderStatus.Cancelled)
                .ToList();

            var dto = new CustomerDetailDto
            {
                CustomerPhone = normalized,
                CustomerName = orders
                    .Select(x => x.CustomerName)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                TotalOrders = billable.Count,
                CancelledOrders = orders.Count - billable.Count,
                TotalSpent = billable.Sum(x => x.TotalAmount.Amount),
                FirstOrderAt = orders.Count > 0
                    ? orders.Min(x => x.CreatedAt)
                    : (DateTime?)null,
                LastOrderAt = orders.Count > 0
                    ? orders.Max(x => x.CreatedAt)
                    : (DateTime?)null,
            };

            dto.TopProducts = billable
                .SelectMany(x => x.Lines)
                .GroupBy(l => l.ProductName)
                .Select(g => new CustomerTopProductDto
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(l => (long)l.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(3)
                .ToList();

            dto.RecentOrders = orders
                .Take(10)
                .Select(x => new CustomerRecentOrderDto
                {
                    OrderNumber = x.OrderNumber,
                    CreatedAt = x.CreatedAt,
                    TotalAmount = x.TotalAmount.Amount,
                    Status = x.Status.ToString(),
                    StatusLabel = StatusLabel(x.Status),
                    PaymentMethodLabel = PaymentLabel(x.PaymentMethod)
                })
                .ToList();

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
