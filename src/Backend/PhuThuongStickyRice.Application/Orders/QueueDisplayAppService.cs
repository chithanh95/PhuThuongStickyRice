// CS8632: file dùng annotation nullable (?); bật ngữ cảnh annotations để
// compiler hiểu đúng ý nghĩa, không bật phân tích luồng (dự án chưa bật nullable toàn cục).
#nullable enable annotations

using Microsoft.Extensions.Caching.Memory;
using PhuThuongStickyRice.Application.Common;
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
    /// <summary>
    /// Hợp đồng màn hình số thứ tự chờ của quán (item 178): trả về các đơn
    /// đang chuẩn bị và được mời lấy món để hiển thị trên TV trang /cho.
    /// Public không cần đăng nhập, không lộ thông tin khách hàng.
    /// </summary>
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

    /// <summary>
    /// Item 178: màn hình số thứ tự chờ ("Đang chuẩn bị" / "Mời lấy món")
    /// cho khu vực chờ của quán (trang /cho, thường hiển thị trên TV).
    /// Public không cần đăng nhập; chỉ trả về số thứ tự, mã đơn rút gọn
    /// và trạng thái thô — không lộ thông tin khách hàng.
    /// </summary>
    public class QueueDisplayAppService : ApplicationService, IQueueDisplayAppService
    {
        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IMemoryCache _memoryCache;

        // perf-hunt 2026-10-19: màn hình chờ /cho poll endpoint này mỗi 15s.
        // Cache 15s — stale tối đa đúng 1 chu kỳ poll, không cần logic
        // invalidate vì queue number luôn tính lại từ chuỗi đơn trong ngày.
        private const string NowServingCacheKey = "queue:now-serving";
        private static readonly TimeSpan NowServingCacheTtl = TimeSpan.FromSeconds(15);

        // Best practice: tập trạng thái hiển thị là static readonly HashSet —
        // endpoint bị poll mỗi 15s nên không cấp phát array mới + scan tuyến
        // tính mỗi request; Contains O(1) trong vòng lặp dựng vé. Cùng 3
        // trạng thái như bản cũ, không đổi ngữ nghĩa lọc.
        private static readonly HashSet<OrderStatus> VisibleStatuses = new()
        {
            OrderStatus.Confirmed,
            OrderStatus.Preparing,
            OrderStatus.Delivering
        };

        /// <summary>Nhãn trạng thái vé chờ trên màn hình TV khi đơn đang chuẩn bị.</summary>
        private const string PreparingStatusLabel = "Preparing";
        /// <summary>Nhãn trạng thái vé chờ trên màn hình TV khi đơn đã xong, mời khách ra lấy món.</summary>
        private const string ReadyStatusLabel = "Ready";

        /// <summary>Số ký tự cuối của mã đơn dùng làm mã rút gọn hiển thị trên màn hình chờ.</summary>
        private const int ShortCodeTailLength = 3;

        /// <summary>
        /// Khởi tạo service hiển thị hàng chờ.
        /// </summary>
        /// <param name="orders">Repository đơn hàng.</param>
        /// <param name="asyncExecuter">Thực thi truy vấn bất đồng bộ.</param>
        public QueueDisplayAppService(
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter,
            IMemoryCache memoryCache)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Public: danh sách vé số thứ tự cho màn hình chờ (/cho, TV khu vực chờ).
        /// Chỉ lấy các đơn trong ngày hôm nay (giờ Việt Nam), chưa hủy, ở trạng
        /// thái Đã xác nhận / Đang chuẩn bị / Mời lấy món. Số thứ tự là vị trí
        /// của đơn trong tổng số đơn hôm nay — lọc trạng thái trong memory để
        /// không đánh lại số trên màn hình. Đơn "Mời lấy món" xếp trước vì
        /// khách cần ra quầy lấy ngay.
        /// </summary>
        /// <returns>Danh sách vé: số thứ tự, mã đơn rút gọn (3 ký tự cuối), trạng thái Ready/Preparing.</returns>
        public virtual async Task<List<QueueTicketDto>> GetNowServingAsync()
        {
            if (_memoryCache.TryGetValue(NowServingCacheKey, out List<QueueTicketDto> cached))
            {
                return cached;
            }

            var result = await QueryNowServingAsync();
            _memoryCache.Set(NowServingCacheKey, result, NowServingCacheTtl);
            return result;
        }

        /// <summary>
        /// Truy vấn đơn của ngày hôm nay (giờ VN, loại đơn đã hủy) rồi dựng
        /// danh sách vé chờ: QueueNumber = vị trí đơn trong chuỗi đơn cả ngày
        /// (kể cả đơn bị ẩn), đơn "Mời lấy món" xếp trước để khách ra lấy ngay.
        /// Lọc trạng thái ở memory thay vì SQL để số thứ tự trên màn hình TV
        /// không bị đánh lại mỗi lần có đơn đổi trạng thái.
        /// </summary>
        /// <returns>Danh sách vé chờ đã sắp xếp (mời lấy món trước).</returns>
        private async Task<List<QueueTicketDto>> QueryNowServingAsync()
        {
            // Biên ngày theo giờ Việt Nam (không có DST, offset lấy từ VietnamTime). CreatedAt lưu dạng UTC.
            var vnNow = VietnamTime.ToVietnamTime(DateTime.UtcNow);
            var dayStartUtc = VietnamTime.DayStartUtc(vnNow.Date);
            var dayEndUtc = VietnamTime.DayEndExclusiveUtc(vnNow.Date);

            var queryable = await _orders.GetQueryableAsync();
            // perf-hunt 2026-10-19: this endpoint is polled every 15s by the
            // /cho waiting screen. Project only the two columns the ticket
            // loop needs instead of materializing full Order entities.
            // NOTE: the row set and in-memory status filter are unchanged on
            // purpose — QueueNumber (= position in the day's order sequence)
            // is shown on the TV screen, so filtering statuses in SQL would
            // renumber the visible tickets (business change).
            // CS8143: expression tree không chứa tuple literal được — project
            // anonymous type trong SQL rồi map sang tuple ở memory (lambda
            // thường, không phải expression tree).
            var today = (await _asyncExecuter.ToListAsync(
                queryable
                    .Where(x => x.CreatedAt >= dayStartUtc
                        && x.CreatedAt < dayEndUtc
                        && x.Status != OrderStatus.Cancelled)
                    .OrderBy(x => x.CreatedAt)
                    .Select(x => new { x.Status, x.OrderNumber })))
                .Select(x => (x.Status, x.OrderNumber))
                .ToList();

            var tickets = MapQueueTickets(today);

            // "Mời lấy món" first — those customers need to act.
            return tickets
                .OrderBy(x => x.Status == ReadyStatusLabel ? 0 : 1)
                .ThenBy(x => x.QueueNumber)
                .ToList();
        }

        /// <summary>
        /// Dựng danh sách vé chờ từ đơn trong ngày: QueueNumber = vị trí đơn
        /// trong chuỗi đơn của ngày (kể cả đơn đã ẩn khỏi màn hình — lọc trạng
        /// thái ở memory để không đánh số lại, xem NOTE ở
        /// <see cref="QueryNowServingAsync"/>).
        /// </summary>

        // CA1859: method private, caller duy nhất truyền List — khai báo
        // List cụ thể để indexer/Count không qua interface dispatch.
        // Giữ string? như cũ vì OrderNumber có thể null (xử lý ?? ở dưới).
        // Tập trạng thái hiển thị lấy từ VisibleStatuses (static HashSet) thay
        // vì tham số array — cùng 3 trạng thái, Contains O(1) mỗi vòng lặp.
        private static List<QueueTicketDto> MapQueueTickets(
            List<(OrderStatus Status, string? OrderNumber)> today)
        {
            var result = new List<QueueTicketDto>();
            for (var i = 0; i < today.Count; i++)
            {
                var order = today[i];
                if (!VisibleStatuses.Contains(order.Status))
                {
                    continue;
                }

                var number = order.OrderNumber ?? string.Empty;
                result.Add(new QueueTicketDto
                {
                    QueueNumber = i + 1,
                    ShortCode = number.Length <= ShortCodeTailLength
                        ? number.ToUpperInvariant()
                        : number[^ShortCodeTailLength..].ToUpperInvariant(),
                    Status = order.Status == OrderStatus.Delivering ? ReadyStatusLabel : PreparingStatusLabel
                });
            }

            return result;
        }
    }
}
