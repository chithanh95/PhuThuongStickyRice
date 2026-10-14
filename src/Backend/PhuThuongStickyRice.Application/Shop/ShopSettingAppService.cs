using PhuThuongStickyRice.Application.Shop.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
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
        /// <summary>
        /// Item 136: marker prefix for DataProtection-encrypted values at
        /// rest. Rows written before encryption (plain text) are read as-is
        /// and re-encrypted on the next save.
        /// </summary>
        private const string EncryptedPrefix = "DPv1:";

        private readonly IRepository<ShopSetting, Guid> _settings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IDataProtector _protector;

        public ShopSettingAppService(
            IRepository<ShopSetting, Guid> settings,
            IAsyncQueryableExecuter asyncExecuter,
            IDataProtectionProvider dataProtectionProvider)
        {
            _settings = settings;
            _asyncExecuter = asyncExecuter;
            _protector = dataProtectionProvider.CreateProtector("PhuThuongStickyRice.ShopSettings");
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
            entity.RecaptchaSecretKey = ProtectSecret(dto.RecaptchaSecretKey);
            entity.WebBaseUrl = string.IsNullOrWhiteSpace(dto.WebBaseUrl)
                ? null
                : dto.WebBaseUrl.Trim().TrimEnd('/');
            entity.MaintenanceMode = dto.MaintenanceMode;
            entity.MaintenanceMessage = string.IsNullOrWhiteSpace(dto.MaintenanceMessage)
                ? "Cửa hàng đang bảo trì, vui lòng quay lại sau."
                : dto.MaintenanceMessage.Trim();
            entity.TelegramBotToken = ProtectSecret(dto.TelegramBotToken);
            entity.TelegramChatId = string.IsNullOrWhiteSpace(dto.TelegramChatId)
                ? null
                : dto.TelegramChatId.Trim();
            entity.HealthAlertsEnabled = dto.HealthAlertsEnabled;
            entity.ErrorRateThreshold = dto.ErrorRateThreshold < 1 ? 20 : dto.ErrorRateThreshold;
            entity.UploadsSizeWarningMb = dto.UploadsSizeWarningMb < 1 ? 500 : dto.UploadsSizeWarningMb;
            entity.BankName = TrimOrNull(dto.BankName);
            entity.BankId = TrimOrNull(dto.BankId);
            entity.BankAccountNumber = TrimOrNull(dto.BankAccountNumber);
            entity.BankAccountName = TrimOrNull(dto.BankAccountName);
            entity.BankQrTemplate = TrimOrNull(dto.BankQrTemplate);
            entity.MoMoPartnerCode = TrimOrNull(dto.MoMoPartnerCode);
            entity.MoMoEndpoint = TrimOrNull(dto.MoMoEndpoint);
            entity.ImageBackupProvider = string.IsNullOrWhiteSpace(dto.ImageBackupProvider)
                ? "None"
                : dto.ImageBackupProvider.Trim();
            entity.ImageBackupSettingsJson = TrimOrNull(dto.ImageBackupSettingsJson);
            entity.CheckInPoints = dto.CheckInPoints < 0 ? 5 : dto.CheckInPoints;
            entity.NepPerPortionGrams = dto.NepPerPortionGrams < 1 ? 150 : dto.NepPerPortionGrams;
            entity.DailyTargetAmount = Math.Max(0, dto.DailyTargetAmount);
            await _settings.UpdateAsync(entity, autoSave: true);

            return MapToDto(entity);
        }

        private static string TrimOrNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Item 136: encrypts a secret for storage. Null/empty stays null.
        /// </summary>
        private string ProtectSecret(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return EncryptedPrefix + _protector.Protect(value.Trim());
        }

        /// <summary>
        /// Item 136: decrypts a stored secret. Values without the prefix are
        /// legacy plaintext and are returned as-is.
        /// </summary>
        private string UnprotectSecret(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            if (!stored.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
            {
                return stored;
            }

            try
            {
                return _protector.Unprotect(stored.Substring(EncryptedPrefix.Length));
            }
            catch
            {
                // Key ring unavailable (e.g. fresh deploy without the shared
                // key store) — never crash reads; the admin can re-save.
                Logger.LogWarning("Could not decrypt a stored shop secret; returning null.");
                return null;
            }
        }

        private async Task<ShopSetting> GetSingleAsync()
        {
            var queryable = await _settings.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(queryable);
        }

        private ShopSettingDto MapToDto(ShopSetting entity)
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
                RecaptchaSecretKey = UnprotectSecret(entity.RecaptchaSecretKey),
                WebBaseUrl = entity.WebBaseUrl,
                MaintenanceMode = entity.MaintenanceMode,
                MaintenanceMessage = string.IsNullOrWhiteSpace(entity.MaintenanceMessage)
                    ? "Cửa hàng đang bảo trì, vui lòng quay lại sau."
                    : entity.MaintenanceMessage,
                TelegramBotToken = UnprotectSecret(entity.TelegramBotToken),
                TelegramChatId = entity.TelegramChatId,
                HealthAlertsEnabled = entity.HealthAlertsEnabled,
                ErrorRateThreshold = entity.ErrorRateThreshold < 1 ? 20 : entity.ErrorRateThreshold,
                UploadsSizeWarningMb = entity.UploadsSizeWarningMb < 1 ? 500 : entity.UploadsSizeWarningMb,
                BankName = entity.BankName,
                BankId = entity.BankId,
                BankAccountNumber = entity.BankAccountNumber,
                BankAccountName = entity.BankAccountName,
                BankQrTemplate = entity.BankQrTemplate,
                MoMoPartnerCode = entity.MoMoPartnerCode,
                MoMoEndpoint = entity.MoMoEndpoint,
                ImageBackupProvider = string.IsNullOrWhiteSpace(entity.ImageBackupProvider)
                    ? "None"
                    : entity.ImageBackupProvider,
                ImageBackupSettingsJson = entity.ImageBackupSettingsJson,
                CheckInPoints = entity.CheckInPoints,
                NepPerPortionGrams = entity.NepPerPortionGrams < 1 ? 150 : entity.NepPerPortionGrams,
                DailyTargetAmount = entity.DailyTargetAmount
            };
        }
    }
}
