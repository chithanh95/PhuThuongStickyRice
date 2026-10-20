#nullable enable annotations
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Domain;
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
    /// <summary>
    /// Hợp đồng dịch vụ điểm thành viên theo SĐT: tích điểm khi đơn hoàn thành,
    /// đổi điểm trừ tiền, hoàn điểm khi hủy đơn, thưởng giới thiệu, xếp hạng
    /// Đồng/Bạc/Vàng theo tổng chi tiêu, và tra cứu theo SĐT.
    /// </summary>
    public interface ILoyaltyAppService : IApplicationService
    {
        /// <summary>Public: tóm tắt loyalty (điểm, hạng, tổng chi tiêu) theo SĐT.</summary>
        Task<LoyaltyAccountDto?> GetByPhoneAsync(string phone);

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
        Task<LoyaltyAccount?> FindByReferralCodeAsync(string referralCode);

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

        /// <summary>
        /// Internal: kẹp số điểm muốn đổi theo số tiền tối đa có thể giảm (làm
        /// tròn xuống theo bước đổi) — tránh trừ điểm nhiều hơn giá trị giảm
        /// thực tế mà đơn hàng áp dụng được.
        /// </summary>
        int CapRedeemPoints(int pointsRequested, decimal maxDiscountVnd, ShopSettingDto settings);

        /// <summary>Internal: hoàn lại điểm đã đổi khi đơn bị hủy.</summary>
        Task RefundRedeemedPointsAsync(Order order);

        /// <summary>
        /// Internal (bug-hunt 2026-10-19): hoàn lại phần điểm đổi DƯ khi khách
        /// bớt món làm LoyaltyDiscount bị kẹp xuống theo subtotal mới
        /// (RecalculateTotals) — không hoàn thì khách mất điểm oan cho phần
        /// giảm không còn áp dụng được. Chỉ cộng Points, KHÔNG cộng TotalEarned
        /// (giống RefundRedeemedPointsAsync: hoàn là trả số dư, không phải kiếm mới).
        /// </summary>
        /// <param name="order">Đơn hàng vừa bớt món.</param>
        /// <param name="pointsToRefund">Số điểm dư cần hoàn (&gt; 0).</param>
        Task RefundExcessRedeemedPointsAsync(Order order, int pointsToRefund);

        /// <summary>
        /// Internal: hoàn lại điểm đã đổi cho nhiều đơn bị hủy cùng lúc
        /// (worker tự hủy đơn bank-transfer quá hạn) — 1 query load accounts
        /// + 1 lần commit cuối thay vì 2 roundtrip cho mỗi đơn có đổi điểm.
        /// </summary>
        Task RefundRedeemedPointsBatchAsync(IList<Order> orders);

        /// <summary>Xếp hạng theo tổng chi tiêu: (Tên hạng, % giảm giá của hạng).</summary>
        (string Tier, decimal DiscountPercent) GetTier(decimal totalSpent, ShopSettingDto settings);

        /// <summary>
        /// Chuẩn hóa SĐT về chuỗi chữ số (bỏ dấu cách, dấu chấm, +84...), yêu
        /// cầu ≥ 9 chữ số — SĐT VN đủ 9-10 số; ngắn hơn coi như nhập sai, trả null.
        /// </summary>
        string? NormalizePhone(string phone);

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

        /// <summary>
        /// Lấy bước đổi (số điểm) và mệnh giá (số tiền) của 1 bước từ setting:
        /// setting chưa cấu hình (&lt;= 0) thì dùng default để đổi điểm vẫn
        /// hoạt động thay vì trả về 0.
        /// </summary>
        /// <param name="settings">Cấu hình shop.</param>
        /// <returns>(Unit: số điểm 1 bước, Value: số tiền 1 bước đổi được).</returns>
        private static (int Unit, decimal Value) ResolveRedeemStep(ShopSettingDto settings) =>
            (settings.PointsRedeemUnit > 0 ? settings.PointsRedeemUnit : DefaultPointsRedeemUnit,
             settings.PointsRedeemValue > 0 ? settings.PointsRedeemValue : DefaultPointsRedeemValue);

        /// <summary>
        /// Giới hạn số dòng khi admin liệt kê tài khoản điểm — bảng lớn dần
        /// theo đơn nên không trả toàn bộ.
        /// </summary>
        private const int MaxAdminListRows = 200;

        /// <summary>
        /// Số lần thử tối đa khi sinh mã giới thiệu "PT-XXXXXX" trước khi
        /// fallback sang GUID — tránh treo request khi DB gần đầy mã.
        /// </summary>
        private const int MaxReferralCodeAttempts = 10;

        private readonly Domain.Repositories.ILoyaltyAccountRepository _accounts;
        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IShopSettingAppService _shopSettings;

        /// <summary>Khởi tạo service quản lý điểm thành viên với repository và cài đặt shop.</summary>
        public LoyaltyAppService(
            Domain.Repositories.ILoyaltyAccountRepository accounts,
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter,
            IShopSettingAppService shopSettings)
        {
            _accounts = accounts;
            _orders = orders;
            _asyncExecuter = asyncExecuter;
            _shopSettings = shopSettings;
        }

        /// <summary>
        /// Tra cứu tài khoản điểm theo SĐT (public — trang tra cứu đơn/hồ sơ
        /// gọi để hiện điểm, hạng, tổng chi tiêu). SĐT được chuẩn hóa trước;
        /// SĐT sai định dạng hoặc chưa có tài khoản trả null (không lỗi 404).
        /// </summary>
        /// <param name="phone">SĐT khách hàng (chuẩn hóa digits đầu vào).</param>
        /// <returns>DTO tài khoản kèm hạng; null nếu SĐT không hợp lệ hoặc chưa có tài khoản.</returns>
        [AllowAnonymous]
        public virtual async Task<LoyaltyAccountDto?> GetByPhoneAsync(string phone)
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

        /// <summary>
        /// Khách tự cập nhật ngày sinh của mình (public — trang tra cứu đơn,
        /// mục "hồ sơ"). Tài khoản chưa có thì tạo mới. Chỉ lưu phần ngày
        /// (bỏ giờ) để so sánh sinh nhật hàng năm chính xác.
        /// </summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <param name="input">Ngày sinh mới.</param>
        /// <returns>Tài khoản sau khi cập nhật.</returns>
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

        /// <summary>
        /// Admin: danh sách tài khoản điểm, sắp xếp theo tổng chi tiêu giảm
        /// dần (khách chi nhiều nhất lên trước). Giới hạn 200 dòng mới nhất
        /// vì bảng lớn dần theo đơn. Tìm kiếm theo SĐT chỉ giữ lại chữ số
        /// (khách gõ "+84..." hay "0912..." đều tìm được).
        /// </summary>
        /// <param name="searchTerm">Chuỗi tìm kiếm theo SĐT; rỗng thì lấy tất cả.</param>
        /// <returns>Danh sách tài khoản kèm hạng hiện tại.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<LoyaltyAccountDto>> GetListAsync(string searchTerm)
        {
            var queryable = await _accounts.GetQueryableAsync();
            IQueryable<LoyaltyAccount> query = queryable.OrderByDescending(x => x.TotalSpent);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // codequality-team 2026-10-20: dùng PhoneNumber.DigitsOnly thay
                // copy-paste new string(...Where(char.IsDigit)...) — ngữ nghĩa giữ nguyên.
                var term = PhoneNumber.DigitsOnly(searchTerm);
                query = query.Where(x => x.Phone.Contains(term));
            }

            var items = await _asyncExecuter.ToListAsync(query.Take(MaxAdminListRows));
            var settings = await _shopSettings.GetAsync();
            return items.Select(x => MapToDto(x, settings)).ToList();
        }

        /// <summary>
        /// Admin: cộng/trừ điểm thủ công cho tài khoản (thưởng bù, phạt gian
        /// lận...). Chặn điểm về âm — đơn đã đổi điểm rồi bị hủy sẽ được
        /// RefundRedeemedPointsAsync hoàn sau nên trừ tay không được cho về âm
        /// trước. TotalEarned (lịch sử "đã kiếm") chỉ tăng khi cộng điểm.
        /// </summary>
        /// <param name="phone">SĐT tài khoản.</param>
        /// <param name="input">Số điểm thay đổi (âm = trừ).</param>
        /// <returns>Tài khoản sau khi điều chỉnh.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<LoyaltyAccountDto> AdjustAsync(string phone, AdjustPointsInput input)
        {
            var normalized = NormalizePhone(phone);
            // Try*PointsAsync chỉ UPDATE được trên row đã tồn tại
            // (WHERE Phone = ...): tạo tài khoản trước để tránh cộng/trừ bị
            // silent no-op, và tránh trừ điểm trên tài khoản chưa có bị ném
            // nhầm lỗi "Điểm không thể âm" dù số dư không hề âm.
            await GetOrCreateAsync(phone);
            if (input.PointsDelta < 0)
            {
                // Trừ điểm NGUYÊN TỬ (UPDATE ... WHERE Phone = @phone
                // AND Points >= @points): 2 admin trừ cùng lúc không ghi đè
                // lẫn nhau và không làm điểm về âm. Trả false cũng chính là
                // case "điểm không thể âm" (kể cả khi tài khoản vừa được tạo
                // với 0 điểm).
                var decremented = await _accounts.TryDecrementPointsAsync(normalized, -input.PointsDelta);
                if (!decremented)
                {
                    throw new Domain.Exceptions.DomainException("Điểm không thể âm.");
                }
            }
            else if (input.PointsDelta > 0)
            {
                // Cộng điểm NGUYÊN TỬ (Points và TotalEarned tăng trong 1 câu
                // UPDATE). TotalEarned là lịch sử "đã kiếm" nên chỉ tăng khi
                // cộng điểm, giữ đúng ngữ nghĩa cũ.
                await _accounts.TryIncrementPointsAsync(normalized, input.PointsDelta);
            }
            // Delta = 0: no-op (bản cũ chỉ chạm UpdatedAt), bỏ qua.

            // Đọc lại sau câu UPDATE nguyên tử (bypass change tracker) để
            // DTO phản ánh đúng số điểm mới nhất.
            var fresh = await FindByPhoneAsync(normalized)
                ?? throw new Domain.Exceptions.DomainException(
                    "Không tạo được tài khoản tích điểm, vui lòng thử lại.");
            return await MapToDtoAsync(fresh);
        }

        /// <summary>
        /// Giới hạn số ngày quét sinh nhật sắp tới (60 ngày): đủ cho chiến dịch
        /// chúc mừng, đồng thời tránh tính toán trên khoảng thời gian quá xa.
        /// </summary>
        private const int MaxDaysAhead = 60;

        /// <summary>
        /// Admin: tài khoản có sinh nhật trong N ngày tới (phục vụ chiến dịch
        /// gửi voucher sinh nhật). Kẹp daysAhead trong 0..60 ngày để admin
        /// không vô tình quét cả năm. Pre-filter theo tháng chạy trên SQL
        /// (superset, tối đa 3 tháng), so sánh tháng/ngày chính xác chạy
        /// in-memory (không phụ thuộc DB provider) và chỉ SELECT 6 cột cần cho DTO.
        /// </summary>
        /// <param name="daysAhead">Số ngày tới cần quét (0..60).</param>
        /// <returns>Danh sách tài khoản kèm hạng, sắp xếp theo ngày sinh nhật gần nhất.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<LoyaltyAccountDto>> GetUpcomingBirthdaysAsync(int daysAhead)
        {
            daysAhead = ClampDaysAhead(daysAhead);

            var today = VietnamTime.Today();
            var upcoming = await LoadUpcomingBirthdayRowsAsync(today, daysAhead);

            var settings = await _shopSettings.GetAsync();
            return upcoming.Select(row => MapToBirthdayDto(row, settings)).ToList();
        }

        /// <summary>
        /// Dòng rút gọn chỉ gồm 6 cột DTO sinh nhật cần — tránh materialize
        /// cả entity <c>LoyaltyAccount</c> khi chỉ cần so sánh tháng/ngày.
        /// </summary>
        private sealed record BirthdayRow(
            string Phone,
            int Points,
            long TotalEarned,
            decimal TotalSpent,
            string ReferralCode,
            // Non-nullable: truy vấn ở dưới đã lọc x.DateOfBirth.HasValue nên
            // truyền thẳng .Value vào đây, không cần null-forgiving (!) ở chỗ dùng.
            DateTime DateOfBirth);

        /// <summary>Giới hạn <paramref name="daysAhead"/> trong khoảng 0..MaxDaysAhead.</summary>
        private static int ClampDaysAhead(int daysAhead)
            => Math.Clamp(daysAhead, 0, MaxDaysAhead);

        /// <summary>
        /// Tải các dòng sinh nhật sắp tới, sắp xếp theo ngày sinh nhật gần nhất.
        /// </summary>
        /// <remarks>
        /// So sánh tháng/ngày chi tiết chạy trong bộ nhớ — giữ truy vấn không
        /// phụ thuộc nhà cung cấp DB (năm nhuận v.v.). Chỉ 6 cột DTO cần được
        /// select từ SQL thay vì materialize full entity.
        /// perf-hunt 2026-10-19: pre-filter theo tháng ngay trên SQL —
        /// sinh nhật trong [today, today+daysAhead] (tối đa 60 ngày) chỉ rơi
        /// vào tối đa 3 tháng, nên không cần quét toàn bộ tài khoản có ngày
        /// sinh (bảng tăng vô hạn). Đây là superset của filter chính xác theo
        /// ngày bên dưới nên kết quả trả về giống hệt.
        /// </remarks>
        private async Task<List<BirthdayRow>> LoadUpcomingBirthdayRowsAsync(DateTime today, int daysAhead)
        {
            var months = Enumerable.Range(0, daysAhead + 1)
                .Select(d => today.AddDays(d).Month)
                .Distinct()
                .ToList();

            var queryable = await _accounts.GetQueryableAsync();
            var rows = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.DateOfBirth.HasValue
                            && months.Contains(x.DateOfBirth.Value.Month))
                         .Select(x => new BirthdayRow(
                             x.Phone,
                             x.Points,
                             x.TotalEarned,
                             x.TotalSpent,
                             x.ReferralCode,
                             x.DateOfBirth.Value)));

            return rows.Select(row => (Row: row, Next: NextBirthday(row.DateOfBirth, today)))
                       .Where(x => (x.Next - today).TotalDays <= daysAhead)
                       .OrderBy(x => x.Next)
                       .Select(x => x.Row)
                       .ToList();
        }

        /// <summary>
        /// Map một dòng sinh nhật sang DTO, kèm hạng thành viên hiện tại.
        /// </summary>
        private LoyaltyAccountDto MapToBirthdayDto(BirthdayRow row, ShopSettingDto settings)
        {
            var (tier, pct) = GetTier(row.TotalSpent, settings);
            return new LoyaltyAccountDto
            {
                Phone = row.Phone,
                Points = row.Points,
                TotalEarned = row.TotalEarned,
                TotalSpent = row.TotalSpent,
                ReferralCode = row.ReferralCode,
                DateOfBirth = row.DateOfBirth,
                Tier = tier,
                TierDiscountPercent = pct
            };
        }

        /// <summary>
        /// Internal: lấy tài khoản theo SĐT, chưa có thì tạo mới (điểm 0,
        /// sinh mã giới thiệu riêng). Dùng ở checkout, hook đơn hoàn thành và
        /// background worker — SĐT không hợp lệ ném DomainException.
        /// </summary>
        /// <param name="phone">SĐT khách hàng (chuẩn hóa digits).</param>
        /// <returns>Tài khoản đã có hoặc vừa tạo.</returns>
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
            // Tạo tài khoản nguyên tử (bug-hunt run 16:25): 2 request đồng
            // thời cho SĐT mới đều thấy chưa có tài khoản rồi cùng insert —
            // unique (Phone) chỉ cho 1 bên insert, bên thua cũ nhận
            // DbUpdateException -> 500. TryInsertAsync trả null cho bên thua
            // -> đọc lại tài khoản bên thắng vừa tạo.
            var inserted = await _accounts.TryInsertAsync(account);
            if (inserted != null)
            {
                return inserted;
            }

            return await FindByPhoneAsync(normalized)
                ?? throw new Domain.Exceptions.DomainException(
                    "Không tạo được tài khoản tích điểm, vui lòng thử lại.");
        }

        /// <summary>
        /// Internal: tìm tài khoản theo mã giới thiệu. Mã được trim và đưa về
        /// in hoa nên khách nhập "pt-abc123" hay "PT-ABC123" đều tìm được;
        /// mã rỗng/trắng trả null.
        /// </summary>
        /// <param name="referralCode">Mã giới thiệu cần tìm.</param>
        /// <returns>Tài khoản chủ mã; null nếu không tồn tại.</returns>
        public virtual async Task<LoyaltyAccount?> FindByReferralCodeAsync(string referralCode)
        {
            if (string.IsNullOrWhiteSpace(referralCode))
            {
                return null;
            }

            var code = referralCode.Trim().ToUpperInvariant();
            var queryable = await _accounts.GetQueryableAsync();
            // [codequality-practices 2026-10-19] Truyền predicate thẳng vào
            // FirstOrDefaultAsync thay vì bọc queryable.Where(...) thừa.
            return await _asyncExecuter.FirstOrDefaultAsync(queryable, x => x.ReferralCode == code);
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
            // Entity buyer chỉ dùng để lấy Id/Phone (bất biến) cho bước
            // thưởng giới thiệu bên dưới; mọi cộng điểm/chi tiêu đều chạy
            // UPDATE nguyên tử ở DB (bug-hunt run 16:25): bản cũ đọc
            // Points/TotalSpent vào memory rồi += và UpdateAsync — 2 đơn của
            // cùng SĐT hoàn thành đồng thời sẽ cùng đọc số dư cũ, bên ghi sau
            // đè mất phần cộng của bên ghi trước (mất điểm tích lũy của khách
            // / sai tổng chi tiêu tính hạng thành viên).
            var buyer = await GetOrCreateAsync(order.CustomerPhone);

            // 1 điểm cho mỗi PointsPerVnd chi tiêu (mặc định 10.000₫), làm tròn
            // xuống — đơn nhỏ hơn mốc thì không được điểm nào (points = 0,
            // TotalSpent vẫn cộng bình thường).
            var perVnd = settings.PointsPerVnd > 0 ? settings.PointsPerVnd : DefaultPointsPerVnd;
            var earned = (int)Math.Floor(order.TotalAmount.Amount / perVnd);
            var awarded = await _accounts.TryIncrementEarnedAndSpentAsync(
                buyer.Phone, earned, order.TotalAmount.Amount);
            if (!awarded)
            {
                throw new Domain.Exceptions.DomainException(
                    "Không cộng được điểm tích lũy, vui lòng thử lại.");
            }

            await AwardReferralBonusIfFirstCompletedOrderAsync(buyer, order, settings);
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
            // Khách đã từng có đơn completed trước đó → không phải đơn đầu
            // tiên, bỏ qua để chống farm bonus lặp lại. Pre-filter bằng so
            // sánh trên cột chuẩn hóa (index seek) thay vì LIKE '%...%' quét
            // toàn bảng; bước kiểm tra khoan dung cuối cùng trên C# giữ
            // nguyên nên ngữ nghĩa không đổi.
            if (!await HasNoPriorCompletedOrderAsync(orderQueryable, order, buyer))
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

            // Cộng bonus nguyên tử cho cả 2 bên (bug-hunt run 16:25): bản cũ
            // đọc Points vào memory rồi += và UpdateAsync — 2 đơn có thưởng
            // giới thiệu hoàn thành đồng thời (hoặc trùng với điểm danh/đổi
            // điểm cùng SĐT) sẽ ghi đè làm mất bonus của nhau.
            await TryIncrementPointsOrThrowAsync(referrer.Phone, bonus);
            await TryIncrementPointsOrThrowAsync(buyer.Phone, bonus);
        }

        /// <summary>
        /// Cộng bonus nguyên tử cho 1 bên (người giới thiệu hoặc người mua) —
        /// gom 2 khối TryIncrementPointsAsync + throw y hệt nhau ở trên, giữ
        /// nguyên hành vi: DB cập nhật thất bại thì báo lỗi để thử lại.
        /// </summary>
        /// <param name="phone">SĐT đã chuẩn hóa của bên nhận bonus.</param>
        /// <param name="bonus">Số điểm thưởng (&gt; 0).</param>
        private async Task TryIncrementPointsOrThrowAsync(string phone, int bonus)
        {
            var awarded = await _accounts.TryIncrementPointsAsync(phone, bonus);
            if (!awarded)
            {
                throw new Domain.Exceptions.DomainException(
                    "Không cộng được điểm thưởng giới thiệu, vui lòng thử lại.");
            }
        }

        /// <summary>
        /// True khi khách CHƯA từng có đơn hoàn thành nào trước đó (trừ đơn
        /// hiện tại) — điều kiện để nhận bonus giới thiệu cho "đơn đầu tiên".
        /// So sánh tolerant theo 9 số cuối (pattern như HasOrderedBeforeAsync):
        /// Order.CustomerPhone lưu raw theo cách khách gõ, buyer.Phone đã
        /// chuẩn hóa digits — so raw trực tiếp cho phép đổi format SĐT để
        /// farm bonus nhiều lần. Dùng EXISTS thay vì tải toàn bộ SĐT về bộ
        /// nhớ: mọi dòng qua được bộ lọc SQL (CustomerPhoneNorm == 9 số cuối
        /// của buyer) đều thỏa PhoneNumber.PhonesMatch nên AnyAsync tương
        /// đương ngữ nghĩa với Any trên C#.
        /// </summary>
        private async Task<bool> HasNoPriorCompletedOrderAsync(
            IQueryable<Order> orderQueryable, Order order, LoyaltyAccount buyer)
        {
            var buyerKey = PhoneNumber.PhoneTailKey(buyer.Phone);
            return !await _asyncExecuter.AnyAsync(
                orderQueryable
                    .Where(x => x.Id != order.Id
                        && x.Status == OrderStatus.Completed
                        && x.CustomerPhoneNorm == buyerKey));
        }

        /// <summary>
        /// Kẹp số điểm muốn đổi theo số tiền tối đa có thể giảm (làm tròn xuống
        /// theo bước đổi): điểm vượt quá phần này sẽ bị trừ oan, vì
        /// <see cref="Order.Create"/> kẹp giá trị giảm điểm không vượt phần còn
        /// lại của đơn (sau khuyến mãi giờ vàng + mã giảm giá).
        /// </summary>
        /// <param name="pointsRequested">Số điểm khách muốn đổi.</param>
        /// <param name="maxDiscountVnd">Số tiền tối đa (đ) mà điểm đổi có thể giảm.</param>
        /// <param name="settings">Cấu hình bước đổi và mệnh giá.</param>
        /// <returns>Số điểm thực tế nên đổi (≤ điểm yêu cầu).</returns>
        public virtual int CapRedeemPoints(int pointsRequested, decimal maxDiscountVnd, ShopSettingDto settings)
        {
            if (pointsRequested <= 0 || maxDiscountVnd <= 0)
            {
                return 0;
            }

            var (unit, value) = ResolveRedeemStep(settings);
            if (unit <= 0 || value <= 0)
            {
                return 0;
            }

            // Số bước đổi tối đa mà số tiền còn lại "đỡ" được, làm tròn xuống.
            var maxSteps = (int)(maxDiscountVnd / value);
            return Math.Min(pointsRequested, maxSteps * unit);
        }

        /// <summary>
        /// Internal: trừ điểm khi khách đổi điểm ở checkout. Điểm yêu cầu được
        /// làm tròn xuống theo bước đổi (mặc định 100 điểm = 10.000₫) — điểm lẻ
        /// dưới 1 bước không đổi được vì không có mệnh giá lẻ. SĐT placeholder
        /// POS ("0000000000") trả (0, 0): không cho redeem pool điểm chung của
        /// các đơn vãng lai. Ném DomainException khi điểm không đủ hoặc dưới 1 bước đổi.
        /// </summary>
        /// <param name="phone">SĐT tài khoản đổi điểm.</param>
        /// <param name="pointsRequested">Số điểm khách muốn đổi.</param>
        /// <param name="settings">Cấu hình bước đổi và mệnh giá.</param>
        /// <returns>(Điểm thực tế đã trừ, số tiền được giảm ₫).</returns>
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

            var (unit, value) = ResolveRedeemStep(settings);

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
            await TryDecrementPointsOrThrowAsync(phone, points);

            return (points, steps * value);
        }

        /// <summary>
        /// Trừ điểm nguyên tử tại DB (UPDATE ... WHERE Phone = ... AND Points
        /// &gt;= points): 2 request đổi điểm đồng thời cùng SĐT không thể cùng
        /// trừ — chống double-spend (trừ điểm 2 lần / giảm giá 2 lần cho 1 lượt
        /// đổi) của race check-then-set cũ. Lỗi hiếm (điểm vừa bị request khác
        /// trừ sau bước check) thì đọc lại số dư mới nhất để báo message chính xác.
        /// </summary>
        /// <param name="phone">SĐT khách hàng (chưa chuẩn hóa).</param>
        /// <param name="points">Số điểm cần trừ (&gt; 0).</param>
        private async Task TryDecrementPointsOrThrowAsync(string phone, int points)
        {
            var normalized = NormalizePhone(phone);
            if (!await _accounts.TryDecrementPointsAsync(normalized, points))
            {
                var fresh = await FindByPhoneAsync(normalized);
                throw new Domain.Exceptions.DomainException(
                    $"Bạn chỉ có {fresh?.Points ?? 0} điểm.");
            }
        }

        /// <summary>
        /// Internal: hoàn lại điểm đã đổi khi đơn bị hủy — khách không mất
        /// điểm oan vì đơn không thành. Điểm hoàn chỉ cộng lại Points, KHÔNG
        /// cộng TotalEarned (hoàn là trả số dư, không phải kiếm mới). Đơn
        /// không có đổi điểm hoặc tài khoản không còn thì bỏ qua.
        /// Cộng điểm nguyên tử tại DB (TryRefundPointsAsync): 2 lần hủy
        /// đơn đồng thời cùng SĐT không thể cùng đọc số dư cũ rồi ghi đè làm
        /// mất phần cộng của nhau (race read-modify-write của bản cũ
        /// account.Points += ... rồi UpdateAsync).
        /// </summary>
        /// <param name="order">Đơn hàng đã bị hủy.</param>
        public virtual async Task RefundRedeemedPointsAsync(Order order)
        {
            // Hoàn điểm đổi khi đơn bị hủy — khách không mất điểm oan vì đơn
            // không thành. Hoàn điểm chỉ cộng lại Points, KHÔNG cộng
            // TotalEarned (lịch sử "đã kiếm") — hoàn là trả số dư, không phải
            // kiếm mới.
            if (order.LoyaltyPointsRedeemed <= 0)
            {
                return;
            }

            await _accounts.TryRefundPointsAsync(
                NormalizePhone(order.CustomerPhone), order.LoyaltyPointsRedeemed);
        }

        /// <summary>
        /// Internal (bug-hunt 2026-10-19): hoàn phần điểm đổi dư khi khách bớt
        /// món. RecalculateTotals kẹp LoyaltyDiscount theo subtotal mới nhưng
        /// LoyaltyPointsRedeemed giữ nguyên — không hoàn thì khách mất điểm oan
        /// cho phần giảm không còn áp dụng được. Cộng nguyên tử tại DB
        /// (TryRefundPointsAsync): chỉ Points, không phình TotalEarned.
        /// </summary>
        /// <param name="order">Đơn hàng vừa bớt món.</param>
        /// <param name="pointsToRefund">Số điểm dư cần hoàn (&gt; 0).</param>
        public virtual async Task RefundExcessRedeemedPointsAsync(Order order, int pointsToRefund)
        {
            if (order == null || pointsToRefund <= 0 || order.LoyaltyPointsRedeemed <= 0)
            {
                return;
            }

            await _accounts.TryRefundPointsAsync(
                NormalizePhone(order.CustomerPhone), pointsToRefund);
        }

        /// <summary>
        /// Hoàn điểm đổi cho nhiều đơn bị hủy cùng lúc. Gom delta theo từng
        /// SĐT distinct rồi cộng bằng <see cref="Domain.Repositories.ILoyaltyAccountRepository.TryRefundPointsAsync"/>
        /// (UPDATE nguyên tử, chỉ cộng Points không phình TotalEarned) — chống race read-modify-write: worker tự hủy
        /// đơn quá hạn chạy đồng thời với nhân viên hủy tay cùng đơn/SĐT sẽ
        /// không còn đọc số dư cũ rồi ghi đè làm mất phần cộng của nhau.
        /// Tài khoản không tồn tại thì bỏ qua (giống hành vi cũ).
        /// </summary>
        /// <param name="orders">Các đơn đã hủy cần hoàn điểm.</param>
        public virtual async Task RefundRedeemedPointsBatchAsync(IList<Order> orders)
        {
            var targets = (orders ?? Enumerable.Empty<Order>())
                .Where(x => x != null && x.LoyaltyPointsRedeemed > 0)
                .ToList();
            if (targets.Count == 0)
            {
                return;
            }

            var increments = GroupRefundPointsByPhone(targets);

            foreach (var (phone, points) in increments)
            {
                // Hoàn theo batch cũng chỉ cộng Points, không phình TotalEarned.
                await _accounts.TryRefundPointsAsync(phone, points);
            }
        }

        /// <summary>
        /// Gom số điểm cần hoàn theo từng SĐT đã chuẩn hóa (một SĐT có thể
        /// có nhiều đơn hủy cùng lúc): tách vòng lặp gom delta khỏi vòng lặp
        /// ghi DB trong <see cref="RefundRedeemedPointsBatchAsync"/> để mỗi
        /// vòng làm đúng 1 việc. SĐT không chuẩn hóa được thì bỏ qua.
        /// </summary>
        /// <param name="targets">Các đơn có điểm cần hoàn (&gt; 0).</param>
        /// <returns>Map SĐT đã chuẩn hóa → tổng điểm cần cộng lại.</returns>
        private Dictionary<string, int> GroupRefundPointsByPhone(IReadOnlyList<Order> targets)
        {
            var increments = new Dictionary<string, int>();
            foreach (var order in targets)
            {
                var phone = NormalizePhone(order.CustomerPhone);
                if (phone == null)
                {
                    continue;
                }

                increments.TryGetValue(phone, out var current);
                increments[phone] = current + order.LoyaltyPointsRedeemed;
            }

            return increments;
        }

        /// <summary>
        /// Xếp hạng thành viên theo tổng chi tiêu: trên mốc Vàng → "Vàng",
        /// từ mốc Bạc trở lên → "Bạc", còn lại → "Đồng" (không % giảm).
        /// Dùng '>' cho mốc Vàng và '&gt;=' cho mốc Bạc: phải VƯỢT mốc Vàng
        /// mới lên Vàng (đúng bằng mốc vẫn ở Bạc). % giảm âm do nhập nhầm
        /// được chặn về 0.
        /// </summary>
        /// <param name="totalSpent">Tổng số tiền khách đã chi tiêu (₫).</param>
        /// <param name="settings">Cấu hình mốc hạng và % giảm.</param>
        /// <returns>(Tên hạng tiếng Việt, % giảm giá của hạng).</returns>
        public (string Tier, decimal DiscountPercent) GetTier(decimal totalSpent, ShopSettingDto settings)
        {
            // Vàng dùng '>' (phải vượt mốc mới lên Vàng; đúng bằng mốc vẫn ở
            // Bạc), Bạc dùng '>=' (đúng bằng mốc Bạc thì lên Bạc).
            if (settings.TierGoldMin > 0 && totalSpent > settings.TierGoldMin)
            {
                return (MemberTierNames.Gold, Math.Max(0, settings.TierGoldDiscount));
            }

            if (settings.TierSilverMin > 0 && totalSpent >= settings.TierSilverMin)
            {
                return (MemberTierNames.Silver, Math.Max(0, settings.TierSilverDiscount));
            }

            return (MemberTierNames.Bronze, 0);
        }

        /// <summary>
        /// Chuẩn hóa SĐT về chuỗi chữ số (bỏ dấu cách, dấu chấm, +84...), yêu
        /// cầu ≥ 9 chữ số — SĐT VN đủ 9-10 số; ngắn hơn coi như nhập sai, trả null.
        /// Ủy thác cho <see cref="VietnamTime.NormalizePhone"/> để chỉ giữ 1
        /// bản logic duy nhất (xóa bản copy-paste trước đây nằm ở đây).
        /// </summary>
        public string? NormalizePhone(string phone) => VietnamTime.NormalizePhone(phone);

        /// <summary>
        /// SĐT placeholder (vd "0000000000" POS gán cho khách vãng lai): mọi
        /// chữ số đều giống nhau — không bao giờ là SĐT thật của khách.
        /// </summary>
        public bool IsPlaceholderPhone(string phone)
        {
            var digits = PhoneNumber.DigitsOnly(phone);
            return digits.Length > 0 && digits.All(c => c == digits[0]);
        }

        /// <summary>
        /// Tìm tài khoản theo SĐT đã chuẩn hóa digits. SĐT rỗng trả null (không
        /// ném) để các caller public quyết định hành vi (tạo mới hay báo lỗi).
        /// </summary>
        /// <param name="normalizedPhone">SĐT đã chuẩn hóa về chữ số.</param>
        /// <returns>Tài khoản tương ứng; null nếu SĐT rỗng hoặc chưa có tài khoản.</returns>
        private async Task<LoyaltyAccount?> FindByPhoneAsync(string normalizedPhone)
        {
            if (string.IsNullOrEmpty(normalizedPhone))
            {
                return null;
            }

            var queryable = await _accounts.GetQueryableAsync();
            // [codequality-practices 2026-10-19] Truyền predicate thẳng vào
            // FirstOrDefaultAsync thay vì bọc queryable.Where(...) thừa.
            return await _asyncExecuter.FirstOrDefaultAsync(queryable, x => x.Phone == normalizedPhone);
        }

        /// <summary>
        /// Sinh mã giới thiệu duy nhất dạng "PT-XXXXXX" (bỏ ký tự dễ nhầm
        /// I/O/0/1). Thử tối đa 10 lần rồi fallback sang GUID để không treo
        /// request khi DB gần đầy mã.
        /// </summary>
        /// <returns>Mã giới thiệu chưa từng tồn tại trong DB.</returns>
        private async Task<string> GenerateReferralCodeAsync()
        {
            // [codequality-smell 2026-10-20] Dùng helper dùng chung
            // Common.ReadableCodeGenerator — trước đây dòng LINQ sinh chuỗi ngẫu
            // nhiên bị viết lại ở cả đây và DiscountAppService.
            // Mã giới thiệu gắn với điểm thưởng nên dùng RNG an toàn mật mã
            // thay vì Random.Shared (có thể bị đoán mã tiếp theo từ mã đã lộ).

            for (var attempt = 0; attempt < MaxReferralCodeAttempts; attempt++)
            {
                var code = ReferralCodes.Prefix + ReadableCodeGenerator.GenerateSuffix(ReferralCodes.SuffixLength);

                var queryable = await _accounts.GetQueryableAsync();
                // [codequality-practices 2026-10-19] Chỉ cần biết mã đã tồn tại
                // hay chưa — AnyAsync phát sinh SELECT 1 gọn hơn FirstOrDefaultAsync
                // nạp cả entity vào bộ nhớ/change tracker chỉ để check null.
                if (!await _asyncExecuter.AnyAsync(queryable, x => x.ReferralCode == code))
                {
                    return code;
                }
            }

            return ReferralCodes.Prefix + Guid.NewGuid().ToString("N")[..ReferralCodes.SuffixLength].ToUpperInvariant();
        }

        /// <summary>
        /// Tính ngày sinh nhật sắp tới (29/2 được coi là 28/2 vào năm không nhuận,
        /// cùng cách xử lý của trang landing — tránh ArgumentOutOfRangeException
        /// khi khách sinh ngày 29/2 tra cứu danh sách sinh nhật sắp tới).
        /// </summary>
        private static DateTime NextBirthday(DateTime dob, DateTime today)
        {
            var next = SafeBirthdayDate(dob.Month, dob.Day, today.Year);
            if (next < today)
            {
                next = SafeBirthdayDate(dob.Month, dob.Day, today.Year + 1);
            }

            return next;
        }

        /// <summary>
        /// Dựng DateTime an toàn cho ngày sinh: 29/2 → 28/2 vào năm không nhuận.
        /// </summary>
        private static DateTime SafeBirthdayDate(int month, int day, int year)
        {
            if (month == 2 && day == 29 && !DateTime.IsLeapYear(year))
            {
                day = 28;
            }

            return new DateTime(year, month, day);
        }

        /// <summary>Chuyển tài khoản sang DTO (tự load cài đặt shop để tính hạng).</summary>
        /// <param name="account">Entity tài khoản loyalty.</param>
        /// <returns>DTO tài khoản kèm hạng thành viên hiện tại.</returns>
        private async Task<LoyaltyAccountDto> MapToDtoAsync(LoyaltyAccount account)
        {
            var settings = await _shopSettings.GetAsync();
            return MapToDto(account, settings);
        }

        /// <summary>Chuyển tài khoản sang DTO kèm hạng thành viên tính từ <paramref name="settings"/>.</summary>
        /// <param name="account">Entity tài khoản loyalty.</param>
        /// <param name="settings">Cấu hình mốc hạng và % giảm.</param>
        /// <returns>DTO tài khoản kèm hạng thành viên.</returns>
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
