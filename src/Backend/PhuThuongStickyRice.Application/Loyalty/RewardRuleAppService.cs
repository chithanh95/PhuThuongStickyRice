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
using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Application.Loyalty;

namespace PhuThuongStickyRice.Application.Loyalty
{
    /// <summary>
    /// Item 92: automatic rewards evaluated when an order is completed.
    /// FreeTopping appends a note on the order; DiscountCode generates a real
    /// single-use voucher owned by the customer's phone and records it on the
    /// order notes so the admin can tell the customer.
    /// </summary>
    public interface IRewardRuleAppService : IApplicationService
    {
        Task<List<OrderRewardRuleDto>> GetListAsync();

        Task<OrderRewardRuleDto> CreateAsync(CreateOrderRewardRuleDto input);

        Task<OrderRewardRuleDto> UpdateAsync(Guid id, CreateOrderRewardRuleDto input);

        Task DeleteAsync(Guid id);

        /// <summary>Called by the order service right after an order is completed.</summary>
        Task ApplyOnOrderCompletedAsync(Order order);
    }

    [Authorize(Roles = "Admin")]
    public class RewardRuleAppService : ApplicationService, IRewardRuleAppService
    {
        private readonly IRepository<OrderRewardRule, Guid> _rules;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IDiscountAppService _discounts;

        public RewardRuleAppService(
            IRepository<OrderRewardRule, Guid> rules,
            IAsyncQueryableExecuter asyncExecuter,
            IDiscountAppService discounts)
        {
            _rules = rules;
            _asyncExecuter = asyncExecuter;
            _discounts = discounts;
        }

        public virtual async Task<List<OrderRewardRuleDto>> GetListAsync()
        {
            var queryable = await _rules.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.MinAmount));
            return items.Select(Map).ToList();
        }

        public virtual async Task<OrderRewardRuleDto> CreateAsync(CreateOrderRewardRuleDto input)
        {
            var rule = Build(input);
            await _rules.InsertAsync(rule, autoSave: true);
            return Map(rule);
        }

        public virtual async Task<OrderRewardRuleDto> UpdateAsync(Guid id, CreateOrderRewardRuleDto input)
        {
            var rule = await _rules.GetAsync(id);
            Apply(rule, input);
            await _rules.UpdateAsync(rule, autoSave: true);
            return Map(rule);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _rules.DeleteAsync(id, autoSave: true);
        }

        /// <summary>Internal: runs outside the Admin policy via the order service.</summary>
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

            foreach (var rule in rules)
            {
                switch (rule.RewardType)
                {
                    case OrderRewardType.FreeTopping:
                        var topping = string.IsNullOrWhiteSpace(rule.ToppingName)
                            ? "topping miễn phí"
                            : rule.ToppingName.Trim();
                        order.AddRewardNote($"[Quà tặng] Tặng {topping} cho đơn hàng từ {rule.MinAmount:N0} ₫.");
                        break;

                    case OrderRewardType.DiscountCode:
                        var percent = rule.DiscountPercent ?? 5;
                        var code = await _discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
                        {
                            Prefix = "THUONG",
                            DiscountPercent = percent,
                            ExpiresAt = DateTime.UtcNow.AddDays(30),
                            OwnerPhone = NormalizePhone(order.CustomerPhone)
                        });
                        order.AddRewardNote(
                            $"[Quà tặng] Mã giảm {percent:N0}% cho lần sau: {code.Code} (áp dụng cho SĐT {order.CustomerPhone}).");
                        break;
                }
            }
        }

        private static OrderRewardRule Build(CreateOrderRewardRuleDto input)
        {
            var rule = new OrderRewardRule();
            Apply(rule, input);
            return rule;
        }

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

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return phone;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.StartsWith("84") && digits.Length == 11 ? "0" + digits.Substring(2) : digits;
        }
    }
}
