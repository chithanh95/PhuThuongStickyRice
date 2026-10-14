using PhuThuongStickyRice.Application;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.BackgroundWorkers;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace PhuThuongStickyRice.WebAPI
{
    [DependsOn(
        typeof(StickyRiceApplicationModule),
        typeof(StickyRiceEntityFrameworkCoreModule),
        typeof(AbpAspNetCoreMvcModule),
        typeof(AbpAutofacModule),
        typeof(AbpBackgroundWorkersModule))]
    public class StickyRiceWebApiModule : AbpModule
    {
        /// <summary>
        /// Periodic workers: bank-transfer auto-cancel, birthday vouchers,
        /// daily subscription orders. Compile-verified only; runtime needs
        /// verification on the IIS deployment (no SQL Server in sandbox).
        /// </summary>
        public override async Task OnApplicationInitializationAsync(
            ApplicationInitializationContext context)
        {
            await context.AddBackgroundWorkerAsync<OrderAutoCancelWorker>();
            await context.AddBackgroundWorkerAsync<BirthdayVoucherWorker>();
            await context.AddBackgroundWorkerAsync<SubscriptionOrderWorker>();
        }
    }
}
