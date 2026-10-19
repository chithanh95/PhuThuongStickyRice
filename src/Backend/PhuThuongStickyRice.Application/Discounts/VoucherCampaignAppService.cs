using PhuThuongStickyRice.Domain;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.Application.Discounts
{
    /// <summary>
    /// Hợp đồng chiến dịch voucher: xem quy mô từng nhóm đối tượng và
    /// phát hành hàng loạt mã giảm giá cá nhân cho khách.
    /// </summary>
    public interface IVoucherCampaignAppService : IApplicationService
    {
        /// <summary>Admin: quy mô đối tượng cho khung chọn chiến dịch (3 nhóm: không mua 30+ ngày, theo hạng, tất cả).</summary>
        /// <param name="tier">Tên hạng ("Bạc"/"Vàng") cho nhóm theo hạng; trống thì mặc định hạng Vàng.</param>
        /// <returns>Danh sách 3 nhóm kèm số lượng người nhận ước tính.</returns>
        Task<List<VoucherCampaignAudienceDto>> GetAudiencesAsync(string tier);

        /// <summary>Admin: phát hành 1 mã giảm giá cá nhân cho mỗi SĐT thuộc đối tượng đã chọn.</summary>
        /// <param name="input">Đối tượng, mức giảm, tiền tố mã, số ngày hiệu lực.</param>
        /// <returns>Danh sách cặp SĐT → mã voucher đã tạo, để admin copy đi gửi.</returns>
        Task<List<GeneratedVoucherDto>> GenerateAsync(CreateVoucherCampaignInput input);
    }

    /// <summary>
    /// Item 132: chiến dịch phát voucher hàng loạt. Mỗi SĐT nhận đúng 1 mã
    /// riêng (OwnerPhone = SĐT đó) để khách không dùng mã của nhau.
    /// Gửi hàng loạt đang để deferred — cần kênh Zalo OA / SMS; danh sách kết
    /// quả sinh ra để admin copy sang công cụ broadcast sẵn có của quán.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class VoucherCampaignAppService : ApplicationService, IVoucherCampaignAppService
    {
        // Audience keys (dùng chung cho GetAudiencesAsync và GenerateAsync).
        private const string AudienceInactive30 = "inactive30";
        private const string AudienceTier = "tier";
        private const string AudienceAll = "all";

        // "Không mua 30+ ngày": số ngày không có đơn để xếp vào nhóm không hoạt động.
        private const int InactiveDays = 30;

        // Prefix mặc định cho mã voucher sinh tự động.
        private const string DefaultCodePrefix = "VC";

        // Độ dài phần hậu tố ngẫu nhiên của mã voucher (GUID rút gọn, chữ hoa).
        private const int CodeSuffixLength = 6;
        // Số lần thử tối đa khi sinh mã không trùng DB; quá giới hạn thì báo
        // lỗi thay vì treo request (DB gần như đầy mã).
        private const int CodeMaxAttempts = 20;
        // Kích thước chunk khi kiểm tra trùng mã với DB (dưới giới hạn tham số của SQL Server).
        private const int CodeCheckChunkSize = 1000;
        // Số voucher tối đa sinh trong 1 lần GenerateAsync: chặn click nhầm
        // "Tất cả khách" với base lớn gây OOM/timeout. Đồng nhất với
        // MaxExportRows = 5000 của các màn hình export; shop quy mô nhỏ không
        // phát hành hợp lệ >5000 voucher/lần.
        private const int MaxGenerateVouchers = 5000;

        private readonly IRepository<LoyaltyAccount, Guid> _loyaltyAccounts;
        private readonly IRepository<Order, Guid> _orders;
        private readonly IRepository<DiscountCode, Guid> _discountCodes;
        private readonly IShopSettingAppService _shopSettings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Khởi tạo service chiến dịch voucher với các repository và service liên quan.</summary>
        public VoucherCampaignAppService(
            IRepository<LoyaltyAccount, Guid> loyaltyAccounts,
            IRepository<Order, Guid> orders,
            IRepository<DiscountCode, Guid> discountCodes,
            IShopSettingAppService shopSettings,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _loyaltyAccounts = loyaltyAccounts;
            _orders = orders;
            _discountCodes = discountCodes;
            _shopSettings = shopSettings;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Liệt kê 3 nhóm đối tượng chiến dịch cùng số lượng người nhận ước
        /// tính, để admin chọn đối tượng trước khi phát hành.
        /// </summary>
        /// <param name="tier">Tên hạng ("Bạc"/"Vàng") dùng cho nhóm theo hạng; để trống thì mặc định hạng Vàng.</param>
        /// <returns>Danh sách 3 nhóm: khách không mua 30+ ngày, khách theo hạng, tất cả khách.</returns>
        public virtual async Task<List<VoucherCampaignAudienceDto>> GetAudiencesAsync(string tier)
        {
            // perf-hunt 2026-10-19: chỉ cần số lượng để hiển thị quy mô nhóm,
            // dùng SELECT COUNT trên SQL thay vì nạp toàn bộ SĐT về memory.
            var inactiveCount = await GetInactiveCountAsync();
            var tierCount = await GetTierCountAsync(tier);
            var allCount = await GetAllCountAsync();

            return new List<VoucherCampaignAudienceDto>
            {
                new VoucherCampaignAudienceDto { Key = AudienceInactive30, Name = "Không mua 30+ ngày", RecipientCount = inactiveCount },
                new VoucherCampaignAudienceDto { Key = AudienceTier, Name = $"Hạng {tier ?? MemberTierNames.Gold}", RecipientCount = tierCount },
                new VoucherCampaignAudienceDto { Key = AudienceAll, Name = "Tất cả khách", RecipientCount = allCount }
            };
        }

        /// <summary>
        /// Giải quyết đối tượng chiến dịch thành danh sách SĐT nhận voucher.
        /// Tách ra để GenerateAsync ngắn gọn hơn (logic giữ nguyên).
        /// </summary>
        private async Task<List<string>> ResolveAudiencePhonesAsync(string audience, string tier)
        {
            switch ((audience ?? string.Empty).Trim().ToLowerInvariant())
            {
                case AudienceInactive30:
                    return await GetInactivePhonesAsync();
                case AudienceTier:
                    return await GetTierPhonesAsync(tier);
                case AudienceAll:
                    return await GetAllPhonesAsync();
                default:
                    throw new DomainException("Đối tượng chiến dịch không hợp lệ.");
            }
        }

        /// <summary>
        /// Phát hành 1 mã giảm giá cá nhân cho mỗi SĐT trong đối tượng đã chọn
        /// (dùng 1 lần, có hiệu lực <paramref name="input"/> <c>ValidDays</c> ngày).
        /// </summary>
        /// <param name="input">Đối tượng (inactive30/tier/all), mức giảm (% hoặc số tiền cố định), tiền tố mã, hạn dùng.</param>
        /// <returns>Danh sách cặp SĐT → mã voucher đã tạo, để admin copy đi gửi.</returns>
        /// <exception cref="DomainException">Ném khi đối tượng rỗng hoặc không sinh được mã duy nhất.</exception>
        public virtual async Task<List<GeneratedVoucherDto>> GenerateAsync(CreateVoucherCampaignInput input)
        {
            var phones = await ResolveAudiencePhonesAsync(input.Audience, input.Tier);

            if (phones.Count == 0)
            {
                throw new DomainException("Không có khách hàng nào trong đối tượng đã chọn.");
            }

            // Chặn đối tượng quá lớn: GenerateUniqueCodesAsync dựng toàn bộ N mã
            // ứng viên trong 1 HashSet ở memory rồi InsertAsync N lần trong 1
            // UoW — 1 cú click nhầm "Tất cả khách" với base lớn có thể gây
            // OOM/timeout request.
            if (phones.Count > MaxGenerateVouchers)
            {
                throw new DomainException(
                    $"Đối tượng quá lớn ({phones.Count} khách). Vui lòng thu hẹp đối tượng chiến dịch (tối đa {MaxGenerateVouchers}) rồi thử lại.");
            }

            // Chỉ lọc ký tự chữ/số và viết hoa tiền tố để mã voucher gọn, dễ đọc,
            // tránh SĐT/ký tự đặc biệt lọt vào mã.
            var prefix = SanitizeCodePrefix(input.Prefix);

            var expiresAt = DateTime.UtcNow.AddDays(input.ValidDays);
            // perf-hunt 2026-10-19: generate all unique codes with a single
            // batched existence check instead of one DB round-trip per code.
            var codes = await GenerateUniqueCodesAsync(prefix, phones.Count);
            var result = new List<GeneratedVoucherDto>();

            for (var i = 0; i < phones.Count; i++)
            {
                await _discountCodes.InsertAsync(BuildVoucherCode(codes[i], phones[i], input, expiresAt));
                result.Add(new GeneratedVoucherDto { Phone = phones[i], Code = codes[i] });
            }

            // ABP unit of work tự commit toàn bộ batch khi method kết thúc (không cần SaveChanges thủ công).
            return result;
        }

        /// <summary>
        /// Làm sạch tiền tố mã voucher: chỉ giữ chữ/số, viết hoa; rỗng thì
        /// dùng tiền tố mặc định (<see cref="DefaultCodePrefix"/>).
        /// </summary>
        private static string SanitizeCodePrefix(string prefix)
        {
            var sanitized = string.IsNullOrWhiteSpace(prefix)
                ? DefaultCodePrefix
                : new string(prefix.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrEmpty(sanitized) ? DefaultCodePrefix : sanitized;
        }

        /// <summary>
        /// Dựng entity mã voucher cá nhân: dùng 1 lần, gắn đúng SĐT nhận để
        /// không chia sẻ/tái sử dụng; ưu tiên số tiền cố định, % và số tiền
        /// không tồn tại song song (checkout chỉ áp dụng 1 trong 2).
        /// Ủy thác cho <see cref="DiscountCodeFactory.CreatePersonalVoucher"/>
        /// dùng chung với quà sinh nhật của CustomerProfileAppService.
        /// </summary>
        private static DiscountCode BuildVoucherCode(
            string code, string phone, CreateVoucherCampaignInput input, DateTime expiresAt)
        {
            return DiscountCodeFactory.CreatePersonalVoucher(
                code,
                phone,
                discountAmount: input.DiscountAmount > 0 ? input.DiscountAmount : (decimal?)null,
                discountPercent: input.DiscountAmount > 0 ? (decimal?)null : input.DiscountPercent,
                expiresAt: expiresAt);
        }

        /// <summary>
        /// Lấy SĐT khách "không mua 30+ ngày": có tài khoản loyalty nhưng không có
        /// đơn nào (trừ đơn đã hủy) trong 30 ngày gần nhất.
        /// </summary>
        private async Task<List<string>> GetInactivePhonesAsync()
        {
            // perf-hunt 2026-10-19: anti-join ngay trên SQL (NOT EXISTS) thay vì
            // nạp toàn bộ SĐT bảng LoyaltyAccount (tăng vô hạn theo thời gian)
            // về memory rồi trừ bằng HashSet — mỗi lần admin mở màn hình chọn
            // đối tượng chiến dịch đều quét full bảng trước đây.
            // So khớp khoan dung theo 9 số cuối — cùng chuẩn
            // Order.CustomerPhoneNorm (đã backfill + index từ migration
            // 20261019160000) — nên khách đổi cách gõ SĐT ("0912..." vs
            // "+84912...") vẫn tính là đã mua, thay vì lọt nhầm vào nhóm
            // "không mua" như bản so full-digits trước đây.
            var activeNorms = await GetActivePhoneNormsAsync();

            var loyaltyQuery = await _loyaltyAccounts.GetQueryableAsync();
            return await _asyncExecuter.ToListAsync(
                loyaltyQuery
                    .Where(x => !activeNorms.Contains(
                        x.Phone.Length <= PhoneNumber.TailDigitCount ? x.Phone : x.Phone.Substring(x.Phone.Length - PhoneNumber.TailDigitCount)))
                    .Select(x => x.Phone));
        }

        /// <summary>
        /// Query SĐT chuẩn hóa (9 số cuối) của khách ĐÃ mua trong
        /// <see cref="InactiveDays"/> ngày gần nhất (trừ đơn đã hủy).
        /// Dùng chung cho <see cref="GetInactivePhonesAsync"/> và
        /// <see cref="GetInactiveCountAsync"/> để 2 luồng "liệt kê" và "đếm"
        /// không lệch điều kiện nhau.
        /// </summary>
        private async Task<IQueryable<string>> GetActivePhoneNormsAsync()
        {
            var since = DateTime.UtcNow.AddDays(-InactiveDays);
            var orderQuery = await _orders.GetQueryableAsync();
            return orderQuery
                .Where(x => x.CreatedAt >= since && x.Status != OrderStatus.Cancelled)
                .Select(x => x.CustomerPhoneNorm)
                .Distinct();
        }

        /// <summary>
        /// Lấy SĐT khách theo hạng, khớp chuẩn xếp hạng
        /// <see cref="PhuThuongStickyRice.Application.Loyalty.LoyaltyAppService.GetTier"/> (nơi tính % giảm
        /// hạng ở checkout và hiển thị hạng cho khách): Vàng = phải VƯỢT mốc
        /// (&gt;), Bạc = từ mốc trở lên (&gt;=); mốc &lt;= 0 nghĩa là hạng đó
        /// không tồn tại → không nhắm ai.
        /// </summary>
        /// <param name="tier">Tên hạng ("Bạc" dùng ngưỡng Bạc, còn lại dùng ngưỡng Vàng).</param>
        private async Task<List<string>> GetTierPhonesAsync(string tier)
        {
            var resolved = await ResolveTierMinAsync(tier);
            if (resolved == null)
            {
                return new List<string>();
            }

            var (isSilver, min) = resolved.Value;
            var queryable = await _loyaltyAccounts.GetQueryableAsync();
            if (isSilver)
            {
                return await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.TotalSpent >= min).Select(x => x.Phone));
            }

            return await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.TotalSpent > min).Select(x => x.Phone));
        }

        /// <summary>
        /// Đọc mốc chi tiêu của hạng từ setting. Khớp chuẩn xếp hạng
        /// <see cref="PhuThuongStickyRice.Application.Loyalty.LoyaltyAppService.GetTier"/>
        /// (nơi tính % giảm hạng ở checkout và hiển thị hạng cho khách):
        /// Vàng = phải VƯỢT mốc (&gt;), Bạc = từ mốc trở lên (&gt;=).
        /// Mốc &lt;= 0 nghĩa là hạng đó không tồn tại → trả null để bên gọi
        /// bỏ qua nhóm (bản cũ thiếu guard này sẽ nhắm toàn bộ tài khoản
        /// trong khi GetTier không xếp ai vào hạng đó — bug-hunt run 16:25).
        /// Dùng chung cho <see cref="GetTierPhonesAsync"/> và
        /// <see cref="GetTierCountAsync"/> để 2 luồng "liệt kê" và "đếm"
        /// không lệch mốc nhau.
        /// </summary>
        /// <param name="tier">Tên hạng ("Bạc" dùng ngưỡng Bạc, còn lại dùng ngưỡng Vàng).</param>
        /// <returns>(isSilver, mốc) hoặc null khi hạng không tồn tại.</returns>
        private async Task<(bool IsSilver, decimal Min)?> ResolveTierMinAsync(string tier)
        {
            var settings = await _shopSettings.GetAsync();
            var isSilver = string.Equals(tier, MemberTierNames.Silver, StringComparison.OrdinalIgnoreCase);
            var min = isSilver ? settings.TierSilverMin : settings.TierGoldMin;
            return min <= 0 ? null : (isSilver, min);
        }

        /// <summary>
        /// Đếm khách "không mua 30+ ngày" bằng SELECT COUNT — cùng điều kiện
        /// <see cref="GetInactivePhonesAsync"/> nhưng không nạp SĐT về memory.
        /// </summary>
        private async Task<int> GetInactiveCountAsync()
        {
            var activeNorms = await GetActivePhoneNormsAsync();

            var loyaltyQuery = await _loyaltyAccounts.GetQueryableAsync();
            return await _asyncExecuter.CountAsync(
                loyaltyQuery
                    .Where(x => !activeNorms.Contains(
                        x.Phone.Length <= PhoneNumber.TailDigitCount ? x.Phone : x.Phone.Substring(x.Phone.Length - PhoneNumber.TailDigitCount))));
        }

        /// <summary>
        /// Đếm khách theo hạng bằng SELECT COUNT — cùng điều kiện
        /// <see cref="GetTierPhonesAsync"/> nhưng không nạp SĐT về memory.
        /// </summary>
        private async Task<int> GetTierCountAsync(string tier)
        {
            var resolved = await ResolveTierMinAsync(tier);
            if (resolved == null)
            {
                return 0;
            }

            var (isSilver, min) = resolved.Value;
            var queryable = await _loyaltyAccounts.GetQueryableAsync();
            if (isSilver)
            {
                return await _asyncExecuter.CountAsync(
                    queryable.Where(x => x.TotalSpent >= min));
            }

            return await _asyncExecuter.CountAsync(
                queryable.Where(x => x.TotalSpent > min));
        }

        /// <summary>Đếm tổng số tài khoản loyalty (dùng hiển thị quy mô nhóm "tất cả khách").</summary>
        private async Task<int> GetAllCountAsync()
        {
            var queryable = await _loyaltyAccounts.GetQueryableAsync();
            return await _asyncExecuter.CountAsync(queryable);
        }

        /// <summary>Lấy toàn bộ SĐT có tài khoản loyalty (nhóm "tất cả khách").</summary>
        private async Task<List<string>> GetAllPhonesAsync()
        {
            var queryable = await _loyaltyAccounts.GetQueryableAsync();
            return await _asyncExecuter.ToListAsync(queryable.Select(x => x.Phone));
        }

        /// <summary>
        /// Sinh <paramref name="count"/> mã voucher duy nhất dạng "TIỀN TỐ + '-' + 6 ký tự".
        /// Trùng mã trong batch loại bằng HashSet; trùng với DB phát hiện bằng 1 query
        /// gộp mỗi lần thử (cắt chunk 1000 để dưới giới hạn tham số của SQL Server),
        /// chỉ sinh lại những mã bị trùng. Thử tối đa 20 lần rồi báo lỗi để không treo request.
        /// </summary>
        /// <param name="prefix">Tiền tố mã (đã chuẩn hóa chữ hoa).</param>
        /// <param name="count">Số mã cần sinh (= số người nhận).</param>
        /// <returns>Danh sách mã chưa từng tồn tại trong DB.</returns>
        /// <exception cref="DomainException">Ném khi sau 20 lần thử vẫn còn trùng mã.</exception>
        private async Task<List<string>> GenerateUniqueCodesAsync(string prefix, int count)
        {
            var codes = new List<string>(count);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                codes.Add(GenerateCandidate(prefix, used));
            }

            for (var attempt = 0; attempt < CodeMaxAttempts; attempt++)
            {
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var queryable = await _discountCodes.GetQueryableAsync();
                foreach (var chunk in codes.Chunk(CodeCheckChunkSize))
                {
                    var found = await _asyncExecuter.ToListAsync(
                        queryable.Where(x => chunk.Contains(x.Code)).Select(x => x.Code));
                    foreach (var code in found)
                    {
                        existing.Add(code);
                    }
                }

                if (existing.Count == 0)
                {
                    return codes;
                }

                for (var i = 0; i < codes.Count; i++)
                {
                    if (existing.Contains(codes[i]))
                    {
                        used.Remove(codes[i]);
                        codes[i] = GenerateCandidate(prefix, used);
                    }
                }
            }

            throw new DomainException("Không thể tạo mã voucher duy nhất, vui lòng thử lại.");
        }

        /// <summary>
        /// Sinh 1 mã ứng viên dạng "TIỀN TỐ-XXXXXX" chưa dùng trong batch này
        /// (loại trùng trong batch bằng HashSet; trùng với DB kiểm tra riêng).
        /// </summary>
        private static string GenerateCandidate(string prefix, HashSet<string> used)
        {
            string candidate;
            do
            {
                // codequality-team 2026-10-20 run-0245 Agent3-BestPractices: range
                // operator thay Substring(0, N) — ToString("N") dài 32 ký tự ≥ CodeSuffixLength.
                candidate = prefix + "-" + Guid.NewGuid().ToString("N")
                    [..CodeSuffixLength].ToUpperInvariant();
            } while (!used.Add(candidate));
            return candidate;
        }
    }
}
