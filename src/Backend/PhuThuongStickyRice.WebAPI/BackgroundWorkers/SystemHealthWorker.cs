using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Threading;
using PhuThuongStickyRice.WebAPI.Common;

namespace PhuThuongStickyRice.WebAPI.BackgroundWorkers
{
    /// <summary>
    /// Health checks every 5 minutes (item 103): database connectivity,
    /// uploads-disk free space, and the recent error-log rate. On failure
    /// sends a Telegram message (bot token + chat id in ShopSetting, empty
    /// = disabled) and/or queues an <see cref="EmailMessage"/> to the owner
    /// email. Alerts are rate-limited to once per hour per check.
    ///
    /// NOTE: this catches app-level problems only. True external uptime
    /// monitoring (UptimeRobot, Better Stack, ...) is still recommended —
    /// if the whole server is down, this worker is down with it.
    /// Compile-verified only (no SQL Server in this sandbox).
    /// </summary>
    public class SystemHealthWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private const long LowDiskBytes = 500L * 1024 * 1024; // 500 MB
        private static readonly TimeSpan AlertCooldown = TimeSpan.FromHours(1);
        // Chu kỳ kiểm tra sức khỏe hệ thống (DB, ổ đĩa, tỉ lệ lỗi log).
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
        // Hệ số đổi byte → MB khi hiển thị dung lượng còn trống.
        private const long BytesPerMegabyte = 1024L * 1024L;
        // Timeout gửi cảnh báo Telegram: worker chạy nền không được treo lâu
        // chờ Telegram trả lời; 10s là đủ cho 1 request gửi tin nhắn.
        private static readonly TimeSpan TelegramHttpTimeout = TimeSpan.FromSeconds(10);

        private readonly IHttpClientFactory _httpClientFactory;

        private DateTime _lastDbAlert = DateTime.MinValue;
        private DateTime _lastDiskAlert = DateTime.MinValue;
        private DateTime _lastErrorRateAlert = DateTime.MinValue;

        public SystemHealthWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory,
            IHttpClientFactory httpClientFactory)
            : base(timer, serviceScopeFactory)
        {
            _httpClientFactory = httpClientFactory;
            Timer.Period = (int)CheckInterval.TotalMilliseconds;
            Timer.RunOnStart = false;
        }

        protected override async Task DoWorkAsync(
            PeriodicBackgroundWorkerContext workerContext)
        {
            var provider = workerContext.ServiceProvider;

            // [codequality-smell 2026-10-20] Đọc cài đặt tách ra method riêng
            // để DoWorkAsync chỉ còn flow 3 bước kiểm tra (DB → ổ đĩa → tỉ lệ
            // lỗi); giữ nguyên hành vi: đọc settings lỗi thì bỏ qua vòng này.
            var settings = await LoadSettingsSafeAsync(provider);
            if (settings == null)
            {
                return;
            }

            // 1. Database connectivity.
            var dbOk = await CheckDatabaseAsync(provider);
            if (!dbOk)
            {
                _lastDbAlert = await AlertAsync(provider, settings, "db", _lastDbAlert,
                    "🚨 *Xôi Huyền — health alert*\nKhông kết nối được database.");
            }

            // 2. Uploads disk space.
            var freeBytes = GetUploadsDriveFreeBytes(provider);
            if (freeBytes.HasValue && freeBytes.Value < LowDiskBytes)
            {
                _lastDiskAlert = await AlertAsync(provider, settings, "disk", _lastDiskAlert,
                    $"🚨 *Xôi Huyền — health alert*\nỔ đĩa uploads sắp đầy: còn {freeBytes.Value / BytesPerMegabyte} MB trống.");
            }

            // 3. Recent error-log rate.
            var recentErrors = await CountRecentErrorsAsync(provider);
            if (recentErrors > settings.ErrorRateThreshold)
            {
                _lastErrorRateAlert = await AlertAsync(provider, settings, "errors", _lastErrorRateAlert,
                    $"🚨 *Xôi Huyền — health alert*\n{recentErrors} lỗi trong 1 giờ qua (ngưỡng {settings.ErrorRateThreshold}). Xem /admin/nhat-ky-he-thong.");
            }

            if (dbOk && recentErrors <= settings.ErrorRateThreshold)
            {
                Logger.LogDebug("SystemHealthWorker: all checks passed.");
            }
        }

        /// <summary>
        /// Đọc cài đặt cảnh báo từ ShopSetting, rút gọn về DTO nội bộ.
        /// Trả null khi đọc thất bại (DB chưa sẵn sàng...) — worker bỏ qua
        /// vòng kiểm tra này thay vì ném exception, giữ nguyên hành vi cũ
        /// (log lỗi rồi return).
        /// </summary>
        private async Task<ShopSettingDtoSafe> LoadSettingsSafeAsync(IServiceProvider provider)
        {
            try
            {
                var shopSettings = provider.GetRequiredService<IShopSettingAppService>();
                var dto = await shopSettings.GetAsync();
                return new ShopSettingDtoSafe
                {
                    HealthAlertsEnabled = dto.HealthAlertsEnabled,
                    ErrorRateThreshold = dto.ErrorRateThreshold,
                    TelegramBotToken = dto.TelegramBotToken,
                    TelegramChatId = dto.TelegramChatId,
                    OwnerReportEmail = dto.OwnerReportEmail
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SystemHealthWorker: could not read shop settings.");
                return null;
            }
        }

        private async Task<bool> CheckDatabaseAsync(IServiceProvider provider)
        {
            try
            {
                var dbContext = provider.GetRequiredService<StickyRiceDbContext>();
                return await dbContext.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SystemHealthWorker: database check failed.");
                return false;
            }
        }

        private long? GetUploadsDriveFreeBytes(IServiceProvider provider)
        {
            try
            {
                var env = provider.GetService<IWebHostEnvironment>();
                var root = WebRootResolver.Resolve(env);
                var uploads = Path.Combine(root, "uploads");
                Directory.CreateDirectory(uploads);
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(uploads)));
                return drive.AvailableFreeSpace;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SystemHealthWorker: disk check failed.");
                return null;
            }
        }

        private async Task<int> CountRecentErrorsAsync(IServiceProvider provider)
        {
            try
            {
                var logs = provider.GetRequiredService<Volo.Abp.Domain.Repositories.IRepository<AppLog, Guid>>();
                var executer = provider.GetRequiredService<IAsyncQueryableExecuter>();
                var since = DateTime.UtcNow.AddHours(-1);
                var queryable = await logs.GetQueryableAsync();
                // CountAsync trả về int luôn — không cần LongCountAsync rồi
                // ép kiểu về int (đếm 64-bit vô nghĩa cho bảng log).
                return await executer.CountAsync(queryable.Where(x =>
                    x.At >= since && (x.Level == "Error" || x.Level == "Critical")));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SystemHealthWorker: error-rate check failed.");
                return 0;
            }
        }

        /// <summary>
        /// Returns the new "last alert" timestamp (unchanged when the
        /// cooldown suppressed the alert).
        /// </summary>
        private async Task<DateTime> AlertAsync(
            IServiceProvider provider, ShopSettingDtoSafe settings,
            string check, DateTime lastAlert, string message)
        {
            if (DateTime.UtcNow - lastAlert < AlertCooldown)
            {
                return lastAlert;
            }

            lastAlert = DateTime.UtcNow;

            if (!settings.HealthAlertsEnabled)
            {
                Logger.LogWarning("SystemHealthWorker [{Check}]: {Message} (alerts disabled — logged only).", check, message);
                return lastAlert;
            }

            var sent = false;

            // Telegram.
            if (!string.IsNullOrWhiteSpace(settings.TelegramBotToken)
                && !string.IsNullOrWhiteSpace(settings.TelegramChatId))
            {
                sent |= await SendTelegramAsync(settings.TelegramBotToken, settings.TelegramChatId, message);
            }

            // Email via the existing EmailMessage queue.
            sent |= await TryQueueAlertEmailAsync(provider, settings, message);

            if (!sent)
            {
                Logger.LogWarning("SystemHealthWorker [{Check}]: {Message} (no alert channel configured).", check, message);
            }

            return lastAlert;
        }

        /// <summary>
        /// Đưa cảnh báo vào hàng đợi EmailMessage để gửi mail cho chủ quán.
        /// Trả false khi chưa cấu hình email nhận hoặc ghi queue thất bại —
        /// caller chỉ log, không ném, để worker không chết vì lỗi kênh cảnh báo.
        /// </summary>
        private async Task<bool> TryQueueAlertEmailAsync(
            IServiceProvider provider, ShopSettingDtoSafe settings, string message)
        {
            if (string.IsNullOrWhiteSpace(settings.OwnerReportEmail))
            {
                return false;
            }

            try
            {
                var queue = provider.GetRequiredService<IEmailMessageRepository>();
                await queue.AddAsync(new EmailMessage
                {
                    From = "brucethanh@gmail.com",
                    Tos = settings.OwnerReportEmail,
                    Subject = "[Xôi Huyền] Cảnh báo hệ thống",
                    Body = message.Replace("*", string.Empty)
                });
                await queue.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SystemHealthWorker: could not queue alert email.");
                return false;
            }
        }

        private async Task<bool> SendTelegramAsync(string botToken, string chatId, string text)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Telegram");
                client.Timeout = TelegramHttpTimeout;
                var payload = JsonSerializer.Serialize(new
                {
                    chat_id = chatId,
                    text,
                    parse_mode = "Markdown"
                });
                // StringContent là IDisposable: dispose sau khi gửi xong thay
                // vì để GC dọn — giữ nguyên hành vi gửi cảnh báo Telegram.
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(
                    $"https://api.telegram.org/bot{botToken}/sendMessage",
                    content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SystemHealthWorker: Telegram send failed.");
                return false;
            }
        }

        /// <summary>Tiny local copy — avoids pulling the full DTO graph into the worker.</summary>
        private sealed class ShopSettingDtoSafe
        {
            public bool HealthAlertsEnabled { get; set; }
            public int ErrorRateThreshold { get; set; }
            public string TelegramBotToken { get; set; }
            public string TelegramChatId { get; set; }
            public string OwnerReportEmail { get; set; }
        }
    }
}
