using PhuThuongStickyRice.Application.Loyalty.DTOs;
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Shop.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Loyalty
{
    public interface ILoyaltyAppService : IApplicationService
    {
        /// <summary>Public: loyalty summary for a phone number.</summary>
        Task<LoyaltyAccountDto> GetByPhoneAsync(string phone);

        /// <summary>Public: customer sets their own birthday (track-order "hồ sơ").</summary>
        Task<LoyaltyAccountDto> UpdateBirthdayAsync(string phone, UpdateBirthdayInput input);

        /// <summary>Admin: paged list, optional search by phone.</summary>
        Task<List<LoyaltyAccountDto>> GetListAsync(string searchTerm);

        /// <summary>Admin: manually adjust points.</summary>
        Task<LoyaltyAccountDto> AdjustAsync(string phone, AdjustPointsInput input);

        /// <summary>Admin: accounts with a birthday in the next N days.</summary>
        Task<List<LoyaltyAccountDto>> GetUpcomingBirthdaysAsync(int daysAhead);

        /// <summary>
        /// Internal: get or create the account for a phone. Used by checkout,
        /// the completion hook and background workers.
        /// </summary>
        Task<LoyaltyAccount> GetOrCreateAsync(string phone);

        /// <summary>
        /// Internal: find an account by its referral code (case-insensitive).
        /// </summary>
        Task<LoyaltyAccount> FindByReferralCodeAsync(string referralCode);

        /// <summary>
        /// Internal: called once when an order reaches Completed — earns
        /// points, updates lifetime spend and awards referral bonuses on the
        /// buyer's first completed order.
        /// </summary>
        Task AwardCompletedOrderAsync(Order order);

        /// <summary>
        /// Internal: deducts points for a redemption. Returns (points, vndOff).
        /// </summary>
        Task<(int Points, decimal VndOff)> RedeemAsync(string phone, int pointsRequested, ShopSettingDto settings);

        /// <summary>Internal: refunds redeemed points when an order is cancelled.</summary>
        Task RefundRedeemedPointsAsync(Order order);

        /// <summary>Tier from lifetime spend: (TierName, DiscountPercent).</summary>
        (string Tier, decimal DiscountPercent) GetTier(decimal totalSpent, ShopSettingDto settings);

        string NormalizePhone(string phone);
    }

    /// <summary>
    /// Quản lý điểm thành viên theo SĐT: tích điểm khi đơn hoàn thành, đổi điểm
    /// trừ tiền, hoàn điểm khi hủy đơn, thưởng giới thiệu (cả 2 bên), xếp hạng
    /// Đồng/Bạc/Vàng theo tổng chi tiêu. Điểm không bao giờ âm.
    /// </summary>
    public class LoyaltyAppService : ApplicationService, ILoyaltyAppService
    {
        private readonly IRepository<LoyaltyAccount, Guid> _accounts;
        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IShopSettingAppService _shopSettings;

        public LoyaltyAppService(
            IRepository<LoyaltyAccount, Guid> accounts,
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter,
            IShopSettingAppService shopSettings)
        {
            _accounts = accounts;
            _orders = orders;
            _asyncExecuter = asyncExecuter;
            _shopSettings = shopSettings;
        }

        [AllowAnonymous]
        public virtual async Task<LoyaltyAccountDto> GetByPhoneAsync(string phone)
        {
            var normalized = NormalizePhone(phone);
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            var account = await FindByPhoneAsync(normalized);
            if (account == null)
            {
                return null;
            }

            return await MapToDtoAsync(account);
        }

        [AllowAnonymous]
        public virtual async Task<LoyaltyAccountDto> UpdateBirthdayAsync(string phone, UpdateBirthdayInput input)
        {
            var account = await GetOrCreateAsync(phone);
            // Chỉ lấy phần ngày (bỏ giờ) để so sánh sinh nhật hàng năm chính xác.
            account.DateOfBirth = input?.DateOfBirth?.Date;
            account.UpdatedAt = DateTime.UtcNow;
            await _accounts.UpdateAsync(account, autoSave: true);
            return await MapToDtoAsync(account);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<LoyaltyAccountDto>> GetListAsync(string searchTerm)
        {
            var queryable = await _accounts.GetQueryableAsync();
            IQueryable<LoyaltyAccount> query = queryable.OrderByDescending(x => x.TotalSpent);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = new string(searchTerm.Where(char.IsDigit).ToArray());
                query = query.Where(x => x.Phone.Contains(term));
            }

            var items = await _asyncExecuter.ToListAsync(query.Take(200));
            var settings = await _shopSettings.GetAsync();
            return items.Select(x => MapToDto(x, settings)).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<LoyaltyAccountDto> AdjustAsync(string phone, AdjustPointsInput input)
        {
            var account = await GetOrCreateAsync(phone);
            // Chặn điểm âm: đơn đã đổi điểm rồi hủy thì RefundRedeemedPoints
            // hoàn sau, nên trừ ở đây không được cho về âm.
            var next = account.Points + input.PointsDelta;
            if (next < 0)
            {
                throw new Domain.Exceptions.DomainException("Điểm không thể âm.");
            }

            account.Points = next;
            if (input.PointsDelta > 0)
            {
                // TotalEarned là lịch sử "đã kiếm" nên chỉ tăng khi cộng điểm
                // (kể cả admin cộng tay); trừ điểm không trừ lịch sử này.
                account.TotalEarned += input.PointsDelta;
            }

            account.UpdatedAt = DateTime.UtcNow;
            await _accounts.UpdateAsync(account, autoSave: true);
            return await MapToDtoAsync(account);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<LoyaltyAccountDto>> GetUpcomingBirthdaysAsync(int daysAhead)
        {
            if (daysAhead < 0)
            {
                daysAhead = 0;
            }

            if (daysAhead > 60)
            {
                daysAhead = 60;
            }

            // Month/day comparison runs in memory — the table is small and
            // this keeps the query provider-agnostic.
            var queryable = await _accounts.GetQueryableAsync();
            var withBirthday = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.DateOfBirth.HasValue));

            var today = DateTime.UtcNow.Date;
            var upcoming = withBirthday
                .Select(x => new
                {
                    Account = x,
                    Next = NextBirthday(x.DateOfBirth.Value, today)
                })
                .Where(x => (x.Next - today).TotalDays <= daysAhead)
                .OrderBy(x => x.Next)
                .Select(x => x.Account)
                .ToList();

            var settings = await _shopSettings.GetAsync();
            return upcoming.Select(x => MapToDto(x, settings)).ToList();
        }

        public virtual async Task<LoyaltyAccount> GetOrCreateAsync(string phone)
        {
            var normalized = NormalizePhone(phone);
            if (string.IsNullOrEmpty(normalized))
            {
                throw new Domain.Exceptions.DomainException("Số điện thoại không hợp lệ.");
            }

            var account = await FindByPhoneAsync(normalized);
            if (account != null)
            {
                return account;
            }

            account = new LoyaltyAccount
            {
                Phone = normalized,
                Points = 0,
                TotalEarned = 0,
                TotalSpent = 0,
                ReferralCode = await GenerateReferralCodeAsync(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _accounts.InsertAsync(account, autoSave: true);
            return account;
        }

        public virtual async Task<LoyaltyAccount> FindByReferralCodeAsync(string referralCode)
        {
            if (string.IsNullOrWhiteSpace(referralCode))
            {
                return null;
            }

            var code = referralCode.Trim().ToUpperInvariant();
            var queryable = await _accounts.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.ReferralCode == code));
        }

        public virtual async Task AwardCompletedOrderAsync(Order order)
        {
            var settings = await _shopSettings.GetAsync();
            var buyer = await GetOrCreateAsync(order.CustomerPhone);

            // 1 điểm cho mỗi PointsPerVnd chi tiêu (mặc định 10.000₫), làm tròn
            // xuống — đơn nhỏ hơn mốc thì không được điểm nào.
            var perVnd = settings.PointsPerVnd > 0 ? settings.PointsPerVnd : 10000;
            var earned = (int)Math.Floor(order.TotalAmount.Amount / perVnd);
            if (earned > 0)
            {
                buyer.Points += earned;
                buyer.TotalEarned += earned;
            }

            buyer.TotalSpent += order.TotalAmount.Amount;
            buyer.UpdatedAt = DateTime.UtcNow;

            // Thưởng giới thiệu: chỉ cho đơn hoàn thành ĐẦU TIÊN có mã giới thiệu
            // hợp lệ — chống tự giới thiệu nhiều lần ăn bonus; người giới thiệu
            // và người được giới thiệu không được là cùng 1 tài khoản.
            if (!string.IsNullOrWhiteSpace(order.ReferralCodeUsed))
            {
                var orderQueryable = await _orders.GetQueryableAsync();
                var priorCompletedCount = await _asyncExecuter.LongCountAsync(
                    orderQueryable.Where(x =>
                        x.Id != order.Id
                        && x.Status == OrderStatus.Completed
                        && x.CustomerPhone == buyer.Phone));

                if (priorCompletedCount == 0)
                {
                    var referrer = await FindByReferralCodeAsync(order.ReferralCodeUsed);
                    if (referrer != null && referrer.Id != buyer.Id)
                    {
                        var bonus = Math.Max(0, settings.ReferralBonusPoints);
                        if (bonus > 0)
                        {
                            referrer.Points += bonus;
                            referrer.TotalEarned += bonus;
                            referrer.UpdatedAt = DateTime.UtcNow;
                            await _accounts.UpdateAsync(referrer, autoSave: true);

                            buyer.Points += bonus;
                            buyer.TotalEarned += bonus;
                        }
                    }
                }
            }

            await _accounts.UpdateAsync(buyer, autoSave: true);
        }

        public virtual async Task<(int Points, decimal VndOff)> RedeemAsync(
            string phone, int pointsRequested, ShopSettingDto settings)
        {
            if (pointsRequested <= 0)
            {
                return (0, 0);
            }

            var unit = settings.PointsRedeemUnit > 0 ? settings.PointsRedeemUnit : 100;
            var value = settings.PointsRedeemValue > 0 ? settings.PointsRedeemValue : 10000;

            // Làm tròn xuống bước đổi chẵn (vd 100 điểm = 10.000₫): điểm lẻ dưới
            // 1 bước không đổi được vì không có mệnh giá lẻ.
            var steps = pointsRequested / unit;
            if (steps <= 0)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Cần ít nhất {unit} điểm để đổi.");
            }

            var account = await GetOrCreateAsync(phone);
            var maxSteps = account.Points / unit;
            if (steps > maxSteps)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Bạn chỉ có {account.Points} điểm.");
            }

            var points = steps * unit;
            account.Points -= points;
            account.UpdatedAt = DateTime.UtcNow;
            await _accounts.UpdateAsync(account, autoSave: true);

            return (points, steps * value);
        }

        public virtual async Task RefundRedeemedPointsAsync(Order order)
        {
            // Hoàn điểm đổi khi đơn bị hủy — khách không mất điểm oan vì đơn
            // không thành; điểm hoàn cộng lại vào cả Points và TotalEarned.
            if (order.LoyaltyPointsRedeemed <= 0)
            {
                return;
            }

            var account = await FindByPhoneAsync(NormalizePhone(order.CustomerPhone));
            if (account == null)
            {
                return;
            }

            account.Points += order.LoyaltyPointsRedeemed;
            account.TotalEarned += order.LoyaltyPointsRedeemed;
            account.UpdatedAt = DateTime.UtcNow;
            await _accounts.UpdateAsync(account, autoSave: true);
        }

        public (string Tier, decimal DiscountPercent) GetTier(decimal totalSpent, ShopSettingDto settings)
        {
            // Vàng dùng '>' còn Bạc dùng '>=': đơn đúng bằng mốc Vàng thì lên
            // Vàng, không bị kẹp lửng giữa hai hạng.
            if (settings.TierGoldMin > 0 && totalSpent > settings.TierGoldMin)
            {
                return ("Vàng", Math.Max(0, settings.TierGoldDiscount));
            }

            if (settings.TierSilverMin > 0 && totalSpent >= settings.TierSilverMin)
            {
                return ("Bạc", Math.Max(0, settings.TierSilverDiscount));
            }

            return ("Đồng", 0);
        }

        /// <summary>
        /// Chuẩn hóa SĐT về chuỗi chữ số (bỏ dấu cách, dấu chấm, +84...), yêu
        /// cầu ≥ 9 chữ số — SĐT VN đủ 9-10 số; ngắn hơn coi như nhập sai, trả null.
        /// </summary>
        public string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length >= 9 ? digits : null;
        }

        private async Task<LoyaltyAccount> FindByPhoneAsync(string normalizedPhone)
        {
            if (string.IsNullOrEmpty(normalizedPhone))
            {
                return null;
            }

            var queryable = await _accounts.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Phone == normalizedPhone));
        }

        private async Task<string> GenerateReferralCodeAsync()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = Random.Shared; // perf-hunt 2026-10-19: Random.Shared is thread-safe and avoids same-tick seed collisions across concurrent requests

            for (var attempt = 0; attempt < 10; attempt++)
            {
                var code = "PT-" + new string(
                    Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());

                var queryable = await _accounts.GetQueryableAsync();
                var exists = await _asyncExecuter.FirstOrDefaultAsync(
                    queryable.Where(x => x.ReferralCode == code));
                if (exists == null)
                {
                    return code;
                }
            }

            return "PT-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        }

        private static DateTime NextBirthday(DateTime dob, DateTime today)
        {
            var next = new DateTime(today.Year, dob.Month, dob.Day);
            if (next < today)
            {
                next = next.AddYears(1);
            }

            return next;
        }

        private async Task<LoyaltyAccountDto> MapToDtoAsync(LoyaltyAccount account)
        {
            var settings = await _shopSettings.GetAsync();
            return MapToDto(account, settings);
        }

        private LoyaltyAccountDto MapToDto(LoyaltyAccount account, ShopSettingDto settings)
        {
            var (tier, pct) = GetTier(account.TotalSpent, settings);
            return new LoyaltyAccountDto
            {
                Phone = account.Phone,
                Points = account.Points,
                TotalEarned = account.TotalEarned,
                TotalSpent = account.TotalSpent,
                ReferralCode = account.ReferralCode,
                DateOfBirth = account.DateOfBirth,
                Tier = tier,
                TierDiscountPercent = pct
            };
        }
    }
}
