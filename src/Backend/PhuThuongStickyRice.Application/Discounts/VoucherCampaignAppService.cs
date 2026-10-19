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
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Common;

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

        // Prefix mặc định cho mã voucher sinh tự động.
        private const string DefaultCodePrefix = "VC";

        // Độ dài phần hậu tố ngẫu nhiên của mã voucher (GUID rút gọn, chữ hoa).
        private const int CodeSuffixLength = 6;
        // Số lần thử tối đa khi sinh mã không trùng DB; quá giới hạn thì báo
        // lỗi thay vì treo request (DB gần như đầy mã).
        private const int CodeMaxAttempts = 20;
        // Kích thước chunk khi kiểm tra trùng mã với DB (dưới giới hạn tham số của SQL Server).
        private const int CodeCheckChunkSize = 1000;

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
            var inactive = await GetInactivePhonesAsync();
            var tierPhones = await GetTierPhonesAsync(tier);
            var allCount = await GetAllCountAsync();

            return new List<VoucherCampaignAudienceDto>
            {
                new VoucherCampaignAudienceDto { Key = AudienceInactive30, Name = "Không mua 30+ ngày", RecipientCount = inactive.Count },
                new VoucherCampaignAudienceDto { Key = AudienceTier, Name = $"Hạng {tier ?? "Vàng"}", RecipientCount = tierPhones.Count },
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

            // Chỉ lọc ký tự chữ/số và viết hoa tiền tố để mã voucher gọn, dễ đọc,
            // tránh SĐT/ký tự đặc biệt lọt vào mã.
            var prefix = string.IsNullOrWhiteSpace(input.Prefix)
                ? DefaultCodePrefix
                : new string(input.Prefix.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(prefix))
            {
                prefix = DefaultCodePrefix;
            }

            var expiresAt = DateTime.UtcNow.AddDays(input.ValidDays);
            // perf-hunt 2026-10-19: generate all unique codes with a single
            // batched existence check instead of one DB round-trip per code.
            var codes = await GenerateUniqueCodesAsync(prefix, phones.Count);
            var result = new List<GeneratedVoucherDto>();

            for (var i = 0; i < phones.Count; i++)
            {
                await _discountCodes.InsertAsync(new DiscountCode
                {
                    Code = codes[i],
                    // Ưu tiên số tiền cố định khi có; % và số tiền không tồn tại song song
                    // vì checkout chỉ áp dụng 1 trong 2.
                    DiscountPercent = input.DiscountAmount > 0 ? (decimal?)null : input.DiscountPercent,
                    DiscountAmount = input.DiscountAmount > 0 ? input.DiscountAmount : (decimal?)null,
                    MinOrderAmount = 0,
                    ExpiresAt = expiresAt,
                    // Mỗi mã chỉ dùng 1 lần và gắn với đúng SĐT nhận, để không chia sẻ/tái sử dụng.
                    MaxUses = 1,
                    UsedCount = 0,
                    IsActive = true,
                    VoucherType = VoucherType.Discount,
                    OwnerPhone = phones[i]
                });
                result.Add(new GeneratedVoucherDto { Phone = phones[i], Code = codes[i] });
            }

            // ABP unit of work tự commit toàn bộ batch khi method kết thúc (không cần SaveChanges thủ công).
            return result;
        }

        /// <summary>
        /// Lấy SĐT khách "không mua 30+ ngày": có tài khoản loyalty nhưng không có
        /// đơn nào (trừ đơn đã hủy) trong 30 ngày gần nhất.
        /// </summary>
        private async Task<List<string>> GetInactivePhonesAsync()
        {
            // Khách "không mua 30+ ngày": có tài khoản loyalty nhưng không có
            // đơn nào (trừ đơn đã hủy) trong 30 ngày gần nhất.
            var since = DateTime.UtcNow.AddDays(-30);
            var orderQuery = await _orders.GetQueryableAsync();
            var activePhones = await _asyncExecuter.ToListAsync(
                orderQuery.Where(x => x.CreatedAt >= since && x.Status != OrderStatus.Cancelled)
                    .Select(x => x.CustomerPhone)
                    .Distinct());

            var loyaltyQuery = await _loyaltyAccounts.GetQueryableAsync();
            var all = await _asyncExecuter.ToListAsync(loyaltyQuery.Select(x => x.Phone));

            var activeSet = new HashSet<string>(activePhones.Select(p => VietnamTime.NormalizePhone(p) ?? p));
            return all.Where(p => !activeSet.Contains(p)).ToList();
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
            var settings = await _shopSettings.GetAsync();
            var isSilver = string.Equals(tier, "Bạc", StringComparison.OrdinalIgnoreCase);
            var min = isSilver ? settings.TierSilverMin : settings.TierGoldMin;

            // Khớp GetTier (bug-hunt run 16:25): bản cũ dùng >= cho cả Vàng
            // và thiếu guard mốc <= 0 → khách chi tiêu ĐÚNG bằng mốc Vàng bị
            // tính Bạc ở checkout (hưởng % giảm thấp hơn) nhưng vẫn nhận
            // voucher chiến dịch "hạng Vàng"; tệ hơn mốc = 0 sẽ nhắm toàn bộ
            // tài khoản trong khi GetTier không xếp ai vào hạng đó.
            if (min <= 0)
            {
                return new List<string>();
            }

            var queryable = await _loyaltyAccounts.GetQueryableAsync();
            if (isSilver)
            {
                return await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.TotalSpent >= min).Select(x => x.Phone));
            }

            return await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.TotalSpent > min).Select(x => x.Phone));
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
                candidate = prefix + "-" + Guid.NewGuid().ToString("N")
                    .Substring(0, CodeSuffixLength).ToUpperInvariant();
            } while (!used.Add(candidate));
            return candidate;
        }
    }
}
