using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using PhuThuongStickyRice.Application.Reports;

namespace PhuThuongStickyRice.WebAPI.BackgroundWorkers
{
    /// <summary>
    /// Item 85: sends the owner report weekly (Monday 7:00, Vietnam time)
    /// and monthly (1st, 7:00) via the EmailMessage queue.
    /// Sent markers are kept in memory (process lifetime); after a restart
    /// inside the same due window the report could be re-queued once —
    /// acceptable for a low-frequency owner report.
    /// NOTE: compile-verified only — no SQL Server in this sandbox, so the
    /// runtime behavior still needs verification on the IIS deployment.
    /// </summary>
    public class OwnerReportWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private DateTime? _lastWeeklySentUtc;
        private DateTime? _lastMonthlySentUtc;

        public OwnerReportWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            Timer.Period = 30 * 60 * 1000; // every 30 minutes
            Timer.RunOnStart = false;
        }

        protected override async Task DoWorkAsync(
            PeriodicBackgroundWorkerContext workerContext)
        {
            try
            {
                var vnNow = DateTime.UtcNow.AddHours(7);

                if (IsWeeklyDue(vnNow))
                {
                    var result = await SendAsync(workerContext, "weekly");
                    if (result.Sent)
                    {
                        _lastWeeklySentUtc = DateTime.UtcNow;
                    }
                }

                if (IsMonthlyDue(vnNow))
                {
                    var result = await SendAsync(workerContext, "monthly");
                    if (result.Sent)
                    {
                        _lastMonthlySentUtc = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OwnerReportWorker failed.");
            }
        }

        private bool IsWeeklyDue(DateTime vnNow)
        {
            // Monday, at/after 7:00, and not already sent this week.
            if (vnNow.DayOfWeek != DayOfWeek.Monday || vnNow.Hour < 7)
            {
                return false;
            }

            var weekStart = vnNow.Date.AddDays(-(((int)vnNow.DayOfWeek + 6) % 7));
            return !_lastWeeklySentUtc.HasValue
                || _lastWeeklySentUtc.Value.AddHours(7).Date < weekStart;
        }

        private bool IsMonthlyDue(DateTime vnNow)
        {
            // 1st of the month, at/after 7:00, and not already sent this month.
            if (vnNow.Day != 1 || vnNow.Hour < 7)
            {
                return false;
            }

            var monthStart = new DateTime(vnNow.Year, vnNow.Month, 1);
            return !_lastMonthlySentUtc.HasValue
                || _lastMonthlySentUtc.Value.AddHours(7).Date < monthStart;
        }

        private static async Task<Application.Reports.OwnerReportResultDto> SendAsync(
            PeriodicBackgroundWorkerContext workerContext,
            string period)
        {
            var reports = workerContext.ServiceProvider
                .GetRequiredService<IOwnerReportAppService>();
            var result = await reports.SendAsync(period);
            return result;
        }
    }
}
