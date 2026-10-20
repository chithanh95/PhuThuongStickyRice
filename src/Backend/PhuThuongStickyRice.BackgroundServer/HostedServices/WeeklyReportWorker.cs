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
    public partial class WeeklyReportWorker : BackgroundService
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
            // CA1848: dùng LoggerMessage source-generated thay vì
            // LoggerExtensions.LogInformation — template được parse 1 lần lúc
            // compile, không boxing argument, nhanh hơn khi log bật.
            Log.WorkerStarted(_logger);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await TrySendIfDueAsync();
                }
                catch (Exception ex)
                {
                    Log.WorkerFailed(_logger, ex);
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                // codequality-team 2026-10-20 run-1625 Agent3-BestPractices:
                // chỉ thoát vòng lặp khi host thật sự yêu cầu dừng (cùng pattern
                // SendEmailWorker đã dùng): TaskCanceledException không gắn
                // stoppingToken mà bị nuốt ở đây sẽ dừng worker trong im lặng.
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            Log.WorkerStopping(_logger);
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

            // CA1873: result.Note là property — không đánh giá argument khi
            // log Information đang tắt (tránh format stack/exception vô ích).
            if (_logger.IsEnabled(LogLevel.Information))
            {
                Log.MondayJobFinished(_logger, result.IsSent, result.ZaloConfigured, result.Note);
            }
        }

        /// <summary>
        /// Log message source-generated (CA1848): template parse 1 lần lúc
        /// compile, không boxing argument, và tự kiểm tra IsEnabled trước khi
        /// format — nhanh hơn LoggerExtensions khi log level đang tắt.
        /// </summary>
        private static partial class Log
        {
            [LoggerMessage(EventId = 1, Level = LogLevel.Information,
                Message = "WeeklyReportWorker started (sends every Monday 08:00 VN).")]
            public static partial void WorkerStarted(ILogger logger);

            [LoggerMessage(EventId = 2, Level = LogLevel.Error,
                Message = "WeeklyReportWorker failed; will retry on the next check.")]
            public static partial void WorkerFailed(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 3, Level = LogLevel.Information,
                Message = "WeeklyReportWorker stopping.")]
            public static partial void WorkerStopping(ILogger logger);

            [LoggerMessage(EventId = 4, Level = LogLevel.Information,
                Message = "Weekly report Monday job finished: sent={Sent}, zaloConfigured={Configured}, note={Note}")]
            public static partial void MondayJobFinished(ILogger logger, bool sent, bool configured, string note);
        }
    }
}
