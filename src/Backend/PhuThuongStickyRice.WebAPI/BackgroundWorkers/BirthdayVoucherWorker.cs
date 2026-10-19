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
using PhuThuongStickyRice.Application.Common;
using System.Collections.Generic;
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
        // Voucher sinh nhật: prefix mã, % giảm giá, số ngày hiệu lực.
        // Đặt tên hằng số để mỗi con số đều tự giải thích ý nghĩa.
        private const string BirthdayVoucherPrefix = "BDAY";
        private const int BirthdayDiscountPercent = 20;
        private const int BirthdayVoucherExpiryDays = 30;

        public BirthdayVoucherWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            Timer.Period = (int)TimeSpan.FromHours(24).TotalMilliseconds; // chạy mỗi 24 giờ
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

                // bug-hunt 2026-10-19: dùng ngày theo giờ VN thay vì
                // UtcNow.Date. Worker chạy 24h/lần; nếu lần chạy rơi vào
                // 00:00–06:59 giờ VN thì UtcNow.Date lệch 1 ngày → voucher
                // sinh nhật bị tạo trễ/nhầm ngày.
                var today = VietnamTime.Today();
                var celebrants = await GetTodayCelebrantsAsync(accounts, asyncExecuter, today);

                // perf-hunt 2026-10-19: kiểm tra mã BDAY còn dùng được cho
                // TẤT CẢ người sinh nhật trong 1 query (IN clause) thay vì
                // gọi HasUsableCodeAsync từng người (N+1). Điều kiện lọc
                // trong GetPhonesWithUsableCodeAsync giữ nguyên nên kết quả
                // tương đương từng phần tử.
                var phonesWithCode = await discounts.GetPhonesWithUsableCodeAsync(
                    BirthdayVoucherPrefix, celebrants.Select(x => x.Phone));

                var created = await CreateMissingVouchersAsync(discounts, celebrants, phonesWithCode);

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

        /// <summary>
        /// Lấy danh sách tài khoản loyalty có sinh nhật đúng hôm nay (theo giờ VN).
        /// </summary>
        private static async Task<List<LoyaltyAccount>> GetTodayCelebrantsAsync(
            IRepository<LoyaltyAccount, Guid> accounts,
            IAsyncQueryableExecuter asyncExecuter,
            DateTime today)
        {
            var queryable = await accounts.GetQueryableAsync();
            return await asyncExecuter.ToListAsync(
                queryable.Where(x =>
                    x.DateOfBirth.HasValue
                    && x.DateOfBirth.Value.Month == today.Month
                    && x.DateOfBirth.Value.Day == today.Day));
        }

        /// <summary>
        /// Tạo voucher sinh nhật cho những người chưa có mã còn dùng được.
        /// Trả về số voucher đã tạo.
        /// </summary>
        private static async Task<int> CreateMissingVouchersAsync(
            IDiscountAppService discounts,
            List<LoyaltyAccount> celebrants,
            HashSet<string> phonesWithCode)
        {
            var created = 0;
            foreach (var account in celebrants)
            {
                // Skip when a usable birthday voucher already exists.
                if (phonesWithCode.Contains(account.Phone))
                {
                    continue;
                }

                await discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
                {
                    Prefix = BirthdayVoucherPrefix,
                    DiscountPercent = BirthdayDiscountPercent,
                    ExpiresAt = DateTime.UtcNow.AddDays(BirthdayVoucherExpiryDays),
                    OwnerPhone = account.Phone
                });
                created++;
            }

            return created;
        }
    }
}
