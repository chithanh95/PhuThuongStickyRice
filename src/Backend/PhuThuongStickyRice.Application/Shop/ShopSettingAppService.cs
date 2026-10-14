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
            entity.BankTransferExpiryMinutes = dto.BankTransferExpiryMinutes < 1 ? 30 : dto.BankTransferExpiryMinutes;
            entity.PointsPerVnd = dto.PointsPerVnd <= 0 ? 10000 : dto.PointsPerVnd;
            entity.PointsRedeemUnit = dto.PointsRedeemUnit < 1 ? 100 : dto.PointsRedeemUnit;
            entity.PointsRedeemValue = Math.Max(0, dto.PointsRedeemValue);
            entity.ReferralBonusPoints = Math.Max(0, dto.ReferralBonusPoints);
            entity.DepositThreshold = Math.Max(0, dto.DepositThreshold);
            entity.DepositPercent = Math.Min(100, Math.Max(0, dto.DepositPercent));
            entity.GiftFee = Math.Max(0, dto.GiftFee);
            entity.TierSilverMin = Math.Max(0, dto.TierSilverMin);
            entity.TierSilverDiscount = Math.Min(100, Math.Max(0, dto.TierSilverDiscount));
            entity.TierGoldMin = Math.Max(0, dto.TierGoldMin);
            entity.TierGoldDiscount = Math.Min(100, Math.Max(0, dto.TierGoldDiscount));
            entity.OwnerReportEmail = string.IsNullOrWhiteSpace(dto.OwnerReportEmail)
                ? null
                : dto.OwnerReportEmail.Trim();
            entity.UnconfirmedAlertMinutes = dto.UnconfirmedAlertMinutes < 1 ? 15 : dto.UnconfirmedAlertMinutes;
            entity.RecaptchaSiteKey = string.IsNullOrWhiteSpace(dto.RecaptchaSiteKey)
                ? null
                : dto.RecaptchaSiteKey.Trim();
            entity.RecaptchaSecretKey = string.IsNullOrWhiteSpace(dto.RecaptchaSecretKey)
                ? null
                : dto.RecaptchaSecretKey.Trim();
            entity.WebBaseUrl = string.IsNullOrWhiteSpace(dto.WebBaseUrl)
                ? null
                : dto.WebBaseUrl.Trim().TrimEnd('/');
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
            // Rows created before Wave 2 have 0 in the new columns; fall back
            // to the documented defaults on read so old rows keep working.
            return new ShopSettingDto
            {
                DeliveryFee = entity.DeliveryFee,
                FreeShipThreshold = entity.FreeShipThreshold,
                MinOrderAmount = entity.MinOrderAmount,
                EstimatedDeliveryMinutes = entity.EstimatedDeliveryMinutes,
                BankTransferExpiryMinutes = entity.BankTransferExpiryMinutes < 1 ? 30 : entity.BankTransferExpiryMinutes,
                PointsPerVnd = entity.PointsPerVnd <= 0 ? 10000 : entity.PointsPerVnd,
                PointsRedeemUnit = entity.PointsRedeemUnit < 1 ? 100 : entity.PointsRedeemUnit,
                PointsRedeemValue = entity.PointsRedeemValue,
                ReferralBonusPoints = entity.ReferralBonusPoints,
                DepositThreshold = entity.DepositThreshold,
                DepositPercent = entity.DepositPercent,
                GiftFee = entity.GiftFee,
                TierSilverMin = entity.TierSilverMin,
                TierSilverDiscount = entity.TierSilverDiscount,
                TierGoldMin = entity.TierGoldMin,
                TierGoldDiscount = entity.TierGoldDiscount,
                OwnerReportEmail = entity.OwnerReportEmail,
                UnconfirmedAlertMinutes = entity.UnconfirmedAlertMinutes < 1 ? 15 : entity.UnconfirmedAlertMinutes,
                RecaptchaSiteKey = entity.RecaptchaSiteKey,
                RecaptchaSecretKey = entity.RecaptchaSecretKey,
                WebBaseUrl = entity.WebBaseUrl
            };
        }
    }
}
