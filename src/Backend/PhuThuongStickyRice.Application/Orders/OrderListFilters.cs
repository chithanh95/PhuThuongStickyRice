using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Linq;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Bộ lọc danh sách đơn dùng chung: tìm kiếm (mã đơn/tên khách/SĐT),
    /// trạng thái, shipper được gán, khoảng ngày Việt Nam và trạng thái thu
    /// tiền mặt. Gom bản copy-paste trước đây nằm ở OrderAppService
    /// (ApplyOrderListFiltersAsync) và OrderReportsAppService (ApplyFiltersAsync)
    /// về 1 chỗ — màn danh sách và file Excel xuất ra luôn khớp nhau với cùng
    /// 1 bộ filter. Bộ lọc VIP (cần gọi service bất đồng bộ) giữ lại ở từng nơi gọi.
    /// </summary>
    internal static class OrderListFilters
    {
        /// <summary>
        /// Áp các bộ lọc đồng bộ (không cần gọi service khác) lên query đơn hàng.
        /// </summary>
        /// <param name="query">Query đơn hàng gốc.</param>
        /// <param name="input">Bộ lọc từ màn danh sách/xuất báo cáo.</param>
        /// <returns>Query đã áp filter.</returns>
        public static IQueryable<Order> Apply(IQueryable<Order> query, GetOrdersInput input)
        {
            // Tìm theo mã đơn / tên khách / SĐT.
            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.Trim();
                query = query.Where(x =>
                    x.OrderNumber.Contains(term)
                    || x.CustomerName.Contains(term)
                    || x.CustomerPhone.Contains(term));
            }

            // Trạng thái nhập sai chính tả thì bỏ qua bộ lọc, không lỗi trang.
            if (!string.IsNullOrWhiteSpace(input.Status)
                && Enum.TryParse<OrderStatus>(input.Status, ignoreCase: true, out var status))
            {
                query = query.Where(x => x.Status == status);
            }

            // Item 82: lọc theo shipper được gán.
            if (input.ShipperId.HasValue && input.ShipperId.Value != Guid.Empty)
            {
                var shipperId = input.ShipperId.Value;
                query = query.Where(x => x.ShipperId == shipperId);
            }

            // Wave 6c (item 35): khoảng ngày theo giờ Việt Nam — đầu ngày
            // FromDate (biên bao) đến hết ngày ToDate (biên độc quyền).
            if (input.FromDate.HasValue)
            {
                var fromUtc = VietnamTime.DayStartUtc(input.FromDate.Value);
                query = query.Where(x => x.CreatedAt >= fromUtc);
            }

            if (input.ToDate.HasValue)
            {
                var toUtc = VietnamTime.DayEndExclusiveUtc(input.ToDate.Value);
                query = query.Where(x => x.CreatedAt < toUtc);
            }

            // Item 163: lọc theo trạng thái đã thu tiền mặt.
            if (input.CashCollected.HasValue)
            {
                var collected = input.CashCollected.Value;
                query = query.Where(x => x.CashCollected == collected);
            }

            return query;
        }
    }
}
