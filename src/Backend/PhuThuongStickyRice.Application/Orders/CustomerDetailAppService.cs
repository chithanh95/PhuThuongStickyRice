using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Hợp đồng chi tiết khách hàng (item 168, admin/thu ngân): lịch sử mua
    /// hàng theo SĐT — tổng chi tiêu, đơn đầu/cuối, top 3 món, 10 đơn gần
    /// nhất và tag CRM. Chỉ đọc, đơn hủy tách riêng không tính vào doanh thu.
    /// </summary>
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
    [Authorize(Roles = RoleGroups.AdminCashier)]
    public class CustomerDetailAppService : ApplicationService, ICustomerDetailAppService
    {
        private readonly IRepository<Order, Guid> _orders;
        private readonly IRepository<OrderLine, Guid> _orderLines;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly ICustomerTagAppService _customerTags;

        /// <summary>Số đơn gần nhất hiển thị ở trang chi tiết khách hàng.</summary>
        private const int RecentOrdersLimit = 10;

        /// <summary>Số sản phẩm top hiển thị ở trang chi tiết khách hàng.</summary>
        private const int TopProductsLimit = 3;

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

        /// <summary>
        /// Chi tiết một khách hàng theo SĐT: thống kê đơn/chi tiêu, 10 đơn gần
        /// nhất, 3 món mua nhiều nhất và nhãn khách hàng.
        /// Thống kê tổng hợp tính trong SQL (GROUP BY), không nạp toàn bộ đơn
        /// của khách vào bộ nhớ.
        /// </summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <returns>DTO chi tiết khách hàng.</returns>
        /// <exception cref="ArgumentException">Ném khi thiếu SĐT.</exception>
        public virtual async Task<CustomerDetailDto> GetDetailAsync(string phone)
        {
            var normalized = (phone ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                throw new ArgumentException("Thiếu số điện thoại.");
            }

            var queryable = await _orders.GetQueryableAsync();
            var byPhone = queryable.Where(x => x.CustomerPhone == normalized);

            var stats = await LoadStatsAsync(byPhone);
            var customerName = await LoadCustomerNameAsync(byPhone);
            var recentOrders = await LoadRecentOrdersAsync(byPhone);
            var topProducts = await LoadTopProductsAsync(byPhone);
            var tags = await LoadTagsAsync(normalized);

            return new CustomerDetailDto
            {
                CustomerPhone = normalized,
                CustomerName = customerName,
                TotalOrders = stats?.BillableCount ?? 0,
                // Số đơn hủy không lưu riêng — suy ra bằng tổng đơn trừ đơn tính tiền
                // (BillableCount đã loại đơn hủy ở LoadStatsAsync).
                CancelledOrders = (stats?.TotalCount ?? 0) - (stats?.BillableCount ?? 0),
                TotalSpent = stats?.TotalSpent ?? 0m,
                FirstOrderAt = stats?.FirstOrderAt,
                LastOrderAt = stats?.LastOrderAt,
                RecentOrders = recentOrders,
                TopProducts = topProducts,
                Tags = tags
            };
        }

        /// <summary>
        /// Thống kê tổng hợp của khách: tổng đơn, đơn tính tiền (loại đơn hủy),
        /// tổng chi tiêu, đơn đầu/cuối. Mọi thống kê tính trong SQL (GROUP BY),
        /// không load toàn bộ đơn của khách vào memory.
        /// </summary>
        private sealed class CustomerOrderStats
        {
            public long TotalCount { get; set; }

            public long BillableCount { get; set; }

            public decimal TotalSpent { get; set; }

            public DateTime? FirstOrderAt { get; set; }

            public DateTime? LastOrderAt { get; set; }
        }

        private async Task<CustomerOrderStats> LoadStatsAsync(IQueryable<Order> byPhone)
        {
            return await _asyncExecuter.FirstOrDefaultAsync(
                byPhone
                    .GroupBy(x => x.CustomerPhone)
                    .Select(g => new CustomerOrderStats
                    {
                        TotalCount = g.LongCount(),
                        BillableCount = g.LongCount(x => x.Status != OrderStatus.Cancelled),
                        TotalSpent = g
                            .Where(x => x.Status != OrderStatus.Cancelled)
                            .Sum(x => x.TotalAmount.Amount),
                        FirstOrderAt = g.Min(x => x.CreatedAt),
                        LastOrderAt = g.Max(x => x.CreatedAt)
                    }));
        }

        /// <summary>Tên khách: lấy tên mới nhất từng nhập (theo đơn mới nhất).</summary>
        private async Task<string> LoadCustomerNameAsync(IQueryable<Order> byPhone)
        {
            return await _asyncExecuter.FirstOrDefaultAsync(
                byPhone
                    .Where(x => x.CustomerName != null && x.CustomerName.Trim() != "")
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => x.CustomerName));
        }

        /// <summary>N đơn gần nhất của khách, kèm nhãn trạng thái/thanh toán.</summary>
        private async Task<List<CustomerRecentOrderDto>> LoadRecentOrdersAsync(IQueryable<Order> byPhone)
        {
            var recentRows = await _asyncExecuter.ToListAsync(
                byPhone
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(RecentOrdersLimit)
                    .Select(x => new
                    {
                        x.OrderNumber,
                        x.CreatedAt,
                        Total = x.TotalAmount.Amount,
                        x.Status,
                        x.PaymentMethod
                    }));

            return recentRows.Select(x => new CustomerRecentOrderDto
            {
                OrderNumber = x.OrderNumber,
                CreatedAt = x.CreatedAt,
                TotalAmount = x.Total,
                Status = x.Status.ToString(),
                StatusLabel = StatusLabel(x.Status),
                PaymentMethodLabel = PaymentLabel(x.PaymentMethod)
            }).ToList();
        }

        /// <summary>
        /// Top sản phẩm khách mua nhiều nhất. Truy vấn trực tiếp bảng OrderLines
        /// (trước đây TopProducts luôn rỗng vì Lines không được Include —
        /// không có lazy loading).
        /// </summary>
        private async Task<List<CustomerTopProductDto>> LoadTopProductsAsync(IQueryable<Order> byPhone)
        {
            var linesQueryable = await _orderLines.GetQueryableAsync();
            var billableIds = byPhone
                .Where(x => x.Status != OrderStatus.Cancelled)
                .Select(x => x.Id);
            return await _asyncExecuter.ToListAsync(
                linesQueryable
                    .Where(l => billableIds.Contains(l.OrderId))
                    .GroupBy(l => l.ProductName)
                    .Select(g => new CustomerTopProductDto
                    {
                        ProductName = g.Key,
                        Quantity = g.Sum(l => (long)l.Quantity)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(TopProductsLimit));
        }

        /// <summary>Tag CRM gắn với SĐT; rỗng khi khách chưa có tag.</summary>
        private async Task<List<CustomerTagDto>> LoadTagsAsync(string normalizedPhone)
        {
            var tagsByPhone = await _customerTags.GetTagsByPhonesAsync(
                new[] { normalizedPhone });
            return tagsByPhone.TryGetValue(normalizedPhone, out var tags)
                ? tags
                : new List<CustomerTagDto>();
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
