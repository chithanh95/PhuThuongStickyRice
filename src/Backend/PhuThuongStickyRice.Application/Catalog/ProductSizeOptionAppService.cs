using PhuThuongStickyRice.Application.Products;
using PhuThuongStickyRice.Application.Products.DTOs;
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
    public interface IProductSizeOptionAppService : IApplicationService
    {
        Task<List<ProductSizeOptionDto>> GetByProductAsync(Guid productId);

        Task<ProductSizeOptionDto> CreateAsync(CreateUpdateProductSizeOptionDto dto);

        Task<ProductSizeOptionDto> UpdateAsync(Guid id, CreateUpdateProductSizeOptionDto dto);

        Task DeleteAsync(Guid id);
    }

    [Authorize(Roles = "Admin")]
    public class ProductSizeOptionAppService : ApplicationService, IProductSizeOptionAppService
    {
        private readonly IRepository<ProductSizeOption, Guid> _options;
        private readonly IRepository<Product, Guid> _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IProductCatalogCache _catalogCache;

        public ProductSizeOptionAppService(
            IRepository<ProductSizeOption, Guid> options,
            IRepository<Product, Guid> products,
            IAsyncQueryableExecuter asyncExecuter,
            IProductCatalogCache catalogCache)
        {
            _options = options;
            _products = products;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        public virtual async Task<List<ProductSizeOptionDto>> GetByProductAsync(Guid productId)
        {
            var queryable = await _options.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.ProductId == productId)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Price));
            return entities.Select(MapToDto).ToList();
        }

        public virtual async Task<ProductSizeOptionDto> CreateAsync(CreateUpdateProductSizeOptionDto dto)
        {
            await EnsureProductExistsAsync(dto.ProductId);

            var entity = new ProductSizeOption
            {
                ProductId = dto.ProductId,
                Name = dto.Name.Trim(),
                Price = Math.Max(0, dto.Price),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };

            await _options.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        public virtual async Task<ProductSizeOptionDto> UpdateAsync(Guid id, CreateUpdateProductSizeOptionDto dto)
        {
            var entity = await _options.GetAsync(id);

            entity.Name = dto.Name.Trim();
            entity.Price = Math.Max(0, dto.Price);
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;

            await _options.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _options.DeleteAsync(id);
            _catalogCache.Invalidate();
        }

        private async Task EnsureProductExistsAsync(Guid productId)
        {
            if (await _products.FindAsync(productId) == null)
            {
                throw new Domain.Exceptions.DomainException($"Product '{productId}' not found.");
            }
        }

        private static ProductSizeOptionDto MapToDto(ProductSizeOption entity)
        {
            return new ProductSizeOptionDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                Name = entity.Name,
                Price = entity.Price,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive
            };
        }
    }
}
