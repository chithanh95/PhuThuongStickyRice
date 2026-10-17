using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Wave5.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Wave5
{
    public interface ICheckInAppService : IApplicationService
    {
        /// <summary>Public: check in today for a phone; awards points once per day.</summary>
        Task<CheckInResultDto> CheckInAsync(string phone);

        /// <summary>Public: current streak + whether the phone checked in today.</summary>
        Task<CheckInResultDto> GetStatusAsync(string phone);
    }

    /// <summary>Item 121: daily check-in awarding loyalty points.</summary>
    public class CheckInAppService : ApplicationService, ICheckInAppService
    {
        private readonly IRepository<CheckIn, Guid> _checkIns;
        private readonly IRepository<LoyaltyAccount, Guid> _loyaltyAccounts;
        private readonly IShopSettingAppService _shopSettings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public CheckInAppService(
            IRepository<CheckIn, Guid> checkIns,
            IRepository<LoyaltyAccount, Guid> loyaltyAccounts,
            IShopSettingAppService shopSettings,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _checkIns = checkIns;
            _loyaltyAccounts = loyaltyAccounts;
            _shopSettings = shopSettings;
            _asyncExecuter = asyncExecuter;
        }

        [AllowAnonymous]
        public virtual async Task<CheckInResultDto> CheckInAsync(string phone)
        {
            var normalized = VietnamTime.NormalizePhone(phone);
            if (string.IsNullOrEmpty(normalized))
            {
                throw new DomainException("Số điện thoại không hợp lệ.");
            }

            var today = VietnamTime.Today();
            var existing = await FindAsync(normalized, today);
            var loyalty = await GetOrCreateLoyaltyAsync(normalized);

            if (existing != null)
            {
                return new CheckInResultDto
                {
                    AlreadyCheckedIn = true,
                    PointsAwarded = 0,
                    StreakDays = await GetStreakAsync(normalized, today),
                    LoyaltyPoints = loyalty.Points
                };
            }

            var settings = await _shopSettings.GetAsync();
            var points = settings.CheckInPoints > 0 ? settings.CheckInPoints : 5;

            await _checkIns.InsertAsync(new CheckIn
            {
                Phone = normalized,
                Date = today,
                PointsAwarded = points,
                CreatedAt = DateTime.UtcNow
            }, autoSave: true);

            loyalty.Points += points;
            loyalty.TotalEarned += points;
            loyalty.UpdatedAt = DateTime.UtcNow;
            await _loyaltyAccounts.UpdateAsync(loyalty, autoSave: true);

            return new CheckInResultDto
            {
                AlreadyCheckedIn = false,
                PointsAwarded = points,
                StreakDays = await GetStreakAsync(normalized, today),
                LoyaltyPoints = loyalty.Points
            };
        }

        [AllowAnonymous]
        public virtual async Task<CheckInResultDto> GetStatusAsync(string phone)
        {
            var normalized = VietnamTime.NormalizePhone(phone);
            var today = VietnamTime.Today();
            var loyalty = normalized == null
                ? null
                : await FindLoyaltyAsync(normalized);

            return new CheckInResultDto
            {
                AlreadyCheckedIn = normalized != null && await FindAsync(normalized, today) != null,
                PointsAwarded = 0,
                StreakDays = normalized == null ? 0 : await GetStreakAsync(normalized, today),
                LoyaltyPoints = loyalty?.Points ?? 0
            };
        }

        private async Task<CheckIn> FindAsync(string phone, DateTime date)
        {
            var queryable = await _checkIns.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Phone == phone && x.Date == date));
        }

        private async Task<LoyaltyAccount> FindLoyaltyAsync(string phone)
        {
            var queryable = await _loyaltyAccounts.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(queryable.Where(x => x.Phone == phone));
        }

        private async Task<LoyaltyAccount> GetOrCreateLoyaltyAsync(string phone)
        {
            var account = await FindLoyaltyAsync(phone);
            if (account != null)
            {
                return account;
            }

            account = new LoyaltyAccount
            {
                Phone = phone,
                Points = 0,
                TotalEarned = 0,
                TotalSpent = 0,
                ReferralCode = "PT-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _loyaltyAccounts.InsertAsync(account, autoSave: true);
            return account;
        }

        private async Task<int> GetStreakAsync(string phone, DateTime today)
        {
            var queryable = await _checkIns.GetQueryableAsync();
            var dates = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.Phone == phone && x.Date <= today)
                    .OrderByDescending(x => x.Date)
                    .Select(x => x.Date));

            var streak = 0;
            var expected = today;
            foreach (var date in dates)
            {
                if (date == expected)
                {
                    streak++;
                    expected = expected.AddDays(-1);
                }
                else if (date < expected)
                {
                    break;
                }
            }

            return streak;
        }
    }
}
