
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Common
{
    public interface IFeatureFlagAppService : IApplicationService
    {
        /// <summary>Admin: all flags with metadata.</summary>
        Task<List<FeatureFlagDto>> GetListAsync();

        /// <summary>Admin: flip a flag.</summary>
        Task<FeatureFlagDto> SetEnabledAsync(string key, bool enabled);

        /// <summary>Public: map of key → enabled for UI gating.</summary>
        Task<Dictionary<string, bool>> GetEnabledMapAsync();

        /// <summary>Internal: true when the flag exists and is enabled.</summary>
        Task<bool> IsEnabledAsync(string key);
    }

    public class FeatureFlagAppService : ApplicationService, IFeatureFlagAppService
    {
        private readonly IRepository<FeatureFlag, Guid> _flags;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        private static readonly (string Key, string Name, string Description)[] Defaults =
        {
            (FeatureFlag.LuckySpin, "Vòng quay may mắn", "Mini-game quay thưởng sau khi đặt đơn thành công."),
            (FeatureFlag.GiftWrap, "Gói quà", "Cho phép khách chọn gói quà (tính phí) ở bước thanh toán."),
            (FeatureFlag.Referrals, "Giới thiệu bạn bè", "Nhập mã giới thiệu ở bước thanh toán để nhận điểm thưởng."),
            (FeatureFlag.PromoPopup, "Popup khuyến mãi", "Hiện popup khuyến mãi trên trang chủ (1 lần/phiên).")
        };

        public FeatureFlagAppService(
            IRepository<FeatureFlag, Guid> flags,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _flags = flags;
            _asyncExecuter = asyncExecuter;
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<FeatureFlagDto>> GetListAsync()
        {
            await EnsureDefaultsAsync();
            var queryable = await _flags.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(queryable.OrderBy(x => x.Key));
            return entities.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<FeatureFlagDto> SetEnabledAsync(string key, bool enabled)
        {
            await EnsureDefaultsAsync();
            var entity = await FindOrThrowAsync(key);
            entity.IsEnabled = enabled;
            await _flags.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [AllowAnonymous]
        public virtual async Task<Dictionary<string, bool>> GetEnabledMapAsync()
        {
            await EnsureDefaultsAsync();
            var queryable = await _flags.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(queryable);
            return entities.ToDictionary(x => x.Key, x => x.IsEnabled);
        }

        public virtual async Task<bool> IsEnabledAsync(string key)
        {
            var queryable = await _flags.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Key == key));
            // Unknown flags default to enabled so a missing seed row can
            // never break an existing feature.
            return entity == null || entity.IsEnabled;
        }

        private async Task EnsureDefaultsAsync()
        {
            var queryable = await _flags.GetQueryableAsync();
            var existing = (await _asyncExecuter.ToListAsync(queryable))
                .Select(x => x.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (key, name, description) in Defaults)
            {
                if (existing.Contains(key))
                {
                    continue;
                }

                await _flags.InsertAsync(new FeatureFlag
                {
                    Key = key,
                    Name = name,
                    Description = description,
                    IsEnabled = true
                }, autoSave: true);
            }
        }

        private async Task<FeatureFlag> FindOrThrowAsync(string key)
        {
            var queryable = await _flags.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Key == key));
            if (entity == null)
            {
                throw new Domain.Exceptions.DomainException($"Feature flag '{key}' không tồn tại.");
            }

            return entity;
        }

        private static FeatureFlagDto MapToDto(FeatureFlag entity)
        {
            return new FeatureFlagDto
            {
                Id = entity.Id,
                Key = entity.Key,
                Name = entity.Name,
                IsEnabled = entity.IsEnabled,
                Description = entity.Description
            };
        }
    }
}
