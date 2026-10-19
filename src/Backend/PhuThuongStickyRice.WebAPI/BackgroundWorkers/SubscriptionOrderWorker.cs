using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Application.Subscriptions;
using System;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PhuThuongStickyRice.WebAPI.BackgroundWorkers
{
    /// <summary>
    /// Every morning creates a Pending COD order for each active daily
    /// subscription that is due. Runs every 6 hours and relies on the
    /// per-subscription <c>LastOrderDate</c> guard so at most one order per
    /// day is created even if the worker runs more than once.
    /// NOTE: compile-verified only — runtime needs verification on IIS.
    /// </summary>
    public class SubscriptionOrderWorker : AsyncPeriodicBackgroundWorkerBase
    {
        // Mỗi subscription có guard LastOrderDate nên chạy 6 giờ/lần vẫn
        // đảm bảo tối đa 1 đơn/ngày — không cần tick dày hơn.
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

        public SubscriptionOrderWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            Timer.Period = (int)CheckInterval.TotalMilliseconds;
            Timer.RunOnStart = false;
        }

        protected override async Task DoWorkAsync(
            PeriodicBackgroundWorkerContext workerContext)
        {
            try
            {
                var subscriptions = workerContext.ServiceProvider
                    .GetRequiredService<ISubscriptionAppService>();
                var created = await subscriptions.CreateDueOrdersAsync();
                if (created > 0)
                {
                    Logger.LogInformation(
                        "SubscriptionOrderWorker: created {Count} subscription orders.",
                        created);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SubscriptionOrderWorker failed.");
            }
        }
    }
}
