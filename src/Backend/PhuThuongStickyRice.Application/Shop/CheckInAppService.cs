using PhuThuongStickyRice.Application.Shop;

using PhuThuongStickyRice.Domain;
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
        /// <summary>
        /// Public: khách điểm danh hôm nay để nhận điểm loyalty
        /// (mỗi SĐT tối đa 1 lần/ngày).
        /// </summary>
        /// <param name="phone">SĐT khách điểm danh.</param>
        /// <returns>Kết quả: đã điểm danh chưa, điểm được tặng, streak, tổng điểm.</returns>
        Task<CheckInResultDto> CheckInAsync(string phone);

        /// <summary>
        /// Public: trạng thái điểm danh hiện tại của SĐT (đã điểm danh hôm nay
        /// chưa, streak bao nhiêu ngày, tổng điểm loyalty). Chỉ đọc, không tạo
        /// tài khoản mới.
        /// </summary>
        /// <param name="phone">SĐT cần tra cứu.</param>
        /// <returns>Trạng thái điểm danh, streak và tổng điểm hiện tại.</returns>
        Task<CheckInResultDto> GetStatusAsync(string phone);
    }

    /// <summary>
    /// Điểm danh mỗi ngày nhận điểm loyalty (item 121). Mỗi SĐT tối đa 1 bản
    /// ghi/ngày (unique Phone+Date): điểm danh trùng ngày không cộng điểm lần 2.
    /// Insert điểm danh và cộng điểm đều nguyên tử tại DB để 2 request đồng
    /// thời không cộng trùng; streak đếm theo từng đợt 370 ngày mới nhất.
    /// </summary>
    public class CheckInAppService : ApplicationService, ICheckInAppService
    {
        // [codequality-smell 2026-10-20] Dùng hằng số dùng chung
        // ShopSetting.DefaultCheckInPoints thay vì copy private const = 5
        // (trước đây 3 nơi cùng định nghĩa 5: entity, service này và
        // ShopSettingAppService — đổi giá trị 1 nơi quên 1 nơi sẽ lệch).

        private readonly ICheckInRepository _checkIns;
        private readonly ILoyaltyAccountRepository _loyaltyAccounts;
        private readonly IShopSettingAppService _shopSettings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Khởi tạo dịch vụ điểm danh với các repository và service liên quan.</summary>
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
                return await BuildAlreadyCheckedInResultAsync(normalized, today, loyalty.Points);
            }

            var settings = await _shopSettings.GetAsync();
            var points = settings.CheckInPoints > 0 ? settings.CheckInPoints : ShopSetting.DefaultCheckInPoints;

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
                return await BuildAlreadyCheckedInResultAsync(normalized, today, current?.Points ?? loyalty.Points);
            }

            // Cộng điểm nguyên tử tại DB — tách ra để CheckInAsync gọn
            // dưới 50 dòng (logic giữ nguyên, xem comment chi tiết ở helper).
            await IncrementLoyaltyPointsAsync(normalized, points);

            return await BuildResultAsync(normalized, today,
                alreadyCheckedIn: false, pointsAwarded: points, loyaltyPoints: loyalty.Points + points);
        }

        /// <summary>
        /// Cộng điểm điểm danh vào tài khoản loyalty theo cách nguyên tử tại DB.
        /// bug-hunt run 16:05: bản cũ đọc loyalty.Points vào memory rồi += và
        /// UpdateAsync — nếu cùng lúc có luồng khác đổi điểm của SĐT này (tích
        /// điểm khi đơn hoàn thành, đổi điểm ở checkout, thưởng giới thiệu) thì
        /// 1 bên ghi đè làm mất phần cộng của bên kia. TryIncrementPointsAsync
        /// chạy UPDATE ... SET Points = Points + @points ... nên không mất mát;
        /// 3 field cộng/ghi giống hệt bản cũ (Points, TotalEarned, UpdatedAt).
        /// </summary>
        /// <param name="phone">SĐT đã chuẩn hóa của khách.</param>
        /// <param name="points">Số điểm cần cộng (theo cài đặt quán).</param>
        /// <exception cref="DomainException">Ném khi DB không cộng được điểm.</exception>
        private async Task IncrementLoyaltyPointsAsync(string phone, int points)
        {
            var incremented = await _loyaltyAccounts.TryIncrementPointsAsync(phone, points);
            if (!incremented)
            {
                throw new DomainException("Không cộng được điểm tích lũy, vui lòng thử lại.");
            }
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
            // perf-hunt 2026-10-19: GetStreakAndTodayAsync đã nạp bản ghi hôm
            // nay trong đợt đầu (order desc) nên không cần query FindAsync
            // riêng cho AlreadyCheckedIn — 3 query còn 2 (loyalty + streak).
            var (streakDays, alreadyCheckedIn) = normalized == null
                ? (StreakDays: 0, AlreadyCheckedIn: false)
                : await GetStreakAndTodayAsync(normalized, today);

            return new CheckInResultDto
            {
                AlreadyCheckedIn = alreadyCheckedIn,
                PointsAwarded = 0,
                StreakDays = streakDays,
                LoyaltyPoints = loyalty?.Points ?? 0
            };
        }

        /// <summary>
        /// Dựng kết quả "đã điểm danh hôm nay": không cộng điểm, chỉ trả trạng
        /// thái + điểm hiện tại. Dùng chung cho cả 2 luồng (đã có bản ghi từ
        /// trước / thua race khi insert đồng thời) để không lặp khối dựng DTO.
        /// </summary>
        private Task<CheckInResultDto> BuildAlreadyCheckedInResultAsync(
            string phone, DateTime today, int loyaltyPoints)
            => BuildResultAsync(phone, today,
                alreadyCheckedIn: true, pointsAwarded: 0, loyaltyPoints: loyaltyPoints);

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
                StreakDays = (await GetStreakAndTodayAsync(phone, today)).StreakDays,
                LoyaltyPoints = loyaltyPoints
            };
        }

        /// <summary>
        /// Tìm bản ghi điểm danh của SĐT trong 1 ngày (null nếu chưa điểm danh ngày đó).
        /// </summary>
        private async Task<CheckIn> FindAsync(string phone, DateTime date)
        {
            var queryable = await _checkIns.GetQueryableAsync();

            // [codequality-practices 2026-10-19] Truyền predicate thẳng vào
            // FirstOrDefaultAsync thay vì bọc queryable.Where(...) thừa.
            return await _asyncExecuter.FirstOrDefaultAsync(
                queryable, x => x.Phone == phone && x.Date == date);
        }

        /// <summary>
        /// Tìm tài khoản loyalty theo SĐT đã chuẩn hóa (null nếu chưa có).
        /// </summary>
        private async Task<LoyaltyAccount> FindLoyaltyAsync(string phone)
        {
            var queryable = await _loyaltyAccounts.GetQueryableAsync();

            // [codequality-practices 2026-10-19] Truyền predicate thẳng vào
            // FirstOrDefaultAsync thay vì bọc queryable.Where(...) thừa.
            return await _asyncExecuter.FirstOrDefaultAsync(queryable, x => x.Phone == phone);
        }

        /// <summary>
        /// Lấy tài khoản loyalty của SĐT, tạo mới (0 điểm) nếu chưa có. Insert
        /// nguyên tử: 2 request đồng thời cho SĐT mới thì bên thua đọc lại
        /// tài khoản bên thắng vừa tạo thay vì lỗi trùng unique.
        /// </summary>
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
                // Mã giới thiệu: tiền tố + hậu tố ngẫu nhiên (RNG mật mã, bỏ
                // ký tự dễ nhầm I/O/0/1) — dùng cùng helper ReadableCodeGenerator
                // với LoyaltyAppService.GenerateReferralCodeAsync thay vì cắt
                // GUID lặp lại (trước đây 2 nơi viết y hệt biểu thức này).
                ReferralCode = ReferralCodes.Prefix + ReadableCodeGenerator.GenerateSuffix(ReferralCodes.SuffixLength),
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
        /// Đếm số ngày điểm danh liên tiếp tính đến hôm nay, đồng thời cho
        /// biết đã điểm danh hôm nay chưa (đợt đầu order desc nên dates[0]
        /// chính là bản ghi mới nhất — khớp FindAsync(phone, today)).
        /// Perf-hunt 2026-10-19: nạp theo từng đợt (mỗi đợt tối đa 370 ngày)
        /// thay vì toàn bộ lịch sử điểm danh của SĐT. Vòng lặp dừng ngay khi
        /// gặp ngày đứt quãng nên hầu hết trường hợp chỉ cần đúng 1 query có
        /// Take; kết quả trả về giống hệt bản cũ kể cả streak dài hơn 370 ngày.
        /// </summary>
        private async Task<(int StreakDays, bool AlreadyCheckedIn)> GetStreakAndTodayAsync(
            string phone, DateTime today)
        {
            const int ChunkSize = 370;
            var queryable = await _checkIns.GetQueryableAsync();

            var streak = 0;
            var alreadyCheckedIn = false;
            var expected = today;
            var upperBound = today; // đợt hiện tại chỉ lấy các ngày <= upperBound
            var firstChunk = true;
            while (true)
            {
                var dates = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.Phone == phone && x.Date <= upperBound)
                        .OrderByDescending(x => x.Date)
                        .Select(x => x.Date)
                        .Take(ChunkSize));

                if (firstChunk)
                {
                    // Mỗi SĐT tối đa 1 bản ghi/ngày (unique index Phone+Date)
                    // nên phần tử đầu của đợt mới nhất chính là hôm nay nếu
                    // đã điểm danh — thay cho 1 query FindAsync riêng.
                    alreadyCheckedIn = dates.Count > 0 && dates[0] == today;
                    firstChunk = false;
                }

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

            return (streak, alreadyCheckedIn);
        }
    }
}
