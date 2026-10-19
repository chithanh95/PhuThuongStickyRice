using PhuThuongStickyRice.Application.Shop.DTOs;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Shop
{
    public interface IShopSettingAppService : IApplicationService
    {
        Task<ShopSettingDto> GetAsync();

        /// <summary>
        /// Public storefront view of the settings: same as <see cref="GetAsync"/>
        /// but a full-day holiday today automatically switches
        /// <c>AcceptingOrders</c> off with the holiday reason. Used by the
        /// landing/shop/checkout pages and by order creation gating.
        /// </summary>
        Task<ShopSettingDto> GetPublicAsync();

        Task<ShopSettingDto> UpdateAsync(ShopSettingDto dto);

        /// <summary>
        /// Item 180: posts a sample new-order payload to the given URL
        /// (or the saved one when empty). Returns (ok, message) — the
        /// admin UI shows the result; never throws.
        /// </summary>
        Task<(bool Ok, string Message)> TestNewOrderWebhookAsync(string url);
    }

    public class ShopSettingAppService : ApplicationService, IShopSettingAppService
    {
        /// <summary>
        /// Item 136: marker prefix for DataProtection-encrypted values at
        /// rest. Rows written before encryption (plain text) are read as-is
        /// and re-encrypted on the next save.
        /// </summary>
        private const string EncryptedPrefix = "DPv1:";

        // Fallback defaults applied when a setting is missing or invalid —
        // the same values are used on write (UpdateAsync) and on read
        // (MapToDto, for rows written before a column existed).
        private const int DefaultEstimatedDeliveryMinutes = 45;
        private const int DefaultBankTransferExpiryMinutes = 30;
        private const int DefaultPointsPerVnd = 10000;
        private const int DefaultPointsRedeemUnit = 100;
        private const int DefaultUnconfirmedAlertMinutes = 15;
        private const int DefaultErrorRateThreshold = 20;
        private const int DefaultUploadsSizeWarningMb = 500;
        private const int DefaultCheckInPoints = 5;
        private const int DefaultNepPerPortionGrams = 150;
        private const int MaxOrdersPerPhonePerDayLimit = 1000;
        private const int MaxWebhookUrlLength = 1000;
        private const string DefaultImageBackupProvider = "None";
        private const string DefaultOpenTime = "06:00";
        private const string DefaultCloseTime = "10:30";
        private const string DefaultMaintenanceMessage = "Cửa hàng đang bảo trì, vui lòng quay lại sau.";

        private readonly IRepository<ShopSetting, Guid> _settings;
        private readonly IRepository<HolidaySchedule, Guid> _holidays;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IDataProtector _protector;
        private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;
        private readonly Microsoft.Extensions.Logging.ILogger<ShopSettingAppService> _logger;
        private readonly IPermissionService _permissions;
        private readonly IStaffActivityLogger _activityLogger;
        private readonly Products.IProductCatalogCache _catalogCache;

        public ShopSettingAppService(
            IRepository<ShopSetting, Guid> settings,
            IRepository<HolidaySchedule, Guid> holidays,
            IAsyncQueryableExecuter asyncExecuter,
            IDataProtectionProvider dataProtectionProvider,
            System.Net.Http.IHttpClientFactory httpClientFactory,
            Microsoft.Extensions.Logging.ILogger<ShopSettingAppService> logger,
            IPermissionService permissions,
            IStaffActivityLogger activityLogger,
            Products.IProductCatalogCache catalogCache)
        {
            _settings = settings;
            _holidays = holidays;
            _asyncExecuter = asyncExecuter;
            _protector = dataProtectionProvider.CreateProtector("PhuThuongStickyRice.ShopSettings");
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _permissions = permissions;
            _activityLogger = activityLogger;
            _catalogCache = catalogCache;
        }

        /// <summary>Public — the checkout page needs the fee rules.</summary>
        [AllowAnonymous]
        public virtual async Task<ShopSettingDto> GetAsync()
        {
            // Perf: shop settings are read on every checkout + public pages.
            return await _catalogCache.GetOrAddAsync("shop-settings", async () =>
            {
                var entity = await GetSingleAsync();
                return MapToDto(entity ?? ShopSetting.CreateDefault());
            });
        }

        /// <summary>
        /// Public storefront settings with the holiday auto-pause applied:
        /// on a full-day holiday the shop is shown as paused with the holiday
        /// reason, without changing the stored manual toggle. A manual pause
        /// (AcceptingOrders = false) always wins over the holiday reason.
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<ShopSettingDto> GetPublicAsync()
        {
            var dto = await GetAsync();
            if (dto.AcceptingOrders)
            {
                var reason = await GetTodayHolidayReasonAsync();
                if (!string.IsNullOrEmpty(reason))
                {
                    dto.AcceptingOrders = false;
                    dto.PausedReason = $"Hôm nay quán nghỉ: {reason}";
                    dto.IsHolidayPaused = true;
                }
            }
            return dto;
        }

        /// <summary>Vietnam calendar date (UTC+7, no DST).</summary>
        private async Task<string> GetTodayHolidayReasonAsync()
        {
            try
            {
                var today = DateTime.UtcNow.AddHours(7).Date;
                // Perf: GetPublicAsync runs on every storefront page load, so the
                // holiday lookup must not hit the DB each time. Keyed by Vietnam
                // date so it rolls over at midnight; admin holiday CRUD busts it
                // via IProductCatalogCache.Invalidate.
                return await _catalogCache.GetOrAddAsync(
                    $"shop-settings:holiday:{today:yyyyMMdd}", async () =>
                    {
                        var queryable = await _holidays.GetQueryableAsync();
                        var holiday = await _asyncExecuter.FirstOrDefaultAsync(
                            queryable.Where(x => x.HolidayDate == today && x.IsFullDay));
                        return holiday?.Reason;
                    });
            }
            catch
            {
                // Fail-soft: a holiday lookup problem must never break the storefront.
                return null;
            }
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<ShopSettingDto> UpdateAsync(ShopSettingDto dto)
        {
            // Item 155: editing shop settings needs settings.edit.
            await _permissions.RequirePermissionAsync(PermissionKeys.SettingsEdit);

            var entity = await GetSingleAsync();
            if (entity == null)
            {
                entity = ShopSetting.CreateDefault();
                await _settings.InsertAsync(entity, autoSave: true);
            }

            entity.DeliveryFee = Math.Max(0, dto.DeliveryFee);
            entity.FreeShipThreshold = Math.Max(0, dto.FreeShipThreshold);
            entity.MinOrderAmount = Math.Max(0, dto.MinOrderAmount);
            entity.EstimatedDeliveryMinutes = dto.EstimatedDeliveryMinutes < 0 ? DefaultEstimatedDeliveryMinutes : dto.EstimatedDeliveryMinutes;
            entity.BankTransferExpiryMinutes = dto.BankTransferExpiryMinutes < 1 ? DefaultBankTransferExpiryMinutes : dto.BankTransferExpiryMinutes;
            entity.PointsPerVnd = dto.PointsPerVnd <= 0 ? DefaultPointsPerVnd : dto.PointsPerVnd;
            entity.PointsRedeemUnit = dto.PointsRedeemUnit < 1 ? DefaultPointsRedeemUnit : dto.PointsRedeemUnit;
            entity.PointsRedeemValue = Math.Max(0, dto.PointsRedeemValue);
            entity.ReferralBonusPoints = Math.Max(0, dto.ReferralBonusPoints);
            entity.DepositThreshold = Math.Max(0, dto.DepositThreshold);
            entity.DepositPercent = ClampPercent(dto.DepositPercent);
            entity.GiftFee = Math.Max(0, dto.GiftFee);
            entity.TierSilverMin = Math.Max(0, dto.TierSilverMin);
            entity.TierSilverDiscount = ClampPercent(dto.TierSilverDiscount);
            entity.TierGoldMin = Math.Max(0, dto.TierGoldMin);
            entity.TierGoldDiscount = ClampPercent(dto.TierGoldDiscount);
            entity.OwnerReportEmail = string.IsNullOrWhiteSpace(dto.OwnerReportEmail)
                ? null
                : dto.OwnerReportEmail.Trim();
            entity.UnconfirmedAlertMinutes = dto.UnconfirmedAlertMinutes < 1 ? DefaultUnconfirmedAlertMinutes : dto.UnconfirmedAlertMinutes;
            entity.RecaptchaSiteKey = string.IsNullOrWhiteSpace(dto.RecaptchaSiteKey)
                ? null
                : dto.RecaptchaSiteKey.Trim();
            entity.RecaptchaSecretKey = ProtectSecret(dto.RecaptchaSecretKey);
            entity.WebBaseUrl = string.IsNullOrWhiteSpace(dto.WebBaseUrl)
                ? null
                : dto.WebBaseUrl.Trim().TrimEnd('/');
            entity.MaintenanceMode = dto.MaintenanceMode;
            entity.MaintenanceMessage = string.IsNullOrWhiteSpace(dto.MaintenanceMessage)
                ? DefaultMaintenanceMessage
                : dto.MaintenanceMessage.Trim();
            entity.TelegramBotToken = ProtectSecret(dto.TelegramBotToken);
            entity.TelegramChatId = string.IsNullOrWhiteSpace(dto.TelegramChatId)
                ? null
                : dto.TelegramChatId.Trim();
            entity.HealthAlertsEnabled = dto.HealthAlertsEnabled;
            entity.ErrorRateThreshold = dto.ErrorRateThreshold < 1 ? DefaultErrorRateThreshold : dto.ErrorRateThreshold;
            entity.UploadsSizeWarningMb = dto.UploadsSizeWarningMb < 1 ? DefaultUploadsSizeWarningMb : dto.UploadsSizeWarningMb;
            entity.BankName = TrimOrNull(dto.BankName);
            entity.BankId = TrimOrNull(dto.BankId);
            entity.BankAccountNumber = TrimOrNull(dto.BankAccountNumber);
            entity.BankAccountName = TrimOrNull(dto.BankAccountName);
            entity.BankQrTemplate = TrimOrNull(dto.BankQrTemplate);
            entity.MoMoPartnerCode = TrimOrNull(dto.MoMoPartnerCode);
            entity.MoMoEndpoint = TrimOrNull(dto.MoMoEndpoint);
            entity.ImageBackupProvider = string.IsNullOrWhiteSpace(dto.ImageBackupProvider)
                ? DefaultImageBackupProvider
                : dto.ImageBackupProvider.Trim();
            entity.ImageBackupSettingsJson = TrimOrNull(dto.ImageBackupSettingsJson);
            entity.CheckInPoints = dto.CheckInPoints < 0 ? DefaultCheckInPoints : dto.CheckInPoints;
            entity.NepPerPortionGrams = dto.NepPerPortionGrams < 1 ? DefaultNepPerPortionGrams : dto.NepPerPortionGrams;
            entity.DailyTargetAmount = Math.Max(0, dto.DailyTargetAmount);
            // Wave 6a (item 22): normalized to "HH:mm"; admin config UI arrives in Wave 6b.
            entity.OpenTime = NormalizeHour(dto.OpenTime, DefaultOpenTime);
            entity.CloseTime = NormalizeHour(dto.CloseTime, DefaultCloseTime);
            // Wave 6b (items 27, 30): floating contact numbers + pause toggle.
            entity.Hotline = TrimOrNull(dto.Hotline);
            entity.ZaloPhone = TrimOrNull(dto.ZaloPhone);
            // Item 101: per-product Zalo chat button toggle.
            entity.ShowZaloChatButton = dto.ShowZaloChatButton;
            entity.GoogleMapsUrl = TrimOrNull(dto.GoogleMapsUrl);
            entity.FacebookPageAccessToken = ProtectSecret(dto.FacebookPageAccessToken);
            entity.FacebookVerifyToken = TrimOrNull(dto.FacebookVerifyToken);
            // Item 138: weekly owner report via Zalo OA (token encrypted at rest).
            entity.ZaloOaAccessToken = ProtectSecret(dto.ZaloOaAccessToken);
            entity.OwnerPhone = TrimOrNull(dto.OwnerPhone);
            entity.OwnerZaloUserId = TrimOrNull(dto.OwnerZaloUserId);
            entity.LastWeeklyReportSentWeek = TrimOrNull(dto.LastWeeklyReportSentWeek);
            entity.AcceptingOrders = dto.AcceptingOrders;
            entity.PausedReason = TrimOrNull(dto.PausedReason);
            // Wave 6d (item 39): 0 = unlimited, otherwise cap at 1000.
            entity.MaxOrdersPerPhonePerDay = Math.Min(MaxOrdersPerPhonePerDayLimit, Math.Max(0, dto.MaxOrdersPerPhonePerDay));
            await _settings.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();

            return MapToDto(entity);
        }

        private static string TrimOrNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Item 180: only absolute http(s) URLs are accepted as webhook
        /// targets; anything else is treated as "not configured".
        /// </summary>
        private static string NormalizeWebhookUrl(string value)
        {
            var trimmed = TrimOrNull(value);
            if (trimmed == null)
            {
                return null;
            }

            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return trimmed.Length > MaxWebhookUrlLength ? trimmed.Substring(0, MaxWebhookUrlLength) : trimmed;
        }

        /// <summary>Clamps a percentage setting to 0–100.</summary>
        private static decimal ClampPercent(decimal value)
        {
            return Math.Min(100, Math.Max(0, value));
        }

        /// <summary>
        /// Item 180: "Gửi thử" — posts a sample payload to the given URL (or
        /// the saved setting when empty) with a 5s timeout. Never throws;
        /// the admin UI shows the returned message.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<(bool Ok, string Message)> TestNewOrderWebhookAsync(string url)
        {
            var target = NormalizeWebhookUrl(url);
            if (target == null)
            {
                var entity = await GetSingleAsync();
                target = NormalizeWebhookUrl(entity?.NewOrderWebhookUrl);
            }

            if (target == null)
            {
                return (false, "Chưa cấu hình URL webhook (phải bắt đầu bằng http:// hoặc https://).");
            }

            var payload = new
            {
                maDon = "TEST-0001",
                sdt = "0901234567",
                tongTien = 75000,
                trangThai = "Pending",
                thoiGian = DateTime.UtcNow
            };

            try
            {
                var ok = await PostNewOrderWebhookAsync(target, payload);
                return ok
                    ? (true, $"Đã gửi thử tới {target} — máy chủ trả về thành công.")
                    : (false, $"Đã gửi tới {target} nhưng máy chủ không trả về thành công. Kiểm tra log WebAPI để biết chi tiết.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Item 180: test webhook POST failed for {Url}.", target);
                return (false, $"Gửi thử thất bại: {ex.GetType().Name} — {ex.Message}");
            }
        }

        /// <summary>
        /// Item 180: posts the new-order payload. Returns true on HTTP
        /// success. Throws on transport errors; callers must catch and log
        /// so a failing endpoint never breaks order creation.
        /// </summary>
        public static async Task<bool> PostNewOrderWebhookAsync(
            System.Net.Http.IHttpClientFactory httpClientFactory, string url, object payload)
        {
            var client = httpClientFactory.CreateClient("PhuThuongStickyRice.Webhook");
            client.Timeout = TimeSpan.FromSeconds(5);

            var body = System.Text.Json.JsonSerializer.Serialize(payload);
            using var request = new System.Net.Http.HttpRequestMessage(
                System.Net.Http.HttpMethod.Post, url)
            {
                Content = new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        private Task<bool> PostNewOrderWebhookAsync(string url, object payload)
        {
            return PostNewOrderWebhookAsync(_httpClientFactory, url, payload);
        }

        /// <summary>
        /// Wave 6a (item 22): normalizes an "HH:mm" open/close time; anything
        /// unparseable falls back to the documented default so the public
        /// display never breaks on a bad admin value.
        /// </summary>
        private static string NormalizeHour(string value, string fallback)
        {
            if (TimeSpan.TryParse(value?.Trim(), out var time)
                && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1))
            {
                return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}";
            }

            return fallback;
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
                BankTransferExpiryMinutes = entity.BankTransferExpiryMinutes < 1 ? DefaultBankTransferExpiryMinutes : entity.BankTransferExpiryMinutes,
                PointsPerVnd = entity.PointsPerVnd <= 0 ? DefaultPointsPerVnd : entity.PointsPerVnd,
                PointsRedeemUnit = entity.PointsRedeemUnit < 1 ? DefaultPointsRedeemUnit : entity.PointsRedeemUnit,
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
                UnconfirmedAlertMinutes = entity.UnconfirmedAlertMinutes < 1 ? DefaultUnconfirmedAlertMinutes : entity.UnconfirmedAlertMinutes,
                RecaptchaSiteKey = entity.RecaptchaSiteKey,
                RecaptchaSecretKey = UnprotectSecret(entity.RecaptchaSecretKey),
                WebBaseUrl = entity.WebBaseUrl,
                MaintenanceMode = entity.MaintenanceMode,
                MaintenanceMessage = string.IsNullOrWhiteSpace(entity.MaintenanceMessage)
                    ? DefaultMaintenanceMessage
                    : entity.MaintenanceMessage,
                TelegramBotToken = UnprotectSecret(entity.TelegramBotToken),
                TelegramChatId = entity.TelegramChatId,
                HealthAlertsEnabled = entity.HealthAlertsEnabled,
                ErrorRateThreshold = entity.ErrorRateThreshold < 1 ? DefaultErrorRateThreshold : entity.ErrorRateThreshold,
                UploadsSizeWarningMb = entity.UploadsSizeWarningMb < 1 ? DefaultUploadsSizeWarningMb : entity.UploadsSizeWarningMb,
                BankName = entity.BankName,
                BankId = entity.BankId,
                BankAccountNumber = entity.BankAccountNumber,
                BankAccountName = entity.BankAccountName,
                BankQrTemplate = entity.BankQrTemplate,
                MoMoPartnerCode = entity.MoMoPartnerCode,
                MoMoEndpoint = entity.MoMoEndpoint,
                ImageBackupProvider = string.IsNullOrWhiteSpace(entity.ImageBackupProvider)
                    ? DefaultImageBackupProvider
                    : entity.ImageBackupProvider,
                ImageBackupSettingsJson = entity.ImageBackupSettingsJson,
                CheckInPoints = entity.CheckInPoints,
                NepPerPortionGrams = entity.NepPerPortionGrams < 1 ? DefaultNepPerPortionGrams : entity.NepPerPortionGrams,
                DailyTargetAmount = entity.DailyTargetAmount,
                OpenTime = NormalizeHour(entity.OpenTime, DefaultOpenTime),
                CloseTime = NormalizeHour(entity.CloseTime, DefaultCloseTime),
                // Wave 6b (items 27, 30). The migration backfills
                // AcceptingOrders = true for pre-existing rows.
                Hotline = entity.Hotline,
                ZaloPhone = entity.ZaloPhone,
                ShowZaloChatButton = entity.ShowZaloChatButton,
                GoogleMapsUrl = entity.GoogleMapsUrl,
                FacebookPageAccessToken = UnprotectSecret(entity.FacebookPageAccessToken),
                FacebookVerifyToken = entity.FacebookVerifyToken,
                // Item 138: weekly owner report via Zalo OA.
                ZaloOaAccessToken = UnprotectSecret(entity.ZaloOaAccessToken),
                OwnerPhone = entity.OwnerPhone,
                OwnerZaloUserId = entity.OwnerZaloUserId,
                LastWeeklyReportSentWeek = entity.LastWeeklyReportSentWeek,
                AcceptingOrders = entity.AcceptingOrders,
                PausedReason = entity.PausedReason,
                // Wave 6d (item 39): backfilled to 10 for pre-existing rows.
                MaxOrdersPerPhonePerDay = entity.MaxOrdersPerPhonePerDay
            };
        }
    }
}
