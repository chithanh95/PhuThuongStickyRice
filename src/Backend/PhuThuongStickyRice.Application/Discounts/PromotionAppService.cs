
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
using PhuThuongStickyRice.Application.Discounts;

namespace PhuThuongStickyRice.Application.Discounts
{
    /// <summary>
    /// Item 91: "Khuyến mãi giờ vàng" time-window promotions.
    /// The best active promotion is auto-applied at checkout (Vietnam time).
    /// </summary>
    public interface IPromotionAppService : IApplicationService
    {
        Task<List<PromotionDto>> GetListAsync();

        Task<PromotionDto> CreateAsync(CreatePromotionDto input);

        Task<PromotionDto> UpdateAsync(Guid id, CreatePromotionDto input);

        Task DeleteAsync(Guid id);

        /// <summary>
        /// Best (highest percent) active promotion for the given moment
        /// (Vietnam local time). Null when none applies.
        /// </summary>
        Task<PromotionDto> GetBestActiveAsync(DateTime? vnNow = null);
    }

    [Authorize(Roles = "Admin")]
    public class PromotionAppService : ApplicationService, IPromotionAppService
    {
        private readonly IRepository<Promotion, Guid> _promotions;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        public PromotionAppService(
            IRepository<Promotion, Guid> promotions,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _promotions = promotions;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        public virtual async Task<List<PromotionDto>> GetListAsync()
        {
            var queryable = await _promotions.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.IsActive).ThenBy(x => x.Name));
            return items.Select(Map).ToList();
        }

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

        public virtual async Task DeleteAsync(Guid id)
        {
            await _promotions.DeleteAsync(id, autoSave: true);
            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Public: the checkout page previews the active promotion.
        /// The IsActive list is cached (5 min); the time-window filter still
        /// runs per call so start/end semantics are unchanged.
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<PromotionDto> GetBestActiveAsync(DateTime? vnNow = null)
        {
            var now = vnNow ?? DateTime.UtcNow.AddHours(7);
            var active = await _catalogCache.GetOrAddAsync("promotions:active-list", QueryActiveAsync);
            var best = active
                .Where(x => MatchesWindow(x, now))
                .OrderByDescending(x => x.Dto.DiscountPercent)
                .FirstOrDefault();
            return best?.Dto;
        }

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
            public PromotionDto Dto { get; init; }
        }

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
                return time >= p.StartTime && time < p.EndTime;
            }

            // Overnight window, e.g. 22:00–02:00.
            return time >= p.StartTime || time < p.EndTime;
        }

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
