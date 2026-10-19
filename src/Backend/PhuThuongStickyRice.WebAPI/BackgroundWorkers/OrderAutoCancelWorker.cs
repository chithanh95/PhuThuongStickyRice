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
        // Quét định kỳ mỗi 5 phút: đủ nhanh để đơn chuyển khoản hết hạn sớm
        // bị hủy kịp thời, nhưng không quét quá dày gây tốn tài nguyên DB.
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

        public OrderAutoCancelWorker(
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
