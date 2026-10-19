using PhuThuongStickyRice.Application.Shop.DTOs;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Shop
{
    /// <summary>
    /// Hợp đồng cấu hình toàn quán (1 dòng cấu hình duy nhất dùng chung mọi
    /// màn hình): giá/phí ship/đơn tối thiểu, luật loyalty và hạng thành viên,
    /// giờ mở cửa, giới hạn nhận đơn, thông tin liên hệ và các tích hợp.
    /// Mọi lần sửa đều xóa cache để có hiệu lực ngay; secret tích hợp bị che ở
    /// endpoint public và chỉ admin xem đầy đủ qua endpoint riêng.
    /// </summary>
    public interface IShopSettingAppService : IApplicationService
    {
        /// <summary>Admin: lấy toàn bộ cấu hình shop (kèm secret tích hợp, yêu cầu quyền Admin).</summary>
        Task<ShopSettingDto> GetAsync();

        /// <summary>
        /// Public storefront view of the settings: same as <see cref="GetAsync"/>
        /// but a full-day holiday today automatically switches
        /// <c>AcceptingOrders</c> off with the holiday reason. Used by the
        /// landing/shop/checkout pages and by order creation gating.
        /// </summary>
        Task<ShopSettingDto> GetPublicAsync();

        /// <summary>
        /// Admin: cập nhật cấu hình shop. Xóa cache cài đặt sau khi lưu để mọi
        /// trang checkout/public áp dụng ngay; secret tích hợp mới được mã hóa
        /// DataProtection trước khi lưu và validate độ dài URL webhook.
        /// </summary>
        Task<ShopSettingDto> UpdateAsync(ShopSettingDto dto);

        /// <summary>
        /// Item 180: posts a sample new-order payload to the given URL
        /// (or the saved one when empty). Returns (ok, message) — the
        /// admin UI shows the result; never throws.
        /// </summary>
        Task<(bool Ok, string Message)> TestNewOrderWebhookAsync(string url);
    }

    /// <summary>
    /// Trung tâm cấu hình toàn quán — 1 dòng cấu hình duy nhất dùng chung cho
    /// mọi màn hình: giá/phí ship/đơn tối thiểu, luật loyalty (tích điểm, đổi điểm,
    /// hạng Đồng/Bạc/Vàng), giờ mở cửa và giới hạn nhận đơn, thông tin liên hệ,
    /// và các tích hợp (reCAPTCHA, Telegram, Zalo...).
    /// Setting được cache (mọi trang checkout/public đọc qua cache) và mọi lần
    /// sửa đều xóa cache để có hiệu lực ngay. Secret tích hợp lưu ở DB dạng mã
    /// hóa DPv1: và bị che (redact) ở endpoint public — admin xem đầy đủ qua
    /// endpoint riêng yêu cầu quyền Admin.
    /// </summary>
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
        ///
        /// SECURITY: this is the only settings view exposed to anonymous
        /// callers. Integration secrets (API tokens, backup credentials) are
        /// stripped before serialization — the admin form loads them through
        /// the Admin-only GET api/shop-settings instead.
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<ShopSettingDto> GetPublicAsync()
        {
            // Separate cache entry (keyed by Vietnam date) so the public
            // redacted view never shares an instance with the full
            // "shop-settings" cache used by the admin and internal callers.
            // The date in the key also stops a holiday auto-pause from
            // leaking into the next day via a stale cached flag.
            var today = DateTime.UtcNow.AddHours(VietnamTime.UtcOffsetHours).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var dto = await _catalogCache.GetOrAddAsync($"shop-settings:public:{today}", async () =>
            {
                var entity = await GetSingleAsync();
                var publicDto = MapToDto(entity ?? ShopSetting.CreateDefault());
                RedactSecrets(publicDto);
                return publicDto;
            });

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

        /// <summary>
        /// Strips integration secrets from a settings DTO before it leaves
        /// the server on an anonymous endpoint. Anonymous callers never
        /// need API tokens or credentials; the admin form loads them
        /// through the Admin-only GET api/shop-settings.
        /// </summary>
        private static void RedactSecrets(ShopSettingDto dto)
        {
            dto.RecaptchaSecretKey = null;
            dto.TelegramBotToken = null;
            dto.FacebookPageAccessToken = null;
            dto.FacebookVerifyToken = null;
            dto.FacebookAppSecret = null;
            dto.ZaloOaAccessToken = null;
            dto.ImageBackupSettingsJson = null;
        }

        /// <summary>Vietnam calendar date (UTC+7, no DST).</summary>
        private async Task<string> GetTodayHolidayReasonAsync()
        {
            try
            {
                var today = DateTime.UtcNow.AddHours(VietnamTime.UtcOffsetHours).Date;
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

        /// <summary>
        /// Admin (cần quyền "settings.edit"): lưu toàn bộ cấu hình quán. Chưa có
        /// dòng cấu hình thì tạo mới từ giá trị mặc định; số âm/0 không hợp lệ ở
        /// các trường tiền/phí thì bị kẹp về 0 hoặc default ở từng nhóm
        /// (Apply*Settings) thay vì báo lỗi — để admin không bị kẹt form.
        /// Sau khi lưu thì xóa cache cấu hình để shop/admin thấy ngay.
        /// Secret tích hợp gửi lên được mã hóa trước khi lưu (không lưu plain text).
        /// </summary>
        /// <param name="dto">Toàn bộ cấu hình từ form admin.</param>
        /// <returns>Cấu hình sau khi lưu.</returns>
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

            ApplyPricingSettings(entity, dto);
            ApplyLoyaltySettings(entity, dto);
            ApplyOrderingSettings(entity, dto);
            ApplyContactSettings(entity, dto);
            ApplyIntegrationSettings(entity, dto);

            await _settings.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();

            return MapToDto(entity);
        }

        /// <summary>Giá cả, phí ship, đơn tối thiểu, thời gian giao dự kiến.</summary>
        private void ApplyPricingSettings(ShopSetting entity, ShopSettingDto dto)
        {
            entity.DeliveryFee = Math.Max(0, dto.DeliveryFee);
            entity.FreeShipThreshold = Math.Max(0, dto.FreeShipThreshold);
            entity.MinOrderAmount = Math.Max(0, dto.MinOrderAmount);
            entity.EstimatedDeliveryMinutes = dto.EstimatedDeliveryMinutes < 0 ? DefaultEstimatedDeliveryMinutes : dto.EstimatedDeliveryMinutes;
            entity.BankTransferExpiryMinutes = dto.BankTransferExpiryMinutes < 1 ? DefaultBankTransferExpiryMinutes : dto.BankTransferExpiryMinutes;
            entity.PointsRedeemValue = Math.Max(0, dto.PointsRedeemValue);
            entity.DepositThreshold = Math.Max(0, dto.DepositThreshold);
            entity.DepositPercent = ClampPercent(dto.DepositPercent);
            entity.GiftFee = Math.Max(0, dto.GiftFee);
        }

        /// <summary>Điểm thưởng, hạng thành viên (Đồng/Bạc/Vàng).</summary>
        private void ApplyLoyaltySettings(ShopSetting entity, ShopSettingDto dto)
        {
            entity.PointsPerVnd = dto.PointsPerVnd <= 0 ? DefaultPointsPerVnd : dto.PointsPerVnd;
            entity.PointsRedeemUnit = dto.PointsRedeemUnit < 1 ? DefaultPointsRedeemUnit : dto.PointsRedeemUnit;
            entity.ReferralBonusPoints = Math.Max(0, dto.ReferralBonusPoints);
            entity.TierSilverMin = Math.Max(0, dto.TierSilverMin);
            entity.TierSilverDiscount = ClampPercent(dto.TierSilverDiscount);
            entity.TierGoldMin = Math.Max(0, dto.TierGoldMin);
            entity.TierGoldDiscount = ClampPercent(dto.TierGoldDiscount);
        }

        /// <summary>Giờ mở cửa, nhận đơn, giới hạn đơn theo SĐT/ngày.</summary>
        private void ApplyOrderingSettings(ShopSetting entity, ShopSettingDto dto)
        {
            entity.CheckInPoints = dto.CheckInPoints < 0 ? DefaultCheckInPoints : dto.CheckInPoints;
            entity.NepPerPortionGrams = dto.NepPerPortionGrams < 1 ? DefaultNepPerPortionGrams : dto.NepPerPortionGrams;
            entity.DailyTargetAmount = Math.Max(0, dto.DailyTargetAmount);
            // Wave 6a (item 22): normalized to "HH:mm"; admin config UI arrives in Wave 6b.
            entity.OpenTime = NormalizeHour(dto.OpenTime, DefaultOpenTime);
            entity.CloseTime = NormalizeHour(dto.CloseTime, DefaultCloseTime);
            // Item 101: per-product Zalo chat button toggle.
            entity.ShowZaloChatButton = dto.ShowZaloChatButton;
            entity.AcceptingOrders = dto.AcceptingOrders;
            entity.PausedReason = TrimOrNull(dto.PausedReason);
            // Wave 6d (item 39): 0 = unlimited, otherwise cap at 1000.
            entity.MaxOrdersPerPhonePerDay = Math.Min(MaxOrdersPerPhonePerDayLimit, Math.Max(0, dto.MaxOrdersPerPhonePerDay));
        }

        /// <summary>Hotline, Zalo, bản đồ, email/SĐT nhận báo cáo.</summary>
        private void ApplyContactSettings(ShopSetting entity, ShopSettingDto dto)
        {
            entity.OwnerReportEmail = string.IsNullOrWhiteSpace(dto.OwnerReportEmail)
                ? null
                : dto.OwnerReportEmail.Trim();
            // Wave 6b (items 27, 30): floating contact numbers + pause toggle.
            entity.Hotline = TrimOrNull(dto.Hotline);
            entity.ZaloPhone = TrimOrNull(dto.ZaloPhone);
            entity.GoogleMapsUrl = TrimOrNull(dto.GoogleMapsUrl);
            // Item 138: weekly owner report via Zalo OA (token encrypted at rest).
            entity.OwnerPhone = TrimOrNull(dto.OwnerPhone);
            entity.OwnerZaloUserId = TrimOrNull(dto.OwnerZaloUserId);
            entity.LastWeeklyReportSentWeek = TrimOrNull(dto.LastWeeklyReportSentWeek);
        }

        /// <summary>reCAPTCHA, Telegram, ngân hàng, MoMo, Facebook, Zalo OA, cảnh báo hệ thống.</summary>
        private void ApplyIntegrationSettings(ShopSetting entity, ShopSettingDto dto)
        {
            ApplySiteIntegrationSettings(entity, dto);
            ApplyMonitoringSettings(entity, dto);
            ApplyBankTransferSettings(entity, dto);
            ApplyMessagingIntegrationSettings(entity, dto);
            ApplyImageBackupSettings(entity, dto);
        }

        /// <summary>
        /// Tích hợp phía website: reCAPTCHA chống bot ở checkout, URL gốc của web
        /// và chế độ bảo trì. Thứ tự gán giữ nguyên như bản cũ.
        /// </summary>
        private void ApplySiteIntegrationSettings(ShopSetting entity, ShopSettingDto dto)
        {
            entity.RecaptchaSiteKey = TrimOrNull(dto.RecaptchaSiteKey);
            entity.RecaptchaSecretKey = ProtectSecret(dto.RecaptchaSecretKey);
            entity.WebBaseUrl = TrimOrNull(dto.WebBaseUrl)?.TrimEnd('/');
            entity.MaintenanceMode = dto.MaintenanceMode;
            entity.MaintenanceMessage = string.IsNullOrWhiteSpace(dto.MaintenanceMessage)
                ? DefaultMaintenanceMessage
                : dto.MaintenanceMessage.Trim();
        }

        /// <summary>
        /// Cài đặt giám sát vận hành: nhắc đơn chưa xác nhận, cảnh báo sức khỏe
        /// hệ thống (tỷ lệ lỗi, dung lượng upload). Giá trị &lt; 1 rơi về mặc định.
        /// </summary>
        private void ApplyMonitoringSettings(ShopSetting entity, ShopSettingDto dto)
        {
            entity.UnconfirmedAlertMinutes = dto.UnconfirmedAlertMinutes < 1 ? DefaultUnconfirmedAlertMinutes : dto.UnconfirmedAlertMinutes;
            entity.HealthAlertsEnabled = dto.HealthAlertsEnabled;
            entity.ErrorRateThreshold = dto.ErrorRateThreshold < 1 ? DefaultErrorRateThreshold : dto.ErrorRateThreshold;
            entity.UploadsSizeWarningMb = dto.UploadsSizeWarningMb < 1 ? DefaultUploadsSizeWarningMb : dto.UploadsSizeWarningMb;
        }

        /// <summary>
        /// Thông tin chuyển khoản ngân hàng (hiển thị QR cho khách) và MoMo.
        /// </summary>
        private void ApplyBankTransferSettings(ShopSetting entity, ShopSettingDto dto)
        {
            entity.BankName = TrimOrNull(dto.BankName);
            entity.BankId = TrimOrNull(dto.BankId);
            entity.BankAccountNumber = TrimOrNull(dto.BankAccountNumber);
            entity.BankAccountName = TrimOrNull(dto.BankAccountName);
            entity.BankQrTemplate = TrimOrNull(dto.BankQrTemplate);
            entity.MoMoPartnerCode = TrimOrNull(dto.MoMoPartnerCode);
            entity.MoMoEndpoint = TrimOrNull(dto.MoMoEndpoint);
        }

        /// <summary>
        /// Kênh nhắn tin: Telegram (báo đơn), Facebook Page và Zalo OA
        /// (gửi e-receipt, báo cáo tuần). Secret được mã hóa trước khi lưu.
        /// </summary>
        private void ApplyMessagingIntegrationSettings(ShopSetting entity, ShopSettingDto dto)
        {
            entity.TelegramBotToken = ProtectSecret(dto.TelegramBotToken);
            entity.TelegramChatId = TrimOrNull(dto.TelegramChatId);
            entity.FacebookPageAccessToken = ProtectSecret(dto.FacebookPageAccessToken);
            entity.FacebookVerifyToken = TrimOrNull(dto.FacebookVerifyToken);
            entity.FacebookAppSecret = ProtectSecret(dto.FacebookAppSecret);
            entity.ZaloOaAccessToken = ProtectSecret(dto.ZaloOaAccessToken);
        }

        /// <summary>Cấu hình sao lưu ảnh sản phẩm sang nhà cung cấp ngoài.</summary>
        private void ApplyImageBackupSettings(ShopSetting entity, ShopSettingDto dto)
        {
            entity.ImageBackupProvider = string.IsNullOrWhiteSpace(dto.ImageBackupProvider)
                ? DefaultImageBackupProvider
                : dto.ImageBackupProvider.Trim();
            entity.ImageBackupSettingsJson = TrimOrNull(dto.ImageBackupSettingsJson);
        }

        /// <summary>
        /// Chuẩn hóa chuỗi setting: null/rỗng/toàn khoảng trắng → null,
        /// ngược lại trim. Tránh lưu chuỗi rác vào DB rồi phải xử lý ở nơi đọc.
        /// </summary>
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

            // Dispose: response chỉ dùng để đọc IsSuccessStatusCode, không giữ lại.
            using var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gửi webhook "có đơn mới" (item 180). Lỗi HTTP (4xx/5xx) trả về
        /// false; lỗi transport (timeout, DNS) thì THROW — nơi gọi phải
        /// catch và log (webhook là thông báo phụ, không được làm hỏng
        /// luồng tạo đơn).
        /// </summary>
        /// <param name="url">URL webhook tuyệt đối (http/https).</param>
        /// <param name="payload">Dữ liệu đơn serialize thành JSON.</param>
        /// <returns>true nếu endpoint trả về HTTP thành công.</returns>
        private Task<bool> PostNewOrderWebhookAsync(string url, object payload)
        {
            return PostNewOrderWebhookAsync(_httpClientFactory, url, payload);
        }

        /// <summary>
        /// Số nguyên: dùng fallback khi thiếu/sai (<= 0). Các cột thêm sau
        /// qua migration có giá trị 0 ở các row cũ nên phải fallback lúc đọc.
        /// </summary>
        private static int FallbackWhenNonPositive(int value, int fallback) =>
            value < 1 ? fallback : value;

        /// <summary>Bản decimal cho các cột tiền/số lượng dùng kiểu decimal.</summary>
        private static decimal FallbackWhenNonPositive(decimal value, decimal fallback) =>
            value < 1 ? fallback : value;

        /// <summary>Chuỗi: dùng fallback khi null/rỗng/toàn khoảng trắng.</summary>
        private static string FallbackWhenBlank(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;

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
                return _protector.Unprotect(stored[EncryptedPrefix.Length..]);
            }
            catch
            {
                // Key ring unavailable (e.g. fresh deploy without the shared
                // key store) — never crash reads; the admin can re-save.
                Logger.LogWarning("Could not decrypt a stored shop secret; returning null.");
                return null;
            }
        }

        /// <summary>
        /// Lấy bản ghi setting duy nhất của shop. Thiết kế 1 row/table nên
        /// lấy row đầu tiên, không có thì trả null để nơi gọi dùng default.
        /// </summary>
        private async Task<ShopSetting> GetSingleAsync()
        {
            var queryable = await _settings.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(queryable);
        }

        /// <summary>
        /// Map entity sang DTO, đồng thời áp fallback cho các cột số:
        /// cột thêm sau qua migration có giá trị 0 ở row cũ nên phải
        /// fallback về default lúc đọc (xem comment trong thân hàm).
        /// </summary>
        private ShopSettingDto MapToDto(ShopSetting entity)
        {
            // Rows created before Wave 2 have 0 in the new columns; fall back
            // to the documented defaults on read so old rows keep working.
            var dto = new ShopSettingDto();
            MapOrderAndFeeSettings(dto, entity);
            MapLoyaltyAndTierSettings(dto, entity);
            MapNotificationAndSecuritySettings(dto, entity);
            MapBankAndStorefrontSettings(dto, entity);
            return dto;
        }

        /// <summary>Nhóm cài đặt đơn hàng/phí ship/giờ mở cửa — kiểm soát việc nhận đơn.</summary>
        private void MapOrderAndFeeSettings(ShopSettingDto dto, ShopSetting entity)
        {
            dto.DeliveryFee = entity.DeliveryFee;
            dto.FreeShipThreshold = entity.FreeShipThreshold;
            dto.MinOrderAmount = entity.MinOrderAmount;
            dto.EstimatedDeliveryMinutes = entity.EstimatedDeliveryMinutes;
            dto.BankTransferExpiryMinutes = FallbackWhenNonPositive(entity.BankTransferExpiryMinutes, DefaultBankTransferExpiryMinutes);
            dto.UnconfirmedAlertMinutes = FallbackWhenNonPositive(entity.UnconfirmedAlertMinutes, DefaultUnconfirmedAlertMinutes);
            dto.OpenTime = NormalizeHour(entity.OpenTime, DefaultOpenTime);
            dto.CloseTime = NormalizeHour(entity.CloseTime, DefaultCloseTime);
            dto.AcceptingOrders = entity.AcceptingOrders;
            dto.PausedReason = entity.PausedReason;
            // Wave 6d (item 39): backfilled to 10 for pre-existing rows.
            dto.MaxOrdersPerPhonePerDay = entity.MaxOrdersPerPhonePerDay;
        }

        /// <summary>Nhóm cài đặt tích điểm/khuyến mãi/hạng thành viên — dùng ở checkout và ví.</summary>
        private void MapLoyaltyAndTierSettings(ShopSettingDto dto, ShopSetting entity)
        {
            dto.PointsPerVnd = FallbackWhenNonPositive(entity.PointsPerVnd, DefaultPointsPerVnd);
            dto.PointsRedeemUnit = FallbackWhenNonPositive(entity.PointsRedeemUnit, DefaultPointsRedeemUnit);
            dto.PointsRedeemValue = entity.PointsRedeemValue;
            dto.ReferralBonusPoints = entity.ReferralBonusPoints;
            dto.DepositThreshold = entity.DepositThreshold;
            dto.DepositPercent = entity.DepositPercent;
            dto.GiftFee = entity.GiftFee;
            dto.TierSilverMin = entity.TierSilverMin;
            dto.TierSilverDiscount = entity.TierSilverDiscount;
            dto.TierGoldMin = entity.TierGoldMin;
            dto.TierGoldDiscount = entity.TierGoldDiscount;
            dto.CheckInPoints = entity.CheckInPoints;
            dto.NepPerPortionGrams = FallbackWhenNonPositive(entity.NepPerPortionGrams, DefaultNepPerPortionGrams);
            dto.DailyTargetAmount = entity.DailyTargetAmount;
            dto.OwnerReportEmail = entity.OwnerReportEmail;
        }

        /// <summary>Nhóm thông báo/tích hợp ngoài (Telegram, Facebook, Zalo OA, reCAPTCHA) — secret được giải mã khi đọc.</summary>
        private void MapNotificationAndSecuritySettings(ShopSettingDto dto, ShopSetting entity)
        {
            dto.RecaptchaSiteKey = entity.RecaptchaSiteKey;
            dto.RecaptchaSecretKey = UnprotectSecret(entity.RecaptchaSecretKey);
            dto.WebBaseUrl = entity.WebBaseUrl;
            dto.MaintenanceMode = entity.MaintenanceMode;
            dto.MaintenanceMessage = FallbackWhenBlank(entity.MaintenanceMessage, DefaultMaintenanceMessage);
            dto.TelegramBotToken = UnprotectSecret(entity.TelegramBotToken);
            dto.TelegramChatId = entity.TelegramChatId;
            dto.HealthAlertsEnabled = entity.HealthAlertsEnabled;
            dto.ErrorRateThreshold = FallbackWhenNonPositive(entity.ErrorRateThreshold, DefaultErrorRateThreshold);
            dto.UploadsSizeWarningMb = FallbackWhenNonPositive(entity.UploadsSizeWarningMb, DefaultUploadsSizeWarningMb);
            dto.ImageBackupProvider = FallbackWhenBlank(entity.ImageBackupProvider, DefaultImageBackupProvider);
            dto.ImageBackupSettingsJson = entity.ImageBackupSettingsJson;
            dto.FacebookPageAccessToken = UnprotectSecret(entity.FacebookPageAccessToken);
            dto.FacebookVerifyToken = entity.FacebookVerifyToken;
            dto.FacebookAppSecret = UnprotectSecret(entity.FacebookAppSecret);
            // Item 138: weekly owner report via Zalo OA.
            dto.ZaloOaAccessToken = UnprotectSecret(entity.ZaloOaAccessToken);
            dto.OwnerPhone = entity.OwnerPhone;
            dto.OwnerZaloUserId = entity.OwnerZaloUserId;
            dto.LastWeeklyReportSentWeek = entity.LastWeeklyReportSentWeek;
        }

        /// <summary>Nhóm ngân hàng và hiển thị storefront công khai (liên hệ, link food-app).</summary>
        private void MapBankAndStorefrontSettings(ShopSettingDto dto, ShopSetting entity)
        {
            dto.BankName = entity.BankName;
            dto.BankId = entity.BankId;
            dto.BankAccountNumber = entity.BankAccountNumber;
            dto.BankAccountName = entity.BankAccountName;
            dto.BankQrTemplate = entity.BankQrTemplate;
            dto.MoMoPartnerCode = entity.MoMoPartnerCode;
            dto.MoMoEndpoint = entity.MoMoEndpoint;
            // Wave 6b (items 27, 30). The migration backfills
            // AcceptingOrders = true for pre-existing rows.
            dto.Hotline = entity.Hotline;
            dto.ZaloPhone = entity.ZaloPhone;
            dto.ShowZaloChatButton = entity.ShowZaloChatButton;
            dto.GoogleMapsUrl = entity.GoogleMapsUrl;
            // Perf (items 55/58/75): public storefront display fields — read
            // by the landing controller through cached GetPublicAsync()
            // instead of a direct ShopSettings query per request.
            dto.ZaloVipGroupUrl = entity.ZaloVipGroupUrl;
            dto.WholesaleContact = entity.WholesaleContact;
            dto.WholesaleMinQty = entity.WholesaleMinQty;
            dto.ShopeeFoodUrl = entity.ShopeeFoodUrl;
            dto.GrabFoodUrl = entity.GrabFoodUrl;
        }
    }
}
