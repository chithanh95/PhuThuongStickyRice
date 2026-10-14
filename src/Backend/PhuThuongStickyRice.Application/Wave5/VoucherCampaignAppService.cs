using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Wave5.DTOs;
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

namespace PhuThuongStickyRice.Application.Wave5
{
    public interface IVoucherCampaignAppService : IApplicationService
    {
        /// <summary>Admin: audience sizes for the campaign picker.</summary>
        Task<List<VoucherCampaignAudienceDto>> GetAudiencesAsync(string tier);

        /// <summary>Admin: generates one personal DiscountCode per recipient phone.</summary>
        Task<List<GeneratedVoucherDto>> GenerateAsync(CreateVoucherCampaignInput input);
    }

    /// <summary>
    /// Item 132: voucher campaign. Bulk sending is deferred — sending needs
    /// the Zalo OA / SMS channel; the result list is meant to be copied or
    /// pasted into the shop's existing broadcast tool.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class VoucherCampaignAppService : ApplicationService, IVoucherCampaignAppService
    {
        private readonly IRepository<LoyaltyAccount, Guid> _loyaltyAccounts;
        private readonly IRepository<Order, Guid> _orders;
        private readonly IRepository<DiscountCode, Guid> _discountCodes;
        private readonly IShopSettingAppService _shopSettings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

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

        public async Task<List<VoucherCampaignAudienceDto>> GetAudiencesAsync(string tier)
        {
            var inactive = await GetInactivePhonesAsync();
            var tierPhones = await GetTierPhonesAsync(tier);
            var allCount = await GetAllCountAsync();

            return new List<VoucherCampaignAudienceDto>
            {
                new VoucherCampaignAudienceDto { Key = "inactive30", Name = "Không mua 30+ ngày", RecipientCount = inactive.Count },
                new VoucherCampaignAudienceDto { Key = "tier", Name = $"Hạng {tier ?? "Vàng"}", RecipientCount = tierPhones.Count },
                new VoucherCampaignAudienceDto { Key = "all", Name = "Tất cả khách", RecipientCount = allCount }
            };
        }

        public async Task<List<GeneratedVoucherDto>> GenerateAsync(CreateVoucherCampaignInput input)
        {
            List<string> phones;
            switch ((input.Audience ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "inactive30":
                    phones = await GetInactivePhonesAsync();
                    break;
                case "tier":
                    phones = await GetTierPhonesAsync(input.Tier);
                    break;
                case "all":
                    phones = await GetAllPhonesAsync();
                    break;
                default:
                    throw new DomainException("Đối tượng chiến dịch không hợp lệ.");
            }

            if (phones.Count == 0)
            {
                throw new DomainException("Không có khách hàng nào trong đối tượng đã chọn.");
            }

            var prefix = string.IsNullOrWhiteSpace(input.Prefix)
                ? "VC"
                : new string(input.Prefix.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(prefix))
            {
                prefix = "VC";
            }

            var expiresAt = DateTime.UtcNow.AddDays(input.ValidDays);
            var result = new List<GeneratedVoucherDto>();

            foreach (var phone in phones)
            {
                var code = await GenerateUniqueCodeAsync(prefix);
                await _discountCodes.InsertAsync(new DiscountCode
                {
                    Code = code,
                    DiscountPercent = input.DiscountAmount > 0 ? (decimal?)null : input.DiscountPercent,
                    DiscountAmount = input.DiscountAmount > 0 ? input.DiscountAmount : (decimal?)null,
                    MinOrderAmount = 0,
                    ExpiresAt = expiresAt,
                    MaxUses = 1,
                    UsedCount = 0,
                    IsActive = true,
                    VoucherType = VoucherType.Discount,
                    OwnerPhone = phone
                });
                result.Add(new GeneratedVoucherDto { Phone = phone, Code = code });
            }

            // The ambient ABP unit of work commits the batch on completion.
            return result;
        }

        private async Task<List<string>> GetInactivePhonesAsync()
        {
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

        private async Task<List<string>> GetTierPhonesAsync(string tier)
        {
            var settings = await _shopSettings.GetAsync();
            var min = string.Equals(tier, "Bạc", StringComparison.OrdinalIgnoreCase)
                ? settings.TierSilverMin
                : settings.TierGoldMin;

            var queryable = await _loyaltyAccounts.GetQueryableAsync();
            return await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.TotalSpent >= min).Select(x => x.Phone));
        }

        private async Task<int> GetAllCountAsync()
        {
            var queryable = await _loyaltyAccounts.GetQueryableAsync();
            return await _asyncExecuter.CountAsync(queryable);
        }

        private async Task<List<string>> GetAllPhonesAsync()
        {
            var queryable = await _loyaltyAccounts.GetQueryableAsync();
            return await _asyncExecuter.ToListAsync(queryable.Select(x => x.Phone));
        }

        private async Task<string> GenerateUniqueCodeAsync(string prefix)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var code = prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                var queryable = await _discountCodes.GetQueryableAsync();
                var exists = await _asyncExecuter.AnyAsync(queryable.Where(x => x.Code == code));
                if (!exists)
                {
                    return code;
                }
            }

            throw new DomainException("Không thể tạo mã voucher duy nhất, vui lòng thử lại.");
        }
    }
}
