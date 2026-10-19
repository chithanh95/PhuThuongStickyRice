using PhuThuongStickyRice.Application.Shop;

using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using PhuThuongStickyRice.Domain.Repositories;
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

        private readonly ICheckInRepository _checkIns;
        private readonly ILoyaltyAccountRepository _loyaltyAccounts;
        private readonly IShopSettingAppService _shopSettings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public CheckInAppService(
            ICheckInRepository checkIns,
            ILoyaltyAccountRepository loyaltyAccounts,
            IShopSettingAppService shopSettings,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _checkIns = checkIns;
            _loyaltyAccounts = loyaltyAccounts;
            _shopSettings = shopSettings;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Public: khách điểm danh hôm nay để nhận điểm loyalty (mỗi SĐT tối đa
        /// 1 lần/ngày). SĐT tự chuẩn hóa đầu số Việt Nam nên khách nhập kèm
        /// dấu cách/ký tự khác nhau vẫn được nhận diện đúng 1 tài khoản.
        /// </summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <returns>Kết quả: đã điểm danh chưa, số điểm được tặng, streak, tổng điểm hiện tại.</returns>
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

            // Ghi nhận điểm danh nguyên tử (bug-hunt run 15:35): 2 request
            // điểm danh đồng thời cùng SĐT cùng ngày đều qua bước check
            // (existing == null) rồi cùng insert — unique (Phone, Date) chỉ
            // cho 1 bên insert, bên thua cũ nhận DbUpdateException -> 500.
            // TryInsertAsync trả false cho bên thua -> trả kết quả "đã điểm
            // danh" thay vì lỗi, và KHÔNG cộng điểm lần 2.
            var inserted = await _checkIns.TryInsertAsync(new CheckIn
            {
                Phone = normalized,
                Date = today,
                PointsAwarded = points,
                CreatedAt = DateTime.UtcNow
            });
            if (!inserted)
            {
                var current = await FindLoyaltyAsync(normalized);
                return await BuildResultAsync(normalized, today,
                    alreadyCheckedIn: true, pointsAwarded: 0,
                    loyaltyPoints: current?.Points ?? loyalty.Points);
            }

            // Cộng điểm nguyên tử tại DB (bug-hunt run 16:05): bản cũ đọc
            // loyalty.Points vào memory rồi += và UpdateAsync — nếu cùng lúc
            // có luồng khác đổi điểm của SĐT này (tích điểm khi đơn hoàn
            // thành, đổi điểm ở checkout, thưởng giới thiệu) thì 1 bên ghi
            // đè làm mất phần cộng của bên kia. TryIncrementPointsAsync chạy
            // UPDATE ... SET Points = Points + @points ... nên không mất mát;
            // 3 field cộng/ghi giống hệt bản cũ (Points, TotalEarned, UpdatedAt).
            var incremented = await _loyaltyAccounts.TryIncrementPointsAsync(normalized, points);
            if (!incremented)
            {
                throw new DomainException("Không cộng được điểm tích lũy, vui lòng thử lại.");
            }

            return await BuildResultAsync(normalized, today,
                alreadyCheckedIn: false, pointsAwarded: points, loyaltyPoints: loyalty.Points + points);
        }

        /// <summary>
        /// Public: trạng thái điểm danh hiện tại (đã điểm danh hôm nay chưa,
        /// streak bao nhiêu ngày, tổng điểm loyalty). Không tạo tài khoản mới —
        /// chỉ đọc, nên khách chưa từng dùng loyalty cũng tra cứu được.
        /// </summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <returns>Trạng thái điểm danh, streak và tổng điểm hiện tại.</returns>
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
            // Tạo tài khoản nguyên tử (bug-hunt run 15:35): 2 request đồng
            // thời cho SĐT mới đều thấy chưa có tài khoản rồi cùng insert —
            // unique (Phone) chỉ cho 1 bên, bên thua cũ nhận DbUpdateException
            // -> 500. TryInsertAsync trả null cho bên thua -> đọc lại tài
            // khoản bên thắng vừa tạo.
            var inserted = await _loyaltyAccounts.TryInsertAsync(account);
            if (inserted != null)
            {
                return inserted;
            }

            return await FindLoyaltyAsync(phone)
                ?? throw new DomainException("Không tạo được tài khoản tích điểm, vui lòng thử lại.");
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
