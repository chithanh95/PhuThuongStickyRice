using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Application.Orders;
using System;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PhuThuongStickyRice.WebAPI.BackgroundWorkers
{
    /// <summary>
    /// Every 5 minutes cancels Pending bank-transfer orders that stayed
    /// unpaid past <c>ShopSetting.BankTransferExpiryMinutes</c>.
    /// NOTE: compile-verified only — no SQL Server in this sandbox, so the
    /// runtime behavior still needs verification on the IIS deployment.
    /// </summary>
    public class OrderAutoCancelWorker : AsyncPeriodicBackgroundWorkerBase
    {
        public OrderAutoCancelWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            Timer.Period = 5 * 60 * 1000; // 5 minutes
            Timer.RunOnStart = false;
        }

        protected override async Task DoWorkAsync(
            PeriodicBackgroundWorkerContext workerContext)
        {
            try
            {
                var orderAppService = workerContext.ServiceProvider
                    .GetRequiredService<IOrderAppService>();
                var cancelled = await orderAppService.AutoCancelExpiredBankTransfersAsync();
                if (cancelled > 0)
                {
                    Logger.LogInformation(
                        "OrderAutoCancelWorker: auto-cancelled {Count} expired bank-transfer orders.",
                        cancelled);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OrderAutoCancelWorker failed.");
            }
        }
    }
}
