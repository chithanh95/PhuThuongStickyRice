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
        /// <summary>Public: tóm tắt loyalty (điểm, hạng, tổng chi tiêu) theo SĐT.</summary>
        Task<LoyaltyAccountDto> GetByPhoneAsync(string phone);

        /// <summary>Public: khách tự cập nhật ngày sinh của mình (trang tra cứu đơn "hồ sơ").</summary>
        Task<LoyaltyAccountDto> UpdateBirthdayAsync(string phone, UpdateBirthdayInput input);

        /// <summary>Admin: danh sách tài khoản phân trang, tìm kiếm theo SĐT.</summary>
        Task<List<LoyaltyAccountDto>> GetListAsync(string searchTerm);

        /// <summary>Admin: cộng/trừ điểm thủ công.</summary>
        Task<LoyaltyAccountDto> AdjustAsync(string phone, AdjustPointsInput input);

        /// <summary>Admin: tài khoản có sinh nhật trong N ngày tới.</summary>
        Task<List<LoyaltyAccountDto>> GetUpcomingBirthdaysAsync(int daysAhead);

        /// <summary>
        /// Internal: lấy hoặc tạo tài khoản theo SĐT. Dùng ở checkout,
        /// hook đơn hoàn thành và background worker.
        /// </summary>
        Task<LoyaltyAccount> GetOrCreateAsync(string phone);

        /// <summary>
        /// Internal: tìm tài khoản theo mã giới thiệu (không phân biệt hoa/thường).
        /// </summary>
        Task<LoyaltyAccount> FindByReferralCodeAsync(string referralCode);

        /// <summary>
        /// Internal: gọi đúng 1 lần khi đơn đạt trạng thái Completed — cộng điểm,
        /// cập nhật tổng chi tiêu và thưởng giới thiệu ở đơn hoàn thành đầu tiên
        /// của khách có dùng mã giới thiệu.
        /// </summary>
        Task AwardCompletedOrderAsync(Order order);

        /// <summary>
        /// Internal: trừ điểm khi đổi điểm. Trả về (điểm đã trừ, số tiền được giảm ₫).
        /// </summary>
        Task<(int Points, decimal VndOff)> RedeemAsync(string phone, int pointsRequested, ShopSettingDto settings);

        /// <summary>Internal: hoàn lại điểm đã đổi khi đơn bị hủy.</summary>
        Task RefundRedeemedPointsAsync(Order order);

        /// <summary>Xếp hạng theo tổng chi tiêu: (Tên hạng, % giảm giá của hạng).</summary>
        (string Tier, decimal DiscountPercent) GetTier(decimal totalSpent, ShopSettingDto settings);

        /// <summary>
        /// Chuẩn hóa SĐT về chuỗi chữ số (bỏ dấu cách, dấu chấm, +84...), yêu
        /// cầu ≥ 9 chữ số — SĐT VN đủ 9-10 số; ngắn hơn coi như nhập sai, trả null.
        /// </summary>
        string NormalizePhone(string phone);

        /// <summary>
        /// True cho SĐT placeholder kiểu "0000000000" (POS gán cho khách vãng
        /// lai không để lại SĐT): mọi chữ số đều giống nhau, không phải SĐT thật.
        /// </summary>
        bool IsPlaceholderPhone(string phone);
    }

    /// <summary>
    /// Quản lý điểm thành viên theo SĐT: tích điểm khi đơn hoàn thành, đổi điểm
    /// trừ tiền, hoàn điểm khi hủy đơn, thưởng giới thiệu (cả 2 bên), xếp hạng
    /// Đồng/Bạc/Vàng theo tổng chi tiêu. Điểm không bao giờ âm.
    /// </summary>
    public class LoyaltyAppService : ApplicationService, ILoyaltyAppService
    {
        // Giá trị dự phòng khi setting chưa được cấu hình (<= 0) — giữ đồng bộ
        // với các Default* trong ShopSettingAppService cho cùng 1 khái niệm.
        private const int DefaultPointsPerVnd = 10000;
        private const int DefaultPointsRedeemUnit = 100;
        private const int DefaultPointsRedeemValue = 10000;

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

        /// <summary>
        /// Cộng điểm và tổng chi tiêu khi 1 đơn hàng đạt trạng thái hoàn thành;
        /// đồng thời thưởng điểm giới thiệu (cả người giới thiệu lẫn người mua)
        /// nếu đây là đơn hoàn thành đầu tiên của khách dùng mã giới thiệu hợp lệ.
        /// Bỏ qua SĐT placeholder "0000000000" của POS (khách vãng lai).
        /// </summary>
        public virtual async Task AwardCompletedOrderAsync(Order order)
        {
            // POS gán "0000000000" cho khách vãng lai không để lại SĐT: không
            // tích điểm/tiền vào tài khoản chung này, kẻo điểm của mọi đơn
            // vãng lai dồn vào 1 pool mà bất kỳ ai nhập SĐT đó cũng redeem được.
            if (IsPlaceholderPhone(order.CustomerPhone))
            {
                return;
            }

            var settings = await _shopSettings.GetAsync();
            var buyer = await GetOrCreateAsync(order.CustomerPhone);

            // 1 điểm cho mỗi PointsPerVnd chi tiêu (mặc định 10.000₫), làm tròn
            // xuống — đơn nhỏ hơn mốc thì không được điểm nào.
            var perVnd = settings.PointsPerVnd > 0 ? settings.PointsPerVnd : DefaultPointsPerVnd;
            var earned = (int)Math.Floor(order.TotalAmount.Amount / perVnd);
            if (earned > 0)
            {
                buyer.Points += earned;
                buyer.TotalEarned += earned;
            }

            buyer.TotalSpent += order.TotalAmount.Amount;
            buyer.UpdatedAt = DateTime.UtcNow;

            await AwardReferralBonusIfFirstCompletedOrderAsync(buyer, order, settings);

            await _accounts.UpdateAsync(buyer, autoSave: true);
        }

        /// <summary>
        /// Thưởng điểm giới thiệu cho người giới thiệu và người mua, nhưng CHỈ
        /// cho đơn hoàn thành đầu tiên dùng mã giới thiệu hợp lệ — chống tự
        /// giới thiệu lặp lại để farm bonus; người giới thiệu và người được
        /// giới thiệu không được là cùng 1 tài khoản.
        /// </summary>
        private async Task AwardReferralBonusIfFirstCompletedOrderAsync(
            LoyaltyAccount buyer, Order order, ShopSettingDto settings)
        {
            if (string.IsNullOrWhiteSpace(order.ReferralCodeUsed))
            {
                return;
            }

            var orderQueryable = await _orders.GetQueryableAsync();
            // So sánh tolerant theo 9 số cuối (pattern như
            // HasOrderedBeforeAsync): Order.CustomerPhone lưu raw theo
            // cách khách gõ, buyer.Phone đã chuẩn hóa digits — so raw
            // trực tiếp cho phép đổi format SĐT để farm bonus nhiều lần.
            var buyerKey = buyer.Phone.Length >= 9
                ? buyer.Phone.Substring(buyer.Phone.Length - 9)
                : buyer.Phone;
            var candidatePhones = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Id != order.Id
                        && x.Status == OrderStatus.Completed
                        && x.CustomerPhone != null
                        && x.CustomerPhone.Contains(buyerKey))
                    .Select(x => x.CustomerPhone));
            var priorCompletedCount = candidatePhones
                .Count(p => PhonesEqualTolerant(p, buyer.Phone));

            // Đã từng có đơn completed trước đó → không phải đơn đầu tiên,
            // bỏ qua để chống farm bonus lặp lại.
            if (priorCompletedCount > 0)
            {
                return;
            }

            var referrer = await FindByReferralCodeAsync(order.ReferralCodeUsed);
            // Mã không tồn tại hoặc tự giới thiệu chính mình → không thưởng.
            if (referrer == null || referrer.Id == buyer.Id)
            {
                return;
            }

            var bonus = Math.Max(0, settings.ReferralBonusPoints);
            if (bonus <= 0)
            {
                return;
            }

            referrer.Points += bonus;
            referrer.TotalEarned += bonus;
            referrer.UpdatedAt = DateTime.UtcNow;
            await _accounts.UpdateAsync(referrer, autoSave: true);

            // buyer được lưu bởi method gọi (AwardCompletedOrderAsync) ngay sau đó.
            buyer.Points += bonus;
            buyer.TotalEarned += bonus;
        }

        public virtual async Task<(int Points, decimal VndOff)> RedeemAsync(
            string phone, int pointsRequested, ShopSettingDto settings)
        {
            if (pointsRequested <= 0)
            {
                return (0, 0);
            }

            // SĐT placeholder (POS "0000000000"): không cho redeem pool điểm
            // chung của các đơn vãng lai — trả 0 thay vì trừ tiền.
            if (IsPlaceholderPhone(phone))
            {
                return (0, 0);
            }

            var unit = settings.PointsRedeemUnit > 0 ? settings.PointsRedeemUnit : DefaultPointsRedeemUnit;
            var value = settings.PointsRedeemValue > 0 ? settings.PointsRedeemValue : DefaultPointsRedeemValue;

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

        /// <summary>
        /// SĐT placeholder (vd "0000000000" POS gán cho khách vãng lai): mọi
        /// chữ số đều giống nhau — không bao giờ là SĐT thật của khách.
        /// </summary>
        public bool IsPlaceholderPhone(string phone)
        {
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            return digits.Length > 0 && digits.All(c => c == digits[0]);
        }

        /// <summary>
        /// So sánh 2 SĐT theo 9 số cuối (cùng chuẩn với
        /// OrderAppService.PhonesMatch): "0912 345 678", "+84912345678" và
        /// "0912345678" là cùng 1 số — chống farm bằng cách đổi format.
        /// </summary>
        private static bool PhonesEqualTolerant(string a, string b)
        {
            var da = new string((a ?? string.Empty).Where(char.IsDigit).ToArray());
            var db = new string((b ?? string.Empty).Where(char.IsDigit).ToArray());
            if (da.Length < 9 || db.Length < 9)
            {
                return string.Equals(da, db, StringComparison.Ordinal);
            }

            return string.Equals(
                da.Substring(da.Length - 9),
                db.Substring(db.Length - 9),
                StringComparison.Ordinal);
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
