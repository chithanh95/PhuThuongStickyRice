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
    public interface IToppingAppService : IApplicationService
    {
        /// <summary>Public: active toppings for the quick-view checkboxes.</summary>
        Task<List<ToppingDto>> GetActiveAsync();

        Task<List<ToppingDto>> GetListAsync();

        Task<ToppingDto> GetAsync(Guid id);

        Task<ToppingDto> CreateAsync(CreateUpdateToppingDto dto);

        Task<ToppingDto> UpdateAsync(Guid id, CreateUpdateToppingDto dto);

        Task DeleteAsync(Guid id);
    }

    public class ToppingAppService : ApplicationService, IToppingAppService
    {
        private readonly IRepository<Topping, Guid> _toppings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        public ToppingAppService(
            IRepository<Topping, Guid> toppings,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _toppings = toppings;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        [AllowAnonymous]
        public virtual async Task<List<ToppingDto>> GetActiveAsync()
        {
            var queryable = await _toppings.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<ToppingDto>> GetListAsync()
        {
            var queryable = await _toppings.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<ToppingDto> GetAsync(Guid id)
        {
            return MapToDto(await _toppings.GetAsync(id));
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<ToppingDto> CreateAsync(CreateUpdateToppingDto dto)
        {
            var entity = new Topping
            {
                Name = dto.Name.Trim(),
                ExtraPrice = Math.Max(0, dto.ExtraPrice),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };

            await _toppings.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<ToppingDto> UpdateAsync(Guid id, CreateUpdateToppingDto dto)
        {
            var entity = await _toppings.GetAsync(id);

            entity.Name = dto.Name.Trim();
            entity.ExtraPrice = Math.Max(0, dto.ExtraPrice);
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;

            await _toppings.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            // Topping lưu tên trong OrderLine.ToppingNames (string, không FK),
            // nhưng để giữ lịch sử đơn hàng hiển thị đúng thì deactivate
            // thay vì xóa cứng.
            var entity = await _toppings.GetAsync(id);
            entity.IsActive = false;
            await _toppings.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
        }

        private static ToppingDto MapToDto(Topping entity)
        {
            return new ToppingDto
            {
                Id = entity.Id,
                Name = entity.Name,
                ExtraPrice = entity.ExtraPrice,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive
            };
        }
    }
}
