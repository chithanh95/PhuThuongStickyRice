using PhuThuongStickyRice.Application.Catalog.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Catalog
{
    public interface IComboAppService : IApplicationService
    {
        /// <summary>Public: active combos for the shop.</summary>
        Task<List<ComboDto>> GetActiveAsync();

        Task<List<ComboDto>> GetListAsync();

        Task<ComboDto> GetAsync(Guid id);

        Task<ComboDto> CreateAsync(CreateUpdateComboDto dto);

        Task<ComboDto> UpdateAsync(Guid id, CreateUpdateComboDto dto);

        Task DeleteAsync(Guid id);
    }

    public class ComboAppService : ApplicationService, IComboAppService
    {
        private readonly IRepository<Combo, Guid> _combos;
        private readonly IRepository<Product, Guid> _products;
        private readonly IRepository<OrderLine, Guid> _orderLines;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        public ComboAppService(
            IRepository<Combo, Guid> combos,
            IRepository<Product, Guid> products,
            IRepository<OrderLine, Guid> orderLines,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _combos = combos;
            _products = products;
            _orderLines = orderLines;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        [AllowAnonymous]
        public async Task<List<ComboDto>> GetActiveAsync()
        {
            var queryable = await _combos.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive).OrderBy(x => x.Price));
            return await MapToDtosAsync(entities);
        }

        [Authorize(Roles = "Admin")]
        public async Task<List<ComboDto>> GetListAsync()
        {
            var queryable = await _combos.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedAt));
            return await MapToDtosAsync(entities);
        }

        [Authorize(Roles = "Admin")]
        public async Task<ComboDto> GetAsync(Guid id)
        {
            var entity = await _combos.GetAsync(id);
            return (await MapToDtosAsync(new[] { entity })).First();
        }

        [Authorize(Roles = "Admin")]
        public async Task<ComboDto> CreateAsync(CreateUpdateComboDto dto)
        {
            await EnsureItemsValidAsync(dto);

            var entity = new Combo
            {
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Price = Math.Max(0, dto.Price),
                IsActive = dto.IsActive
            };

            foreach (var item in dto.Items ?? Enumerable.Empty<CreateUpdateComboItemDto>())
            {
                entity.Items.Add(new ComboItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            await _combos.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return (await MapToDtosAsync(new[] { entity })).First();
        }

        [Authorize(Roles = "Admin")]
        public async Task<ComboDto> UpdateAsync(Guid id, CreateUpdateComboDto dto)
        {
            await EnsureItemsValidAsync(dto);

            var entity = await _combos.GetAsync(id);

            entity.Name = dto.Name.Trim();
            entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            entity.Price = Math.Max(0, dto.Price);
            entity.IsActive = dto.IsActive;

            entity.Items.Clear();
            foreach (var item in dto.Items ?? Enumerable.Empty<CreateUpdateComboItemDto>())
            {
                entity.Items.Add(new ComboItem
                {
                    ComboId = entity.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            await _combos.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return (await MapToDtosAsync(new[] { entity })).First();
        }

        [Authorize(Roles = "Admin")]
        public async Task DeleteAsync(Guid id)
        {
            // OrderLine.ProductId chứa combo id khi IsCombo=true.
            // Nếu combo đã có trong đơn hàng thì deactivate thay vì xóa cứng.
            var orderLinesQuery = await _orderLines.GetQueryableAsync();
            var hasOrderHistory = await _asyncExecuter.AnyAsync(
                orderLinesQuery.Where(x => x.ProductId == id && x.IsCombo));
            var entity = await _combos.GetAsync(id);
            if (hasOrderHistory)
            {
                entity.IsActive = false;
                await _combos.UpdateAsync(entity, autoSave: true);
            }
            else
            {
                await _combos.DeleteAsync(entity, autoSave: true);
            }
            _catalogCache.Invalidate();
        }

        private async Task EnsureItemsValidAsync(CreateUpdateComboDto dto)
        {
            var items = dto.Items ?? new List<CreateUpdateComboItemDto>();
            if (items.Count == 0)
            {
                throw new Domain.Exceptions.DomainException("Combo phải có ít nhất một món.");
            }

            var productIds = items.Select(x => x.ProductId).Distinct().ToList();
            var queryable = await _products.GetQueryableAsync();
            var count = await _asyncExecuter.LongCountAsync(
                queryable.Where(x => productIds.Contains(x.Id)));
            if (count != productIds.Count)
            {
                throw new Domain.Exceptions.DomainException("Combo chứa sản phẩm không tồn tại.");
            }
        }

        private async Task<List<ComboDto>> MapToDtosAsync(IEnumerable<Combo> entities)
        {
            var list = entities.ToList();
            var productIds = list.SelectMany(x => x.Items).Select(x => x.ProductId).Distinct().ToList();

            var names = new Dictionary<Guid, (string Name, decimal Price)>();
            if (productIds.Count > 0)
            {
                var queryable = await _products.GetQueryableAsync();
                var products = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => productIds.Contains(x.Id)));
                foreach (var p in products)
                {
                    names[p.Id] = (p.Name, p.Price.Amount);
                }
            }

            return list.Select(e => new ComboDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Price = e.Price,
                IsActive = e.IsActive,
                Items = e.Items.Select(i => new ComboItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = names.TryGetValue(i.ProductId, out var n) ? n.Name : i.ProductId.ToString(),
                    ProductPrice = names.TryGetValue(i.ProductId, out var n2) ? n2.Price : 0,
                    Quantity = i.Quantity
                }).ToList()
            }).ToList();
        }
    }
}
