using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Subscriptions.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Subscriptions
{
    public interface ISubscriptionAppService : IApplicationService
    {
        /// <summary>Public: customer creates a daily subscription.</summary>
        Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto);

        /// <summary>Admin: subscription list.</summary>
        Task<List<SubscriptionDto>> GetListAsync(bool? activeOnly);

        /// <summary>Admin: pause / resume / cancel a subscription.</summary>
        Task<SubscriptionDto> SetActiveAsync(Guid id, bool active);

        /// <summary>
        /// Internal: called by the morning worker — creates a Pending COD
        /// order for every active subscription due today. Returns the number
        /// of orders created.
        /// </summary>
        Task<int> CreateDueOrdersAsync();
    }

    public class SubscriptionAppService : ApplicationService, ISubscriptionAppService
    {
        private readonly Volo.Abp.Domain.Repositories.IRepository<Subscription, Guid> _subscriptions;
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> _sizeOptions;
        private readonly Volo.Abp.Domain.Repositories.IRepository<Topping, Guid> _toppings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IShopSettingAppService _shopSettings;

        public SubscriptionAppService(
            Volo.Abp.Domain.Repositories.IRepository<Subscription, Guid> subscriptions,
            IOrderRepository orders,
            IProductRepository products,
            Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> sizeOptions,
            Volo.Abp.Domain.Repositories.IRepository<Topping, Guid> toppings,
            IAsyncQueryableExecuter asyncExecuter,
            IShopSettingAppService shopSettings)
        {
            _subscriptions = subscriptions;
            _orders = orders;
            _products = products;
            _sizeOptions = sizeOptions;
            _toppings = toppings;
            _asyncExecuter = asyncExecuter;
            _shopSettings = shopSettings;
        }

        [AllowAnonymous]
        public virtual async Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto)
        {
            var (productName, unitPrice, sizeName, toppingNames) =
                await ResolveSnapshotAsync(dto.ProductId, dto.SizeOptionId, dto.ToppingIds);

            var start = dto.StartDate.Date;
            if (start < DateTime.UtcNow.Date)
            {
                throw new Domain.Exceptions.DomainException("Ngày bắt đầu không thể trong quá khứ.");
            }

            var entity = new Subscription
            {
                Phone = dto.Phone.Trim(),
                CustomerName = dto.CustomerName.Trim(),
                ProductId = dto.ProductId,
                ProductName = productName,
                SizeName = sizeName,
                ToppingNames = toppingNames,
                UnitPrice = unitPrice,
                Quantity = dto.Quantity,
                DeliveryAddress = dto.DeliveryAddress.Trim(),
                StartDate = start,
                Active = true,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await _subscriptions.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<SubscriptionDto>> GetListAsync(bool? activeOnly)
        {
            var queryable = await _subscriptions.GetQueryableAsync();
            IQueryable<Subscription> query = queryable.OrderByDescending(x => x.CreatedAt);

            if (activeOnly.HasValue)
            {
                query = query.Where(x => x.Active == activeOnly.Value);
            }

            var items = await _asyncExecuter.ToListAsync(query.Take(200));
            return items.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<SubscriptionDto> SetActiveAsync(Guid id, bool active)
        {
            var entity = await _subscriptions.GetAsync(id);
            entity.Active = active;
            await _subscriptions.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        public virtual async Task<int> CreateDueOrdersAsync()
        {
            var today = DateTime.UtcNow.Date;
            var settings = await _shopSettings.GetAsync();

            var queryable = await _subscriptions.GetQueryableAsync();
            var due = await _asyncExecuter.ToListAsync(
                queryable.Where(x =>
                    x.Active
                    && x.StartDate <= today
                    && (x.LastOrderDate == null || x.LastOrderDate < today)));

            var created = 0;
            foreach (var sub in due)
            {
                var lines = new List<(Guid ProductId, string ProductName, Money UnitPrice, int Quantity,
                    string SizeName, string ToppingNames, decimal ToppingsExtra, bool IsCombo, string Note)>
                {
                    (sub.ProductId, sub.ProductName, Money.Of(sub.UnitPrice, "VND"),
                        sub.Quantity, sub.SizeName, sub.ToppingNames, 0, false, null)
                };

                var subtotal = sub.UnitPrice * sub.Quantity;
                var fee = settings.DeliveryFee > 0
                    && !(settings.FreeShipThreshold > 0 && subtotal >= settings.FreeShipThreshold)
                    ? settings.DeliveryFee
                    : 0;

                var order = Order.Create(
                    await GenerateOrderNumberAsync(),
                    sub.CustomerName,
                    sub.Phone,
                    null,
                    sub.DeliveryAddress,
                    "Đơn định kỳ tự động" + (string.IsNullOrWhiteSpace(sub.Notes) ? "" : " — " + sub.Notes),
                    PaymentMethod.Cod,
                    lines,
                    Money.Of(fee, "VND"));

                await _orders.InsertAsync(order);
                sub.LastOrderDate = today;
                await _subscriptions.UpdateAsync(sub);
                created++;
            }

            return created;
        }

        /// <summary>
        /// Resolves the product/price snapshot for a new subscription from
        /// the current catalog (size replaces base price, toppings add extra).
        /// </summary>
        private async Task<(string ProductName, decimal UnitPrice, string SizeName, string ToppingNames)>
            ResolveSnapshotAsync(Guid productId, Guid? sizeOptionId, List<Guid> toppingIds)
        {
            var productQueryable = await _products.GetQueryableAsync();
            var product = await _asyncExecuter.FirstOrDefaultAsync(
                productQueryable.Where(x => x.Id == productId));
            if (product == null || product.Status != ProductStatus.Active)
            {
                throw new Domain.Exceptions.DomainException("Món ăn không tồn tại hoặc đã ngừng bán.");
            }

            string sizeName = null;
            var unitPrice = product.Price.Amount;
            if (sizeOptionId.HasValue)
            {
                var sizeQueryable = await _sizeOptions.GetQueryableAsync();
                var size = await _asyncExecuter.FirstOrDefaultAsync(
                    sizeQueryable.Where(x => x.Id == sizeOptionId.Value));
                if (size == null || !size.IsActive || size.ProductId != product.Id)
                {
                    throw new Domain.Exceptions.DomainException("Size không khả dụng.");
                }

                sizeName = size.Name;
                unitPrice = size.Price;
            }

            string toppingNames = null;
            var ids = (toppingIds ?? new List<Guid>()).Distinct().ToList();
            if (ids.Count > 0)
            {
                var toppingQueryable = await _toppings.GetQueryableAsync();
                var resolved = await _asyncExecuter.ToListAsync(
                    toppingQueryable.Where(x => ids.Contains(x.Id) && x.IsActive));
                if (resolved.Count != ids.Count)
                {
                    throw new Domain.Exceptions.DomainException("Một topping đã chọn không còn khả dụng.");
                }

                toppingNames = string.Join(", ", resolved.Select(x => x.Name));
                unitPrice += resolved.Sum(x => x.ExtraPrice);
            }

            return (product.Name, unitPrice, sizeName, toppingNames);
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            var random = Random.Shared; // perf-hunt 2026-10-19: Random.Shared is thread-safe and avoids same-tick seed collisions across concurrent requests
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var orderNumber = $"PX-{DateTime.UtcNow:yyyyMMdd}-{random.Next(1000, 9999)}";
                if (!await _orders.ExistsWithOrderNumberAsync(orderNumber))
                {
                    return orderNumber;
                }
            }

            throw new Domain.Exceptions.DomainException("Could not generate a unique order number.");
        }

        private static SubscriptionDto MapToDto(Subscription entity)
        {
            return new SubscriptionDto
            {
                Id = entity.Id,
                Phone = entity.Phone,
                CustomerName = entity.CustomerName,
                ProductId = entity.ProductId,
                ProductName = entity.ProductName,
                SizeName = entity.SizeName,
                ToppingNames = entity.ToppingNames,
                UnitPrice = entity.UnitPrice,
                Quantity = entity.Quantity,
                DeliveryAddress = entity.DeliveryAddress,
                StartDate = entity.StartDate,
                Active = entity.Active,
                Notes = entity.Notes,
                LastOrderDate = entity.LastOrderDate,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
