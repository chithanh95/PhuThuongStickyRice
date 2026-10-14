using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Application.Loyalty;
using System;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Threading;
using PhuThuongStickyRice.Domain.Entities;
using System.Linq;

namespace PhuThuongStickyRice.WebAPI.BackgroundWorkers
{
    /// <summary>
    /// Once a day creates a 20% birthday voucher (single-use, 30-day expiry,
    /// tied to the phone) for every loyalty account whose birthday is today.
    /// NOTE: compile-verified only — runtime needs verification on IIS.
    /// </summary>
    public class BirthdayVoucherWorker : AsyncPeriodicBackgroundWorkerBase
    {
        public BirthdayVoucherWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            Timer.Period = 24 * 60 * 60 * 1000; // 24 hours
            Timer.RunOnStart = false;
        }

        protected override async Task DoWorkAsync(
            PeriodicBackgroundWorkerContext workerContext)
        {
            try
            {
                var provider = workerContext.ServiceProvider;
                var accounts = provider.GetRequiredService<IRepository<LoyaltyAccount, Guid>>();
                var discounts = provider.GetRequiredService<IDiscountAppService>();
                var asyncExecuter = provider.GetRequiredService<IAsyncQueryableExecuter>();

                var today = DateTime.UtcNow.Date;
                var queryable = await accounts.GetQueryableAsync();
                var celebrants = await asyncExecuter.ToListAsync(
                    queryable.Where(x =>
                        x.DateOfBirth.HasValue
                        && x.DateOfBirth.Value.Month == today.Month
                        && x.DateOfBirth.Value.Day == today.Day));

                var created = 0;
                foreach (var account in celebrants)
                {
                    // Skip when a usable birthday voucher already exists.
                    if (await discounts.HasUsableCodeAsync("BDAY", account.Phone))
                    {
                        continue;
                    }

                    await discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
                    {
                        Prefix = "BDAY",
                        DiscountPercent = 20,
                        ExpiresAt = DateTime.UtcNow.AddDays(30),
                        OwnerPhone = account.Phone
                    });
                    created++;
                }

                if (created > 0)
                {
                    Logger.LogInformation(
                        "BirthdayVoucherWorker: created {Count} birthday vouchers.",
                        created);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "BirthdayVoucherWorker failed.");
            }
        }
    }
}
