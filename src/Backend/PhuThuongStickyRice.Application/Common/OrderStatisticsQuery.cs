using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhuThuongStickyRice.Domain.Entities;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Truy vấn thống kê dùng chung về đơn hàng. Gom từ 2 bản copy-paste
    /// trước đây: <c>OrderAppService.LoadTodaySoldByProductAsync</c> (kiểm tra
    /// giới hạn suất/ngày khi tạo đơn) và <c>ProductAppService.QueryDailyRemainingAsync</c>
    /// (trang tồn kho theo ngày) — cùng logic "đơn chưa hủy trong khoảng
    /// [fromUtc, toUtc) → tổng số lượng theo món". Nhận
    /// <see cref="IQueryable{Order}"/> thay vì repository vì 2 nơi dùng 2
    /// kiểu repository khác nhau (<c>IOrderRepository</c> và
    /// <c>IRepository&lt;Order, Guid&gt;</c>). Đổi 1 lần (vd thêm trạng thái
    /// loại trừ) là áp dụng cả 2 nơi.
    /// </summary>
    // [codequality-smell 2026-10-20] Gộp đoạn logic trùng nhau ở 2 service.
    internal static class OrderStatisticsQuery
    {
        /// <summary>
        /// Tổng số lượng đã bán của từng món từ các đơn chưa hủy trong khoảng
        /// nửa mở [fromUtc, toUtc), trả về dictionary keyed theo ProductId.
        /// </summary>
        /// <param name="orderQueryable">Queryable đơn hàng (từ repository của bên gọi).</param>
        /// <param name="asyncExecuter">Executer chạy query async (ABP).</param>
        /// <param name="fromUtc">Đầu khoảng (giờ UTC, inclusive).</param>
        /// <param name="toUtc">Cuối khoảng (giờ UTC, exclusive).</param>
        internal static async Task<Dictionary<Guid, int>> SoldByProductAsync(
            IQueryable<Order> orderQueryable,
            IAsyncQueryableExecuter asyncExecuter,
            DateTime fromUtc,
            DateTime toUtc)
        {
            var soldRows = await asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && x.CreatedAt >= fromUtc
                        && x.CreatedAt < toUtc)
                    .SelectMany(x => x.Lines)
                    .GroupBy(l => l.ProductId)
                    .Select(g => new { ProductId = g.Key, Sold = g.Sum(l => l.Quantity) }));

            return soldRows.ToDictionary(x => x.ProductId, x => (int)x.Sold);
        }
    }
}
