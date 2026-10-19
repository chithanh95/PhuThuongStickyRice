using PhuThuongStickyRice.Application.Shop;

using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Shop
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
        /// <summary>Số điểm tặng mặc định mỗi lần điểm danh khi cài đặt chưa cấu hình.</summary>
        private const int DefaultCheckInPoints = 5;

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
                return await BuildResultAsync(normalized, today,
                    alreadyCheckedIn: true, pointsAwarded: 0, loyaltyPoints: loyalty.Points);
            }

            var settings = await _shopSettings.GetAsync();
            var points = settings.CheckInPoints > 0 ? settings.CheckInPoints : DefaultCheckInPoints;

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

            return await BuildResultAsync(normalized, today,
                alreadyCheckedIn: false, pointsAwarded: points, loyaltyPoints: loyalty.Points);
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

        /// <summary>
        /// Dựng DTO kết quả điểm danh — dùng chung cho cả 2 luồng (đã điểm danh
        /// hôm nay / điểm danh mới) để không lặp khối khởi tạo DTO.
        /// </summary>
        private async Task<CheckInResultDto> BuildResultAsync(
            string phone, DateTime today, bool alreadyCheckedIn, int pointsAwarded, int loyaltyPoints)
        {
            return new CheckInResultDto
            {
                AlreadyCheckedIn = alreadyCheckedIn,
                PointsAwarded = pointsAwarded,
                StreakDays = await GetStreakAsync(phone, today),
                LoyaltyPoints = loyaltyPoints
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

        /// <summary>
        /// Đếm số ngày điểm danh liên tiếp tính đến hôm nay.
        /// Perf-hunt 2026-10-19: nạp theo từng đợt (mỗi đợt tối đa 370 ngày)
        /// thay vì toàn bộ lịch sử điểm danh của SĐT. Vòng lặp dừng ngay khi
        /// gặp ngày đứt quãng nên hầu hết trường hợp chỉ cần đúng 1 query có
        /// Take; kết quả trả về giống hệt bản cũ kể cả streak dài hơn 370 ngày.
        /// </summary>
        private async Task<int> GetStreakAsync(string phone, DateTime today)
        {
            const int ChunkSize = 370;
            var queryable = await _checkIns.GetQueryableAsync();

            var streak = 0;
            var expected = today;
            var upperBound = today; // đợt hiện tại chỉ lấy các ngày <= upperBound
            while (true)
            {
                var dates = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.Phone == phone && x.Date <= upperBound)
                        .OrderByDescending(x => x.Date)
                        .Select(x => x.Date)
                        .Take(ChunkSize));

                var gapFound = false;
                foreach (var date in dates)
                {
                    if (date == expected)
                    {
                        streak++;
                        expected = expected.AddDays(-1);
                    }
                    else if (date < expected)
                    {
                        gapFound = true;
                        break;
                    }
                    // date > expected không xảy ra: mỗi SĐT chỉ có tối đa
                    // 1 bản ghi/ngày và các đợt đi từ mới đến cũ, không chồng lấn.
                }

                if (gapFound || dates.Count < ChunkSize)
                {
                    break; // gặp ngày đứt quãng, hoặc đã hết lịch sử
                }

                // Cả đợt đều liên tiếp nhau: lấy tiếp đợt cũ hơn.
                upperBound = dates[dates.Count - 1].AddDays(-1);
            }

            return streak;
        }
    }
}
