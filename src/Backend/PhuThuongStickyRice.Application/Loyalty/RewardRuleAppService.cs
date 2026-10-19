using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;

using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Loyalty;

namespace PhuThuongStickyRice.Application.Loyalty
{
    /// <summary>
    /// Item 92: automatic rewards evaluated when an order is completed.
    /// FreeTopping appends a note on the order; DiscountCode generates a real
    /// single-use voucher owned by the customer's phone and records it on the
    /// order notes so the admin can tell the customer.
    /// <para>
    /// Hợp đồng quy tắc thưởng tự động: admin CRUD các quy tắc; order service
    /// gọi <c>ApplyOnOrderCompletedAsync</c> ngay sau khi đơn hoàn tất.
    /// </para>
    /// </summary>
    public interface IRewardRuleAppService : IApplicationService
    {
        /// <summary>Admin: liệt kê quy tắc thưởng theo ngưỡng tiền đơn tăng dần.</summary>
        /// <returns>Danh sách quy tắc thưởng.</returns>
        Task<List<OrderRewardRuleDto>> GetListAsync();

        /// <summary>Admin: tạo quy tắc thưởng mới.</summary>
        /// <param name="input">Tên, ngưỡng tiền đơn, loại thưởng, topping/% giảm.</param>
        /// <returns>Quy tắc vừa tạo.</returns>
        Task<OrderRewardRuleDto> CreateAsync(CreateOrderRewardRuleDto input);

        /// <summary>Admin: cập nhật quy tắc thưởng.</summary>
        /// <param name="id">Id quy tắc.</param>
        /// <param name="input">Thông tin mới của quy tắc.</param>
        /// <returns>Quy tắc sau khi cập nhật.</returns>
        Task<OrderRewardRuleDto> UpdateAsync(Guid id, CreateOrderRewardRuleDto input);

        /// <summary>Admin: xóa quy tắc thưởng (không ảnh hưởng đơn đã được thưởng trước đó).</summary>
        /// <param name="id">Id quy tắc cần xóa.</param>
        Task DeleteAsync(Guid id);

        /// <summary>Called by the order service right after an order is completed.</summary>
        Task ApplyOnOrderCompletedAsync(Order order);
    }

    /// <summary>
    /// Item 92: thưởng tự động khi đơn hoàn tất. Hai loại thưởng:
    /// <list type="bullet">
    /// <item><c>FreeTopping</c>: chỉ ghi chú lên đơn để bếp/quán biết tặng kèm topping.</item>
    /// <item><c>DiscountCode</c>: sinh 1 mã giảm giá dùng 1 lần, gắn đúng SĐT khách
    /// (<c>OwnerPhone</c>) cho lần mua sau, và ghi chú lên đơn để admin thông báo.</item>
    /// </list>
    /// Admin quản lý quy tắc; logic chạy qua <c>ApplyOnOrderCompletedAsync</c>
    /// từ phía order service (AllowAnonymous nội bộ).
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class RewardRuleAppService : ApplicationService, IRewardRuleAppService
    {
        private readonly IRepository<OrderRewardRule, Guid> _rules;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IDiscountAppService _discounts;

        // [codequality-smell 2026-10-19] Magic number: mặc định của loại
        // thưởng DiscountCode — đặt hằng số có tên để thay đổi 1 chỗ.
        private const decimal DefaultRewardDiscountPercent = 5;
        private const int RewardVoucherValidDays = 30;

        public RewardRuleAppService(
            IRepository<OrderRewardRule, Guid> rules,
            IAsyncQueryableExecuter asyncExecuter,
            IDiscountAppService discounts)
        {
            _rules = rules;
            _asyncExecuter = asyncExecuter;
            _discounts = discounts;
        }

        /// <summary>
        /// Liệt kê quy tắc thưởng đang có, sắp xếp theo ngưỡng tiền đơn tăng dần.
        /// </summary>
        /// <returns>Danh sách quy tắc thưởng dưới dạng DTO.</returns>
        public virtual async Task<List<OrderRewardRuleDto>> GetListAsync()
        {
            var queryable = await _rules.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.MinAmount));
            return items.Select(Map).ToList();
        }

        /// <summary>
        /// Tạo quy tắc thưởng mới (kiểm tra loại thưởng và trường bắt buộc theo loại).
        /// </summary>
        /// <param name="input">Thông tin quy tắc: tên, ngưỡng tiền đơn, loại thưởng, topping/% giảm.</param>
        /// <returns>Quy tắc vừa tạo.</returns>
        public virtual async Task<OrderRewardRuleDto> CreateAsync(CreateOrderRewardRuleDto input)
        {
            var rule = Build(input);
            await _rules.InsertAsync(rule, autoSave: true);
            return Map(rule);
        }

        /// <summary>
        /// Cập nhật quy tắc thưởng (validate lại toàn bộ điều kiện như lúc tạo).
        /// </summary>
        /// <param name="id">Id quy tắc.</param>
        /// <param name="input">Thông tin mới của quy tắc.</param>
        /// <returns>Quy tắc sau khi cập nhật.</returns>
        public virtual async Task<OrderRewardRuleDto> UpdateAsync(Guid id, CreateOrderRewardRuleDto input)
        {
            var rule = await _rules.GetAsync(id);
            Apply(rule, input);
            await _rules.UpdateAsync(rule, autoSave: true);
            return Map(rule);
        }

        /// <summary>
        /// Xóa quy tắc thưởng. Không ảnh hưởng các đơn đã được áp thưởng trước đó
        /// (ghi chú quà đã nằm trong đơn).
        /// </summary>
        /// <param name="id">Id quy tắc cần xóa.</param>
        public virtual async Task DeleteAsync(Guid id)
        {
            await _rules.DeleteAsync(id, autoSave: true);
        }

        /// <summary>
        /// Áp toàn bộ quy tắc thưởng đủ điều kiện cho 1 đơn vừa hoàn tất.
        /// Chạy nội bộ từ order service (AllowAnonymous vì caller không phải request admin
        /// trực tiếp), không gọi từ bên ngoài.
        /// </summary>
        /// <param name="order">Đơn hàng vừa hoàn tất; null thì bỏ qua.</param>
        /// <returns>Task hoàn tất khi mọi quy tắc đã được xét.</returns>
        [AllowAnonymous]
        public virtual async Task ApplyOnOrderCompletedAsync(Order order)
        {
            if (order == null)
            {
                return;
            }

            var queryable = await _rules.GetQueryableAsync();
            var rules = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive && x.MinAmount <= order.TotalAmount.Amount)
                    .OrderBy(x => x.MinAmount));

            // perf-hunt 2026-10-20: gom các rule DiscountCode rồi tạo mã 1 lần
            // bằng CreateGeneratedBatchAsync — trước đây mỗi rule là 1 lần
            // GenerateUniqueCodeAsync + InsertAsync(autoSave: true) + 1 lần
            // _catalogCache.Invalidate() toàn bộ cache public (products,
            // ratings, FAQs, promos...) trên đường hoàn tất đơn. Ghi chú quà
            // vẫn theo đúng thứ tự rule như cũ (kể cả khi topping và mã giảm
            // đan xen nhau).
            var ownerPhone = NormalizePhone(order.CustomerPhone);
            var discountPercents = new List<decimal>();
            foreach (var rule in rules)
            {
                // Mặc định 5% nếu rule không đặt — mã dùng 1 lần, gắn đúng SĐT khách
                // và có hạn 30 ngày, để khách quay lại mua lần sau.
                if (rule.RewardType == OrderRewardType.DiscountCode)
                {
                    discountPercents.Add(rule.DiscountPercent ?? DefaultRewardDiscountPercent);
                }
            }

            var generatedCodes = discountPercents.Count > 0
                ? await _discounts.CreateGeneratedBatchAsync(
                    discountPercents.Select(percent => new CreateGeneratedDiscountDto
                    {
                        Prefix = "THUONG",
                        DiscountPercent = percent,
                        ExpiresAt = DateTime.UtcNow.AddDays(RewardVoucherValidDays),
                        OwnerPhone = ownerPhone
                    }).ToList())
                : new List<DiscountCodeDto>();

            var discountIndex = 0;
            foreach (var rule in rules)
            {
                switch (rule.RewardType)
                {
                    case OrderRewardType.FreeTopping:
                        // Tặng topping chỉ là ghi chú trên đơn để nhân viên/bếp thực hiện,
                        // không trừ tiền hay tạo mã gì thêm.
                        var topping = string.IsNullOrWhiteSpace(rule.ToppingName)
                            ? "topping miễn phí"
                            : rule.ToppingName.Trim();
                        order.AddRewardNote($"[Quà tặng] Tặng {topping} cho đơn hàng từ {rule.MinAmount:N0} ₫.");
                        break;

                    case OrderRewardType.DiscountCode:
                        var percent = discountPercents[discountIndex];
                        order.AddRewardNote(
                            $"[Quà tặng] Mã giảm {percent:N0}% cho lần sau: {generatedCodes[discountIndex].Code} (áp dụng cho SĐT {order.CustomerPhone}).");
                        discountIndex++;
                        break;
                }
            }
        }

        /// <summary>Tạo entity quy tắc thưởng mới từ dữ liệu nhập (có validate theo loại thưởng).</summary>
        /// <param name="input">Dữ liệu tạo quy tắc.</param>
        /// <returns>Entity quy tắc mới.</returns>
        private static OrderRewardRule Build(CreateOrderRewardRuleDto input)
        {
            var rule = new OrderRewardRule();
            Apply(rule, input);
            return rule;
        }

        /// <summary>
        /// Ghi dữ liệu nhập vào entity quy tắc, validate bắt buộc theo loại thưởng:
        /// <c>FreeTopping</c> phải có tên topping; <c>DiscountCode</c> phải có % giảm &gt; 0.
        /// Dùng chung cho cả tạo mới và cập nhật để 2 luồng không lệch nhau.
        /// </summary>
        /// <param name="rule">Entity quy tắc cần cập nhật.</param>
        /// <param name="input">Dữ liệu nhập.</param>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi loại thưởng không hợp lệ hoặc thiếu trường bắt buộc theo loại.</exception>
        private static void Apply(OrderRewardRule rule, CreateOrderRewardRuleDto input)
        {
            if (!Enum.TryParse<OrderRewardType>(input.RewardType, ignoreCase: true, out var type))
            {
                throw new Domain.Exceptions.DomainException($"Loại thưởng '{input.RewardType}' không hợp lệ.");
            }

            if (type == OrderRewardType.FreeTopping && string.IsNullOrWhiteSpace(input.ToppingName))
            {
                throw new Domain.Exceptions.DomainException("Vui lòng nhập tên topping tặng kèm.");
            }

            if (type == OrderRewardType.DiscountCode
                && (!input.DiscountPercent.HasValue || input.DiscountPercent.Value <= 0))
            {
                throw new Domain.Exceptions.DomainException("Vui lòng nhập % giảm giá của mã tặng.");
            }

            rule.Name = input.Name.Trim();
            rule.MinAmount = Math.Max(0, input.MinAmount);
            rule.RewardType = type;
            rule.ToppingName = string.IsNullOrWhiteSpace(input.ToppingName) ? null : input.ToppingName.Trim();
            rule.DiscountPercent = input.DiscountPercent;
            rule.IsActive = input.IsActive;
        }

        /// <summary>Chuyển entity <c>OrderRewardRule</c> sang DTO.</summary>
        /// <param name="r">Entity quy tắc thưởng.</param>
        /// <returns>DTO quy tắc thưởng.</returns>
        private static OrderRewardRuleDto Map(OrderRewardRule r) => new OrderRewardRuleDto
        {
            Id = r.Id,
            Name = r.Name,
            MinAmount = r.MinAmount,
            RewardType = r.RewardType.ToString(),
            ToppingName = r.ToppingName,
            DiscountPercent = r.DiscountPercent,
            IsActive = r.IsActive
        };

        /// <summary>
        /// Chuẩn hóa SĐT khách trước khi gắn vào mã thưởng: chỉ giữ chữ số, đổi
        /// đầu 84 (11 số) về dạng 0xxx — để mã gắn đúng 1 định dạng SĐT duy nhất
        /// khi kiểm tra chủ sở hữu lúc dùng mã.
        /// </summary>
        /// <param name="phone">SĐT thô.</param>
        /// <returns>SĐT đã chuẩn hóa.</returns>
        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return phone;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            // So sánh lập trình trên chuỗi chữ số: Ordinal đúng + nhanh hơn
            // overload mặc định (culture-sensitive). Giữ nguyên behavior ASCII.

            // CA1845: span-based Concat thay cho Substring — không cấp phát chuỗi trung gian.
            return digits.StartsWith("84", StringComparison.Ordinal) && digits.Length == 11 ? string.Concat("0".AsSpan(), digits.AsSpan(2)) : digits;
        }
    }
}
