using PhuThuongStickyRice.Application.Wave3.DTOs;
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

namespace PhuThuongStickyRice.Application.Wave3
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

        public PromotionAppService(
            IRepository<Promotion, Guid> promotions,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _promotions = promotions;
            _asyncExecuter = asyncExecuter;
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
            return Map(promotion);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _promotions.DeleteAsync(id, autoSave: true);
        }

        /// <summary>Public: the checkout page previews the active promotion.</summary>
        [AllowAnonymous]
        public virtual async Task<PromotionDto> GetBestActiveAsync(DateTime? vnNow = null)
        {
            var now = vnNow ?? DateTime.UtcNow.AddHours(7);
            var queryable = await _promotions.GetQueryableAsync();
            var active = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive));
            var best = active
                .Where(x => x.IsActiveAt(now))
                .OrderByDescending(x => x.DiscountPercent)
                .FirstOrDefault();
            return best == null ? null : Map(best);
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
