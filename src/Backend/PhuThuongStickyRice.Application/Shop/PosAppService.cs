using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;

using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Shop
{
    public interface IPosAppService : IApplicationService
    {
        /// <summary>
        /// Creates a walk-in POS order: COD, pickup, paid immediately and
        /// Completed in one step (loyalty points still accrue on completion).
        /// </summary>
        Task<OrderDto> CreateOrderAsync(CreatePosOrderInput input);
    }

    /// <summary>Item 127: counter POS for Admin/Cashier.</summary>
    [Authorize(Roles = "Admin,Cashier")]
    public class PosAppService : ApplicationService, IPosAppService
    {
        private readonly IOrderAppService _orders;

        public PosAppService(IOrderAppService orders)
        {
            _orders = orders;
        }

        public virtual async Task<OrderDto> CreateOrderAsync(CreatePosOrderInput input)
        {
            if (input?.Lines == null || input.Lines.Count == 0)
            {
                throw new DomainException("POS: chưa chọn món nào.");
            }

            var phone = VietnamTime.NormalizePhone(input.CustomerPhone);
            var order = await _orders.CreateAsync(new CreateOrderDto
            {
                CustomerName = string.IsNullOrWhiteSpace(input.CustomerName)
                    ? "Khách vãng lai"
                    : input.CustomerName.Trim(),
                CustomerPhone = phone ?? "0000000000",
                CustomerAddress = null,
                Notes = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim(),
                PaymentMethod = "Cod",
                FulfillmentMethod = "Pickup",
                Source = "POS",
                Lines = input.Lines
                    .Where(x => x.Quantity > 0)
                    .Select(x => new CreateOrderLineDto
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        SizeOptionId = x.SizeOptionId,
                        ToppingIds = x.ToppingIds ?? new List<System.Guid>()
                    })
                    .ToList()
            }, skipRecaptchaCheck: true);

            if (order.Lines == null || order.Lines.Count == 0)
            {
                throw new DomainException("POS: chưa chọn món nào.");
            }

            // Walk-in: cash collected at the counter, order fulfilled immediately.
            await _orders.ConfirmPaymentAsync(order.Id);
            foreach (var status in new[] { "Confirmed", "Preparing", "Delivering", "Completed" })
            {
                order = await _orders.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = status });
            }

            return order;
        }
    }
}
