using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Shop
{
    public interface IPrepaidCardAppService : IApplicationService
    {
        /// <summary>Public: customer buys a prepaid card package.</summary>
        Task<PrepaidCardDto> BuyAsync(BuyPrepaidCardDto dto);

        /// <summary>Public: list cards of a phone number, newest first.</summary>
        Task<List<PrepaidCardDto>> GetMyCardsAsync(string phone);

        /// <summary>Admin: paged card list, search by name/phone/code.</summary>
        Task<List<PrepaidCardDto>> GetListAsync(string searchTerm, int skip = 0, int take = 20);

        /// <summary>Admin: redeem one suit from a card code.</summary>
        Task<PrepaidCardDto> RedeemAsync(string cardCode);
    }

    /// <summary>Item 164: Thẻ xôi sáng trả trước (gói 20/30 suất).</summary>
    public class PrepaidCardAppService : ApplicationService, IPrepaidCardAppService
    {
        public const decimal BaseSuitPrice = 25000m;

        private static readonly Regex VnPhoneRegex = new(@"^(0)(3|5|7|8|9)\d{8}$", RegexOptions.Compiled);

        private readonly IRepository<PrepaidCard, Guid> _cards;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public PrepaidCardAppService(
            IRepository<PrepaidCard, Guid> cards,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _cards = cards;
            _asyncExecuter = asyncExecuter;
        }

        [AllowAnonymous]
        public virtual async Task<PrepaidCardDto> BuyAsync(BuyPrepaidCardDto dto)
        {
            var name = (dto?.CustomerName ?? string.Empty).Trim();
            var phone = (dto?.CustomerPhone ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException("Vui lòng nhập họ tên.");
            }

            if (!VnPhoneRegex.IsMatch(phone))
            {
                throw new DomainException("Số điện thoại không hợp lệ.");
            }

            decimal paidAmount;
            if (dto.PackageSuits == 20)
            {
                paidAmount = 20 * BaseSuitPrice * 0.90m; // 450_000đ
            }
            else if (dto.PackageSuits == 30)
            {
                paidAmount = 30 * BaseSuitPrice * 0.85m; // 637_500đ
            }
            else
            {
                throw new DomainException("Chỉ có gói 20 hoặc 30 suất.");
            }

            var cardCode = await GenerateUniqueCodeAsync();

            var card = new PrepaidCard
            {
                CustomerName = name,
                CustomerPhone = phone,
                CardCode = cardCode,
                TotalSuits = dto.PackageSuits,
                UsedSuits = 0,
                PaidAmount = paidAmount,
                Status = PrepaidCardStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _cards.InsertAsync(card, autoSave: true);

            return MapToDto(card);
        }

        [AllowAnonymous]
        public virtual async Task<List<PrepaidCardDto>> GetMyCardsAsync(string phone)
        {
            var normalized = (phone ?? string.Empty).Trim();
            var query = (await _cards.GetQueryableAsync())
                .Where(c => c.CustomerPhone == normalized)
                .OrderByDescending(c => c.CreatedAt);

            var cards = await _asyncExecuter.ToListAsync(query);
            return cards.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<PrepaidCardDto>> GetListAsync(string searchTerm, int skip = 0, int take = 20)
        {
            var query = await _cards.GetQueryableAsync();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(c =>
                    c.CustomerName.Contains(term) ||
                    c.CustomerPhone.Contains(term) ||
                    c.CardCode.Contains(term));
            }

            var cards = await _asyncExecuter.ToListAsync(
                query.OrderByDescending(c => c.CreatedAt).Skip(skip).Take(take));

            return cards.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<PrepaidCardDto> RedeemAsync(string cardCode)
        {
            var code = (cardCode ?? string.Empty).Trim().ToUpperInvariant();

            var card = await _cards.FirstOrDefaultAsync(
                c => c.CardCode == code && c.Status == PrepaidCardStatus.Active);

            if (card == null)
            {
                throw new DomainException("Không tìm thấy thẻ.");
            }

            if (card.UsedSuits >= card.TotalSuits)
            {
                throw new DomainException("Thẻ đã hết suất.");
            }

            card.UsedSuits++;

            if (card.UsedSuits >= card.TotalSuits)
            {
                card.Status = PrepaidCardStatus.UsedUp;
                card.CompletedAt = DateTime.UtcNow;
            }

            await _cards.UpdateAsync(card, autoSave: true);

            return MapToDto(card);
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var code = "TX" + Random.Shared.Next(0, 999999).ToString("D6");
                var exists = await _cards.FirstOrDefaultAsync(c => c.CardCode == code);
                if (exists == null)
                {
                    return code;
                }
            }

            throw new DomainException("Không sinh được mã thẻ, vui lòng thử lại.");
        }

        private static PrepaidCardDto MapToDto(PrepaidCard card)
        {
            return new PrepaidCardDto
            {
                Id = card.Id,
                CardCode = card.CardCode,
                CustomerName = card.CustomerName,
                CustomerPhone = card.CustomerPhone,
                TotalSuits = card.TotalSuits,
                UsedSuits = card.UsedSuits,
                RemainingSuits = card.TotalSuits - card.UsedSuits,
                PaidAmount = card.PaidAmount,
                Status = card.Status,
                CreatedAt = card.CreatedAt
            };
        }
    }
}
