#nullable enable

using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Discounts;

namespace PhuThuongStickyRice.Application.Discounts
{
    /// <summary>
    /// Item 91: hợp đồng khuyến mãi "giờ vàng" theo khung giờ.
    /// Khuyến mãi có % giảm cao nhất đang diễn ra được checkout tự áp theo giờ Việt Nam.
    /// <para>
    /// Hợp đồng khuyến mãi "giờ vàng": admin CRUD; checkout gọi
    /// <c>GetBestActiveAsync</c> (không cần login) để lấy khuyến mãi tốt nhất
    /// đang diễn ra cho đơn hàng.
    /// </para>
    /// </summary>
    public interface IPromotionAppService : IApplicationService
    {
        /// <summary>Admin: liệt kê khuyến mãi giờ vàng (đang bật lên trước, rồi theo tên).</summary>
        /// <returns>Danh sách khuyến mãi.</returns>
        Task<List<PromotionDto>> GetListAsync();

        /// <summary>Admin: tạo khuyến mãi giờ vàng mới.</summary>
        /// <param name="input">Tên, % giảm, khung giờ, ngày áp dụng trong tuần, trạng thái bật.</param>
        /// <returns>Khuyến mãi vừa tạo.</returns>
        Task<PromotionDto> CreateAsync(CreatePromotionDto input);

        /// <summary>Admin: cập nhật khuyến mãi giờ vàng.</summary>
        /// <param name="id">Id khuyến mãi.</param>
        /// <param name="input">Thông tin mới của khuyến mãi.</param>
        /// <returns>Khuyến mãi sau khi cập nhật.</returns>
        Task<PromotionDto> UpdateAsync(Guid id, CreatePromotionDto input);

        /// <summary>Admin: xóa khuyến mãi giờ vàng.</summary>
        /// <param name="id">Id khuyến mãi cần xóa.</param>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Khuyến mãi có % giảm cao nhất đang diễn ra tại thời điểm cho trước
        /// (giờ Việt Nam). Trả null khi không có khuyến mãi nào áp dụng.
        /// </summary>
        /// <param name="vnNow">Thời điểm kiểm tra (giờ Việt Nam); null thì dùng thời gian hiện tại.</param>
        /// <returns>DTO khuyến mãi tốt nhất, hoặc null.</returns>
        Task<PromotionDto?> GetBestActiveAsync(DateTime? vnNow = null);
    }

    /// <summary>
    /// Item 91: khuyến mãi "giờ vàng" theo khung giờ + ngày trong tuần
    /// (ví dụ 10:00–14:00 các ngày T2–T6). Khuyến mãi có % giảm cao nhất
    /// đang diễn ra được checkout tự áp theo giờ Việt Nam.
    /// Danh sách KM đang bật được cache 5 phút; khung giờ vẫn lọc theo từng
    /// request để giờ bắt đầu/kết thúc luôn chính xác.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class PromotionAppService : ApplicationService, IPromotionAppService
    {
        private readonly IRepository<Promotion, Guid> _promotions;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        /// <summary>Khởi tạo service khuyến mãi giờ vàng.</summary>
        public PromotionAppService(
            IRepository<Promotion, Guid> promotions,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _promotions = promotions;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Liệt kê khuyến mãi (đang bật lên trước, rồi theo tên) cho trang admin.
        /// </summary>
        /// <returns>Danh sách khuyến mãi.</returns>
        public virtual async Task<List<PromotionDto>> GetListAsync()
        {
            var queryable = await _promotions.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.IsActive).ThenBy(x => x.Name));
            return items.Select(Map).ToList();
        }

        /// <summary>
        /// Tạo khuyến mãi mới. Giờ bắt đầu/kết thúc nhập dạng "HH:mm";
        /// sai định dạng thì báo lỗi thân thiện. Cache catalog được xóa
        /// để trang shop thấy KM mới ngay.
        /// </summary>
        /// <param name="input">Tên, % giảm, khung giờ, ngày áp dụng trong tuần, trạng thái bật.</param>
        /// <returns>Khuyến mãi vừa tạo.</returns>
        public virtual async Task<PromotionDto> CreateAsync(CreatePromotionDto input)
        {
            var promotion = new Promotion
            {
                Name = input.Name.Trim(),
                DiscountPercent = input.DiscountPercent,
                StartTime = ParseTime(input.StartTime),
                EndTime = ParseTime(input.EndTime),
                DaysOfWeekFlags = input.DaysOfWeekFlags,
                IsActive = input.IsActive
            };
            await _promotions.InsertAsync(promotion, autoSave: true);
            _catalogCache.Invalidate();
            return Map(promotion);
        }

        /// <summary>
        /// Cập nhật khuyến mãi (ghi đè toàn bộ khung giờ, ngày áp dụng, trạng thái).
        /// Cache catalog được xóa để trang shop thấy thay đổi ngay.
        /// </summary>
        /// <param name="id">Id khuyến mãi.</param>
        /// <param name="input">Thông tin mới của khuyến mãi.</param>
        /// <returns>Khuyến mãi sau khi cập nhật.</returns>
        public virtual async Task<PromotionDto> UpdateAsync(Guid id, CreatePromotionDto input)
        {
            var promotion = await _promotions.GetAsync(id);
            promotion.Name = input.Name.Trim();
            promotion.DiscountPercent = input.DiscountPercent;
            promotion.StartTime = ParseTime(input.StartTime);
            promotion.EndTime = ParseTime(input.EndTime);
            promotion.DaysOfWeekFlags = input.DaysOfWeekFlags;
            promotion.IsActive = input.IsActive;
            await _promotions.UpdateAsync(promotion, autoSave: true);
            _catalogCache.Invalidate();
            return Map(promotion);
        }

        /// <summary>
        /// Xóa khuyến mãi và xóa cache catalog để trang shop hết hiển thị KM này.
        /// </summary>
        /// <param name="id">Id khuyến mãi cần xóa.</param>
        public virtual async Task DeleteAsync(Guid id)
        {
            await _promotions.DeleteAsync(id, autoSave: true);
            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Public: checkout gọi để xem trước khuyến mãi đang diễn ra.
        /// Danh sách KM đang bật được cache 5 phút (giống mô tả class), còn
        /// lọc theo khung giờ vẫn chạy theo từng request nên giờ bắt đầu/kết
        /// thúc luôn chính xác khi admin đổi.
        /// </summary>
        /// <param name="vnNow">Thời điểm kiểm tra (giờ Việt Nam); null thì dùng thời gian hiện tại.</param>
        /// <returns>DTO khuyến mãi có % giảm cao nhất đang diễn ra, hoặc null.</returns>
        [AllowAnonymous]
        public virtual async Task<PromotionDto?> GetBestActiveAsync(DateTime? vnNow = null)
        {
            // Admin có thể truyền vnNow để preview "lúc này có KM nào"; nếu không
            // truyền thì dùng giờ hiện tại của Việt Nam (UTC+7).
            var now = vnNow ?? DateTime.UtcNow.AddHours(VietnamTime.UtcOffsetHours);
            var active = await _catalogCache.GetOrAddAsync("promotions:active-list", QueryActiveAsync);
            var best = active
                .Where(x => MatchesWindow(x, now))
                .OrderByDescending(x => x.Dto.DiscountPercent)
                .FirstOrDefault();
            return best?.Dto;
        }

        /// <summary>
        /// Truy vấn DB lấy các khuyến mãi đang bật để nạp vào cache.
        /// Lọc khung giờ (giờ/ngày) không làm ở đây — cache chỉ giữ snapshot,
        /// còn kiểm tra khung giờ chạy riêng mỗi request trong <c>GetBestActiveAsync</c>.
        /// </summary>
        private async Task<List<CachedActivePromotion>> QueryActiveAsync()
        {
            var queryable = await _promotions.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive));
            return entities.Select(x => new CachedActivePromotion
            {
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                DaysOfWeekFlags = x.DaysOfWeekFlags,
                Dto = Map(x)
            }).ToList();
        }

        /// <summary>
        /// Immutable cached snapshot of an active promotion. Mirrors
        /// <see cref="Promotion.IsActiveAt"/> so the time check can run on
        /// cached data without a DB round-trip.
        /// </summary>
        private sealed class CachedActivePromotion
        {
            public TimeSpan StartTime { get; init; }
            public TimeSpan EndTime { get; init; }
            public int DaysOfWeekFlags { get; init; }
            // required: Map() luôn trả về PromotionDto non-null; snapshot chỉ
            // hợp lệ khi có Dto, compiler bắt buộc mọi nơi tạo đều phải gán.
            public required PromotionDto Dto { get; init; }
        }

        /// <summary>
        /// Kiểm tra 1 khuyến mãi có đang "trong khung giờ" tại thời điểm
        /// <paramref name="vnNow"/> (giờ Việt Nam) hay không. Ngày trong tuần
        /// kiểm tra bằng bit flags <c>PromotionDay</c>.
        /// </summary>
        /// <param name="p">Khuyến mãi (đã chắc chắn IsActive) cần kiểm tra.</param>
        /// <param name="vnNow">Thời điểm hiện tại theo giờ Việt Nam.</param>
        /// <returns>True nếu hôm nay được áp dụng và giờ nằm trong [StartTime, EndTime).</returns>
        // Same logic as Promotion.IsActiveAt (entity is IsActive already).
        private static bool MatchesWindow(CachedActivePromotion p, DateTime vnNow)
        {
            var dayFlag = vnNow.DayOfWeek switch
            {
                DayOfWeek.Monday => PromotionDay.Monday,
                DayOfWeek.Tuesday => PromotionDay.Tuesday,
                DayOfWeek.Wednesday => PromotionDay.Wednesday,
                DayOfWeek.Thursday => PromotionDay.Thursday,
                DayOfWeek.Friday => PromotionDay.Friday,
                DayOfWeek.Saturday => PromotionDay.Saturday,
                _ => PromotionDay.Sunday
            };

            if ((p.DaysOfWeekFlags & (int)dayFlag) == 0)
            {
                return false;
            }

            var time = vnNow.TimeOfDay;
            if (p.EndTime >= p.StartTime)
            {
                // Khung giờ trong ngày (vd 10:00–14:00): giờ phải nằm giữa 2 mốc.
                return time >= p.StartTime && time < p.EndTime;
            }

            // Khung giờ qua đêm (vd 22:00–02:00): giờ nằm sau mốc bắt đầu
            // HOẶC trước mốc kết thúc (tức là rạng sáng hôm sau).
            return time >= p.StartTime || time < p.EndTime;
        }

        /// <summary>Map entity Promotion → DTO (định dạng giờ về chuỗi "HH:mm" cho API/admin).</summary>
        private static PromotionDto Map(Promotion p) => new PromotionDto
        {
            Id = p.Id,
            Name = p.Name,
            DiscountPercent = p.DiscountPercent,
            StartTime = p.StartTime.ToString(@"hh\:mm"),
            EndTime = p.EndTime.ToString(@"hh\:mm"),
            DaysOfWeekFlags = p.DaysOfWeekFlags,
            IsActive = p.IsActive
        };

        /// <summary>
        /// Chuyển giờ dạng chuỗi "HH:mm" (vd "10:00") thành <c>TimeSpan</c>.
        /// Chấp nhận cả "h:mm" và định dạng mặc định; sai thì báo lỗi thân thiện.
        /// </summary>
        /// <param name="value">Chuỗi giờ nhập từ form admin.</param>
        /// <returns>TimeSpan tương ứng trong ngày.</returns>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi giờ không hợp lệ.</exception>
        private static TimeSpan ParseTime(string value)
        {
            if (TimeSpan.TryParseExact(value?.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out var t)
                || TimeSpan.TryParseExact(value?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out t)
                || TimeSpan.TryParse(value?.Trim(), out t))
            {
                return t;
            }

            throw new Domain.Exceptions.DomainException(
                $"Giờ '{value}' không hợp lệ. Dùng định dạng HH:mm.");
        }
    }
}
