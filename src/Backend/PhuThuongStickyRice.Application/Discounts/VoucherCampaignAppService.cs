using PhuThuongStickyRice.Application.Shop;

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

        public virtual async Task<List<VoucherCampaignAudienceDto>> GetAudiencesAsync(string tier)
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

        public virtual async Task<List<GeneratedVoucherDto>> GenerateAsync(CreateVoucherCampaignInput input)
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
            // perf-hunt 2026-10-19: generate all unique codes with a single
            // batched existence check instead of one DB round-trip per code.
            var codes = await GenerateUniqueCodesAsync(prefix, phones.Count);
            var result = new List<GeneratedVoucherDto>();

            for (var i = 0; i < phones.Count; i++)
            {
                await _discountCodes.InsertAsync(new DiscountCode
                {
                    Code = codes[i],
                    DiscountPercent = input.DiscountAmount > 0 ? (decimal?)null : input.DiscountPercent,
                    DiscountAmount = input.DiscountAmount > 0 ? input.DiscountAmount : (decimal?)null,
                    MinOrderAmount = 0,
                    ExpiresAt = expiresAt,
                    MaxUses = 1,
                    UsedCount = 0,
                    IsActive = true,
                    VoucherType = VoucherType.Discount,
                    OwnerPhone = phones[i]
                });
                result.Add(new GeneratedVoucherDto { Phone = phones[i], Code = codes[i] });
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

        /// <summary>
        /// Generates <paramref name="count"/> unique voucher codes.
        /// In-batch duplicates are excluded via a HashSet; DB collisions are
        /// detected with a single batched query per attempt (chunked at 1000
        /// to stay under SQL Server's parameter limit) and only colliding
        /// codes are regenerated. Semantics match the old per-code check.
        /// </summary>
        private async Task<List<string>> GenerateUniqueCodesAsync(string prefix, int count)
        {
            var codes = new List<string>(count);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                string code;
                do
                {
                    code = prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                } while (!used.Add(code));
                codes.Add(code);
            }

            for (var attempt = 0; attempt < 20; attempt++)
            {
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var queryable = await _discountCodes.GetQueryableAsync();
                foreach (var chunk in codes.Chunk(1000))
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
                        string replacement;
                        do
                        {
                            replacement = prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                        } while (!used.Add(replacement));
                        codes[i] = replacement;
                    }
                }
            }

            throw new DomainException("Không thể tạo mã voucher duy nhất, vui lòng thử lại.");
        }
    }
}
