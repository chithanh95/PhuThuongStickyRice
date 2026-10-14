using PhuThuongStickyRice.Application.Shop.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Shop
{
    public interface IShopSettingAppService : IApplicationService
    {
        Task<ShopSettingDto> GetAsync();

        Task<ShopSettingDto> UpdateAsync(ShopSettingDto dto);
    }

    public class ShopSettingAppService : ApplicationService, IShopSettingAppService
    {
        private readonly IRepository<ShopSetting, Guid> _settings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public ShopSettingAppService(
            IRepository<ShopSetting, Guid> settings,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _settings = settings;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>Public — the checkout page needs the fee rules.</summary>
        [AllowAnonymous]
        public async Task<ShopSettingDto> GetAsync()
        {
            var entity = await GetSingleAsync();
            return MapToDto(entity ?? ShopSetting.CreateDefault());
        }

        [Authorize(Roles = "Admin")]
        public async Task<ShopSettingDto> UpdateAsync(ShopSettingDto dto)
        {
            var entity = await GetSingleAsync();
            if (entity == null)
            {
                entity = ShopSetting.CreateDefault();
                await _settings.InsertAsync(entity, autoSave: true);
            }

            entity.DeliveryFee = Math.Max(0, dto.DeliveryFee);
            entity.FreeShipThreshold = Math.Max(0, dto.FreeShipThreshold);
            entity.MinOrderAmount = Math.Max(0, dto.MinOrderAmount);
            entity.EstimatedDeliveryMinutes = dto.EstimatedDeliveryMinutes < 0 ? 45 : dto.EstimatedDeliveryMinutes;
            await _settings.UpdateAsync(entity, autoSave: true);

            return MapToDto(entity);
        }

        private async Task<ShopSetting> GetSingleAsync()
        {
            var queryable = await _settings.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(queryable);
        }

        private static ShopSettingDto MapToDto(ShopSetting entity)
        {
            return new ShopSettingDto
            {
                DeliveryFee = entity.DeliveryFee,
                FreeShipThreshold = entity.FreeShipThreshold,
                MinOrderAmount = entity.MinOrderAmount,
                EstimatedDeliveryMinutes = entity.EstimatedDeliveryMinutes
            };
        }
    }
}
