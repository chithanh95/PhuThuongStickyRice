using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Reports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.BackgroundServer.HostedServices
{
    /// <summary>
    /// Item 138: delivers the weekly owner report every Monday at 08:00
    /// Vietnam time (UTC+7, no DST). Delivery is idempotent via
    /// <c>ShopSetting.LastWeeklyReportSentWeek</c> — a restart inside the
    /// send window won't double-send, and a manual send from the dashboard
    /// earlier the same Monday suppresses the scheduled one.
    ///
    /// When Zalo OA isn't configured, <see cref="IWeeklyReportSender"/>
    /// returns the report text for manual copy instead of pretending to
    /// send (see the send result note).
    /// </summary>
    public class WeeklyReportWorker : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);
        private const int SendHour = 8;
        private const int SendWindowMinutes = 10;

        private readonly IServiceProvider _services;
        private readonly ILogger<WeeklyReportWorker> _logger;

        public WeeklyReportWorker(
            IServiceProvider services,
            ILogger<WeeklyReportWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WeeklyReportWorker started (sends every Monday 08:00 VN).");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await TrySendIfDueAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WeeklyReportWorker failed; will retry on the next check.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("WeeklyReportWorker stopping.");
        }

        private async Task TrySendIfDueAsync()
        {
            var vnNow = VietnamTime.ToVietnamTime(DateTime.UtcNow);
            if (vnNow.DayOfWeek != DayOfWeek.Monday)
            {
                return;
            }

            if (vnNow.Hour != SendHour || vnNow.Minute >= SendWindowMinutes)
            {
                return;
            }

            using var scope = _services.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<IWeeklyReportSender>();
            var result = await sender.SendAsync();

            _logger.LogInformation(
                "Weekly report Monday job finished: sent={Sent}, zaloConfigured={Configured}, note={Note}",
                result.Sent,
                result.ZaloConfigured,
                result.Note);
        }
    }
}
