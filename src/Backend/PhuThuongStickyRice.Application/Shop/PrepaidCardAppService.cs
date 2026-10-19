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

    /// <summary>
    /// Item 164: Thẻ xôi sáng trả trước (gói 20/30 suất).
    /// Khách mua gói suất ăn sáng trả trước với giá ưu đãi; mỗi lần đến quán
    /// nhân viên nhập mã thẻ để trừ 1 suất. Thẻ hết suất tự chuyển trạng thái đã dùng hết.
    /// </summary>
    public class PrepaidCardAppService : ApplicationService, IPrepaidCardAppService
    {
        /// <summary>Giá niêm yết 1 suất xôi (25.000đ) — làm gốc tính giá gói thẻ.</summary>
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

        /// <summary>
        /// Public: khách mua một gói thẻ trả trước. Validate tên/SĐT và gói hợp lệ,
        /// tính giá gói theo mức chiết khấu, sinh mã thẻ duy nhất rồi lưu.
        /// </summary>
        /// <param name="dto">Tên, SĐT khách và gói suất (chỉ chấp nhận 20 hoặc 30).</param>
        /// <returns>Thông tin thẻ vừa tạo (kèm mã thẻ để khách giữ).</returns>
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
                // Gói 20 suất: giảm 10% so với giá niêm yết (450.000đ) — khuyến khích mua thử.
                paidAmount = 20 * BaseSuitPrice * 0.90m; // 450_000đ
            }
            else if (dto.PackageSuits == 30)
            {
                // Gói 30 suất: giảm 15% (637.500đ) — ưu đãi sâu hơn để giữ khách ăn dài ngày.
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

        /// <summary>
        /// Public: liệt kê các thẻ của một SĐT, mới nhất trước (khách tra cứu thẻ của mình).
        /// </summary>
        /// <param name="phone">SĐT đã đăng ký thẻ.</param>
        [AllowAnonymous]
        public virtual async Task<List<PrepaidCardDto>> GetMyCardsAsync(string phone)
        {
            var normalized = (phone ?? string.Empty).Trim();
            var query = (await _cards.GetQueryableAsync())
                .Where(c => c.CustomerPhone == normalized)
                .OrderByDescending(c => c.CreatedAt)
                // perf-hunt 2026-10-19: cap server-side — thẻ không bao giờ xóa nên
                // danh sách theo SĐT tăng vô hạn; 200 thẻ mới nhất là quá đủ cho tra cứu.
                .Take(200);

            var cards = await _asyncExecuter.ToListAsync(query);
            return cards.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: danh sách thẻ có phân trang, tìm kiếm theo tên/SĐT/mã thẻ.
        /// </summary>
        /// <param name="searchTerm">Từ khóa tìm kiếm (tùy chọn).</param>
        /// <param name="skip">Số dòng bỏ qua (phân trang).</param>
        /// <param name="take">Số dòng lấy, mặc định 20.</param>
        [Authorize(Roles = "Admin")]
        /// <summary>
        /// Admin: danh sách thẻ (tìm theo tên/SĐT/mã thẻ), phân trang.
        /// perf-hunt 2026-10-19: chặn `take` tối đa 200 ở server — tham số do
        /// client truyền, không chặn thì 1 request có thể kéo toàn bộ bảng.
        /// </summary>
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
                query.OrderByDescending(c => c.CreatedAt)
                    .Skip(Math.Max(0, skip))
                    .Take(Math.Clamp(take, 1, 200)));

            return cards.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: trừ 1 suất khỏi thẻ theo mã thẻ (nhân viên quầy thực hiện khi khách đến ăn).
        /// Mã thẻ so sánh không phân biệt hoa/thường để nhân viên nhập nhanh không bị lỗi.
        /// Khi hết suất, thẻ tự chuyển trạng thái "đã dùng hết" và không trừ thêm được nữa.
        /// </summary>
        /// <param name="cardCode">Mã thẻ (ví dụ TX482193).</param>
        /// <returns>Thông tin thẻ sau khi trừ suất.</returns>
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

        /// <summary>
        /// Sinh mã thẻ duy nhất dạng "TX" + 6 chữ số (TX = Thẻ Xôi).
        /// Thử tối đa 5 lần: với 1 triệu mã khả dụng, xác suất trùng sau 5 lần là
        /// không đáng kể; quá 5 lần thì báo lỗi để khách thử lại thay vì treo request.
        /// </summary>
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
